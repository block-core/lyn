using System;
using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.NormalOperations.Messages;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt2.NormalOperations
{
    public class CommitmentSignedMessageService : IBoltMessageService<CommitmentSigned>
    {
        private readonly ILogger<CommitmentSignedMessageService> _logger;
        private readonly IPaymentChannelRepository _paymentChannelRepository;
        private readonly ISecretStore _secretStore;
        private readonly ILightningKeyDerivation _lightningKeyDerivation;

        public CommitmentSignedMessageService(ILogger<CommitmentSignedMessageService> logger,
            IPaymentChannelRepository paymentChannelRepository,
            ISecretStore secretStore,
            ILightningKeyDerivation lightningKeyDerivation)
        {
            _logger = logger;
            _paymentChannelRepository = paymentChannelRepository;
            _secretStore = secretStore;
            _lightningKeyDerivation = lightningKeyDerivation;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<CommitmentSigned> message)
        {
            var commitmentSigned = message.MessagePayload;

            if (commitmentSigned.ChannelId is null || commitmentSigned.Signature is null)
                return new ErrorCloseChannelResponse(commitmentSigned.ChannelId ?? new UInt256(new byte[32]),
                    "Invalid commitment_signed message");

            var channel =
                await _paymentChannelRepository.TryGetPaymentChannelAsync(commitmentSigned.ChannelId);

            if (channel is null)
            {
                _logger.LogError("Payment channel not found for channel id {ChannelId}", commitmentSigned.ChannelId);

                return new ErrorCloseChannelResponse(commitmentSigned.ChannelId, "Channel not found");
            }

            ushort numHtlcs = commitmentSigned.NumHtlcs ??
                              (ushort)(commitmentSigned.HtlcSignature?.Length / CompressedSignature.LENGTH ?? 0);

            if (numHtlcs > 0 &&
                (commitmentSigned.HtlcSignature is null ||
                 commitmentSigned.HtlcSignature.Length != numHtlcs * CompressedSignature.LENGTH))
                return new ErrorCloseChannelResponse(commitmentSigned.ChannelId,
                    "htlc_signature length must match num_htlcs");

            // The remote peer has signed the commitment for our next local commitment number.
            channel.RemoteCommitmentNumber++;

            await _paymentChannelRepository.UpdatePaymentChannelAsync(channel);

            // Respond with revoke_and_ack revoking our previous commitment state as required by BOLT 2.
            var secrets = _lightningKeyDerivation.DeriveSecrets(_secretStore.GetSeed());

            var revokeAndAck = new RevokeAndAck
            {
                ChannelId = channel.ChannelId,
                PerCommitmentSecret =
                    _lightningKeyDerivation.PerCommitmentSecret(secrets.Shaseed, channel.LocalCommitmentNumber),
                NextPerCommitmentPoint = _lightningKeyDerivation.PerCommitmentPoint(secrets.Shaseed,
                    channel.LocalCommitmentNumber + 2)
            };

            channel.PreviousPerCommitmentSecrets = channel.PreviousPerCommitmentSecrets
                .Append(revokeAndAck.PerCommitmentSecret).ToArray();
            channel.LocalCommitmentNumber++;

            await _paymentChannelRepository.UpdatePaymentChannelAsync(channel);

            return new SuccessWithOutputResponse(new BoltMessage { Payload = revokeAndAck });
        }
    }
}
