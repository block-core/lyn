using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment
{
    public class FundingCreatedMessageService : IBoltMessageService<FundingCreated>
    {
        private readonly ILogger<FundingCreatedMessageService> _logger;

        public FundingCreatedMessageService(ILogger<FundingCreatedMessageService> logger)
        {
            _logger = logger;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<FundingCreated> message)
        {
            FundingCreated fundingCreated = message.MessagePayload;

            _logger.LogDebug("FundingCreated");

            return new MessageProcessingOutput { Success = true };
        }
    }
}