using System;
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
        private readonly IPeerRepository _repository;

        public InitMessageService(IPeerRepository repository)
        {
            _repository = repository;
        }

        public async ValueTask<MessageProcessingOutput> ProcessMessageAsync(InitMessage message, CancellationToken cancellation)
        {
            var peer = new Peer
            {
                Featurs = message.Features,
                GlobalFeatures = message.GlobalFeatures
            };
            
            await _repository.AddNewPeerAsync(peer);

            return new MessageProcessingOutput
            {
                Success = true,
                ResponseMessage = CreateInitMessage()
            };
        }

        public ValueTask<InitMessage> CreateNewMessageAsync()
        {
            return new(CreateInitMessage());
        }
        
        
        private static InitMessage CreateInitMessage()
        {
            var supportedFeatures = Features.InitialRoutingSync | Features.GossipQueries;
            
            return new InitMessage
            {
                GlobalFeatures = new byte[4],
                Features = BitConverter.GetBytes((ulong)supportedFeatures),
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
}