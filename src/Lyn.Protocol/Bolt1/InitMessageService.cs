using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Types;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Bolt.Messages.TlvRecords;

namespace Lyn.Protocol.Bolt1
{
    public class InitMessageService : ISetupMessageService<InitMessage>
    {
        private readonly IPeerMessageRepository _repository;

        public InitMessageService(IPeerMessageRepository repository)
        {
            _repository = repository;
        }

        public async ValueTask<MessageProcessingOutput> ProcessMessageAsync(InitMessage message, CancellationToken cancellation)
        {
            var peer = new Peer();
            
            await _repository.AddNewPeerAsync(peer);

            return new MessageProcessingOutput
            {
                Success = true,
                ResponseMessage = CreateInitMessage()
            };
        }

        public ValueTask<InitMessage> CreateNewMessageAsync()
        {
            throw new System.NotImplementedException();
        }
        
        
        private InitMessage CreateInitMessage()
        {
            return new InitMessage
            {
                GlobalFeatures = new byte[4],
                Features = new byte[4],
                Extension = new TlVStream
                {
                    Records = new List<TlvRecord>
                    {
                        new NetworksTlvRecord {Type = 1},
                    }
                }
            };
        }
    }

    public interface IPeerMessageRepository
    {
        ValueTask AddNewPeerAsync(Peer peer);
    }
}