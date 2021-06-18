using System;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt2.Messags;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Connection;
using Lyn.Types;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt2
{
    public class OpenChannelService : IBoltMessageService<OpenChannel>
    {
        private readonly ILogger<OpenChannelService> _logger;
        private readonly ILightningTransactions _lightningTransactions;

        public OpenChannelService(ILogger<OpenChannelService> logger, ILightningTransactions lightningTransactions)
        {
            _logger = logger;
            _lightningTransactions = lightningTransactions;
        }

        public Task ProcessMessageAsync(PeerMessage<OpenChannel> message)
        {
            throw new NotImplementedException();
        }
    }
}