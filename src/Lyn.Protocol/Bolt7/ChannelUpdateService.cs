using System.IO;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt7.Messages;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Lyn.Protocol.Bolt7
{
    public class ChannelUpdateService : IBoltMessageService<ChannelUpdate>
    {
        private readonly ILogger<ChannelUpdateService> _logger;

        public ChannelUpdateService(ILogger<ChannelUpdateService> logger)
        {
            _logger = logger;
        }

        public Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<ChannelUpdate> message)
        {
            var s = JsonSerializer.Create();

            var stream = new MemoryStream();
            
            var writer = new StreamWriter(stream); 
            
            s.Serialize(writer, message.MessagePayload);

            _logger.LogDebug(new StreamReader(stream).ReadToEnd());

            return Task.FromResult((MessageProcessingOutput)new EmptySuccessResponse());
        }
    }
}