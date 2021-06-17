using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lyn.Types;
using Lyn.Types.Bolt.Messages;
using Microsoft.Extensions.Logging;

namespace Lyn.Protocol.Bolt1
{
    public class ErrorMessageService : ISetupMessageService<ErrorMessage>
    {
        private readonly IPeerRepository _repository;
        private readonly ILogger<ErrorMessageService> _logger;

        public ErrorMessageService(IPeerRepository repository, ILogger<ErrorMessageService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async ValueTask<MessageProcessingOutput> ProcessMessageAsync(ErrorMessage message, CancellationToken cancellation)
        {
            _logger.LogDebug($"Received error message from {0}");//PeerContext.PeerId//
            _logger.LogDebug($"{Encoding.ASCII.GetString(message.Data)}");

            await _repository.AddErrorMessageToPeerAsync("TODO add peer context", message);
            
            return new MessageProcessingOutput{Success = true};
        }

        public ValueTask<ErrorMessage> CreateNewMessageAsync()
        {
            return new(new ErrorMessage());
        }
    }
}