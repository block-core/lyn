using System.Text;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Common;
using Lyn.Protocol.Connection;
using Lyn.Types;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt1
{
    public class ErrorMessageService : IBoltMessageService<ErrorMessage>
    {
        private readonly IPeerRepository _repository;
        private readonly ILogger<ErrorMessageService> _logger;

        public ErrorMessageService(ILogger<ErrorMessageService> logger,IPeerRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        public async Task ProcessMessageAsync(PeerMessage<ErrorMessage> request)
        {
            _logger.LogDebug($"Received error message from {0}");//PeerContext.PeerId//
            
            if (request.MessagePayload.Data != null)
                _logger.LogDebug($"{Encoding.ASCII.GetString(request.MessagePayload.Data)}");
            
            await _repository.AddErrorMessageToPeerAsync(request.NodeId, request.MessagePayload);

            //TODO David need to connect the logic to fail a channel after Bolt2 is implemented

            if (request.MessagePayload.ChannelId.IsEmpty)
                _logger.Log(LogLevel.Information,"Need to fail all channels"); // TODO fail all channels
            else
                _logger.Log(LogLevel.Information,
                    $"Need to fail channel {Hex.ToString(request.MessagePayload.ChannelId)}"); // TODO fail the channel
        }
    }
}