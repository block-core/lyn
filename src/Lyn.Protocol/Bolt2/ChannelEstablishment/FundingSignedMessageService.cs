using System.Buffers;
using System.Collections.Generic;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Entities;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Blockchain;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Bitcoin;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Types;
using Lyn.Types.Fundamental;
using Microsoft.Extensions.DependencyInjection;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment
{
    public class FundingSignedMessageService : IBoltMessageService<FundingSigned>
    {
        private readonly ILogger<FundingSigned> _logger;
        private readonly ILightningTransactions _lightningTransactions;
        private readonly ITransactionHashCalculator _transactionHashCalculator;
        private readonly ILightningScripts _lightningScripts;
        private readonly ILightningKeyDerivation _lightningKeyDerivation;
        private readonly IChannelCandidateRepository _channelCandidateRepository;
        private readonly IChainConfigProvider _chainConfigProvider;
        private readonly ISecretStore _secretStore;
        private readonly IPeerRepository _peerRepository;

        public FundingSignedMessageService(ILogger<FundingSigned> logger,
            ILightningTransactions lightningTransactions,
            ITransactionHashCalculator transactionHashCalculator,
            ILightningScripts lightningScripts,
            ILightningKeyDerivation lightningKeyDerivation,
            IChannelCandidateRepository channelCandidateRepository,
            IChainConfigProvider chainConfigProvider,
            ISecretStore secretStore,
            IPeerRepository peerRepository)
        {
            _logger = logger;
            _lightningTransactions = lightningTransactions;
            _transactionHashCalculator = transactionHashCalculator;
            _lightningScripts = lightningScripts;
            _lightningKeyDerivation = lightningKeyDerivation;
            _channelCandidateRepository = channelCandidateRepository;
            _chainConfigProvider = chainConfigProvider;
            _secretStore = secretStore;
            _peerRepository = peerRepository;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<FundingSigned> message)
        {
            FundingSigned fundingSigned = message.MessagePayload;

            _logger.LogDebug("FundingSigned - signature = " + Hex.ToString(_lightningTransactions.FromCompressedSignature(fundingSigned.Signature)));

            return new MessageProcessingOutput { Success = true };
        }
    }
}