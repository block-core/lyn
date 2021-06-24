using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Connection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment
{
    public class OpenChannelValidationService : IBoltValidationService<OpenChannel>
    {
        private readonly ILogger<OpenChannelValidationService> _logger;

        public OpenChannelValidationService(ILogger<OpenChannelValidationService> logger)
        {
            _logger = logger;
        }

        public Task<bool> ValidateMessageAsync(PeerMessage<OpenChannel> message)
        {
            throw new System.NotImplementedException();
        }
    }
}