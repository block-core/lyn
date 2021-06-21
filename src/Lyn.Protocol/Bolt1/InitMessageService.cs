using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Protocol.Connection;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Bolt.Messages.TlvRecords;

namespace Lyn.Protocol.Bolt1
{
    public class InitMessageService : IBoltMessageService<InitMessage>
    {
        private readonly IPeerRepository _repository;
        private readonly IBoltMessageSender<InitMessage> _messageSender;
        
        public InitMessageService(IPeerRepository repository, IBoltMessageSender<InitMessage> messageSender)
        {
            _repository = repository;
            _messageSender = messageSender ?? throw new ArgumentNullException();
        }

        public async Task ProcessMessageAsync(PeerMessage<InitMessage> request)
        {
            var peer = new Peer
            {
                Featurs = request.Message.Features,
                GlobalFeatures = request.Message.GlobalFeatures
            };
            
            await _repository.AddNewPeerAsync(peer);

            await _messageSender.SendMessageAsync(new PeerMessage<InitMessage>
            {
                Message = CreateInitMessage(),
                NodeId = request.NodeId
            });
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