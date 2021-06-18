using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lyn.Protocol.Common;
using Lyn.Types;
using Lyn.Types.Bolt.Messages;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt1
{
    public class ErrorMessageService : ISetupMessageService<ErrorMessage>
    {
        private readonly IPeerRepository _repository;
        private readonly ILogger<ErrorMessageService> _logger;

        public ErrorMessageService(ILogger<ErrorMessageService> logger,IPeerRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        public async ValueTask<MessageProcessingOutput> ProcessMessageAsync(ErrorMessage message, CancellationToken cancellation)
        {
            _logger.LogDebug($"Received error message from {0}");//PeerContext.PeerId//
            
            if (message.Data!= null)
                _logger.LogDebug($"{Encoding.ASCII.GetString(message.Data)}");
            
            await _repository.AddErrorMessageToPeerAsync("TODO add peer context", message);

            //TODO David need to connect the logic to fail a channel after Bolt2 is implemented

            if (message.ChannelId.IsEmpty)
                _logger.Log(LogLevel.Information,"Need to fail all channels"); // TODO fail all channels
            else
                _logger.Log(LogLevel.Information,
                    $"Need to fail channel {Hex.ToString(message.ChannelId)}"); // TODO fail the channel

            return new MessageProcessingOutput {Success = true};
        }

        public ValueTask<ErrorMessage> CreateNewMessageAsync()
        {
            return new(new ErrorMessage());
        }
    }
}