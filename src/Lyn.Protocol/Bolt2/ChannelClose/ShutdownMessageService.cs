using System;
using System.Linq;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.ChannelClose.Messages;
using Lyn.Protocol.Bolt2.NormalOperations;
using Lyn.Protocol.Bolt2.Wallet;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bitcoin;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt2.ChannelClose
{
    public class ShutdownMessageService : IBoltMessageService<Shutdown>
    {
        private readonly ILogger<ShutdownMessageService> _logger;
        private readonly IPaymentChannelRepository _paymentChannelRepository;
        private readonly IWalletTransactions _walletTransactions;

        public ShutdownMessageService(ILogger<ShutdownMessageService> logger,
            IPaymentChannelRepository paymentChannelRepository,
            IWalletTransactions walletTransactions)
        {
            _logger = logger;
            _paymentChannelRepository = paymentChannelRepository;
            _walletTransactions = walletTransactions;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<Shutdown> message)
        {
            var shutdown = message.MessagePayload;

            if (shutdown.ChannelId is null || shutdown.ScriptPubkey is null || shutdown.ScriptPubkey.Length == 0)
                return new ErrorCloseChannelResponse(shutdown.ChannelId ?? new UInt256(new byte[32]),
                    "Invalid shutdown message");

            var channel =
                await _paymentChannelRepository.TryGetPaymentChannelAsync(shutdown.ChannelId);

            if (channel is null)
            {
                _logger.LogError("Payment channel not found for channel id {ChannelId}", shutdown.ChannelId);

                return new ErrorCloseChannelResponse(shutdown.ChannelId, "Channel not found");
            }

            // Per BOLT 2 no HTLCs may be outstanding when shutdown is received.
            if (channel.PendingRemoteHtlcs.Count > 0 && !channel.ShutdownReceived)
                return new ErrorCloseChannelResponse(shutdown.ChannelId,
                    "Can't shutdown while there are pending htlcs");

            // Per BOLT 2 only a restricted set of scriptpubkey formats is allowed.
            if (!IsValidScriptPubKey(shutdown.ScriptPubkey))
                return new ErrorCloseChannelResponse(shutdown.ChannelId,
                    "Unsupported scriptpubkey in shutdown");

            channel.RemoteShutdownScriptPubkey = shutdown.ScriptPubkey;
            channel.ShutdownReceived = true;

            await _paymentChannelRepository.UpdatePaymentChannelAsync(channel);

            // If we haven't sent our own shutdown yet, respond with it now to complete the
            // mutual shutdown initiation as required by BOLT 2.
            BoltMessage? ourShutdown = null;

            if (!channel.ShutdownInitiatedLocally)
            {
                byte[] localScript = await GetLocalShutdownScriptAsync();

                if (localScript.Length > 0)
                {
                    channel.LocalShutdownScriptPubkey = localScript;
                    channel.ShutdownInitiatedLocally = true;

                    await _paymentChannelRepository.UpdatePaymentChannelAsync(channel);

                    ourShutdown = new BoltMessage
                    {
                        Payload = new Shutdown
                        {
                            ChannelId = channel.ChannelId,
                            Length = (ushort)localScript.Length,
                            ScriptPubkey = localScript
                        }
                    };
                }
            }

            _logger.LogDebug("Processed shutdown for channel {ChannelId}", shutdown.ChannelId);

            return ourShutdown == null
                ? new EmptySuccessResponse()
                : new SuccessWithOutputResponse(ourShutdown);
        }

        /// <summary>
        /// Generates a new destination address from the node wallet that funds should be paid
        /// out to when cooperatively closing the channel.
        /// </summary>
        private async Task<byte[]> GetLocalShutdownScriptAsync()
        {
            try
            {
                Transaction transaction =
                    await _walletTransactions.GenerateTransactionForOutputAsync(new TransactionOutput());

                return transaction.Outputs.FirstOrDefault()?.PublicKeyScript ?? Array.Empty<byte>();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Unable to obtain a shutdown destination from the wallet");

                return Array.Empty<byte>();
            }
        }

        private static bool IsValidScriptPubKey(byte[] script)
        {
            return script.Length switch
            {
                25 => true, // P2PKH
                23 => true, // P2SH
                22 => script[0] == 0x00, // P2WPKH
                34 => script[0] == 0x00 || script[0] == 0x51, // P2WSH / P2TR
                _ => false
            };
        }
    }
}
