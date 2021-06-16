using System;
using Lyn.Protocol.Bolt2.Messags;
using Lyn.Protocol.Bolt3;
using Lyn.Types;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt2
{
    public class ChannelEstablishmentService :
        INetworkMessageService<OpenChannel>,
        INetworkMessageService<AcceptChannel>,
        INetworkMessageService<FundingCreated>,
        INetworkMessageService<FundingSigned>,
        INetworkMessageService<FundingLocked>
    {
        private readonly ILogger<ChannelEstablishmentService> _logger;
        private readonly ILightningTransactions _lightningTransactions;

        public ChannelEstablishmentService(ILogger<ChannelEstablishmentService> logger, ILightningTransactions lightningTransactions)
        {
            _logger = logger;
            _lightningTransactions = lightningTransactions;
        }

        public MessageProcessingOutput ProcessMessage(OpenChannel message)
        {
            throw new NotImplementedException();
        }

        public MessageProcessingOutput ProcessMessage(AcceptChannel message)
        {
            throw new NotImplementedException();
        }

        public MessageProcessingOutput ProcessMessage(FundingCreated message)
        {
            throw new NotImplementedException();
        }

        public MessageProcessingOutput ProcessMessage(FundingSigned message)
        {
            throw new NotImplementedException();
        }

        public MessageProcessingOutput ProcessMessage(FundingLocked message)
        {
            throw new NotImplementedException();
        }
    }
}