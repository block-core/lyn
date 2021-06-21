using System.Text;
using System.Threading.Tasks;
using Lyn.Protocol.Common;
using Lyn.Protocol.Connection;
using Lyn.Types.Bolt.Messages;
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
            
            if (request.Message.Data != null)
                _logger.LogDebug($"{Encoding.ASCII.GetString(request.Message.Data)}");
            
            await _repository.AddErrorMessageToPeerAsync(request.NodeId, request.Message);

            //TODO David need to connect the logic to fail a channel after Bolt2 is implemented

            if (request.Message.ChannelId.IsEmpty)
                _logger.Log(LogLevel.Information,"Need to fail all channels"); // TODO fail all channels
            else
                _logger.Log(LogLevel.Information,
                    $"Need to fail channel {Hex.ToString(request.Message.ChannelId)}"); // TODO fail the channel
        }
    }
}