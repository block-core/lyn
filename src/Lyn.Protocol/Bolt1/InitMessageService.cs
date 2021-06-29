using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Protocol.Bolt9;
using Lyn.Protocol.Connection;
using Lyn.Types;
using Lyn.Types.Bolt.Messages;
using Lyn.Types.Bolt.Messages.TlvRecords;

namespace Lyn.Protocol.Bolt1
{
    public class InitMessageService : IBoltMessageService<InitMessage>
    {
            private readonly IPeerRepository _repository;
            private readonly IBoltMessageSender<InitMessage> _messageSender;
            private readonly IBoltFeatures _boltFeatures;

            public InitMessageService(IPeerRepository repository, IBoltMessageSender<InitMessage> messageSender, 
            IBoltFeatures boltFeatures)
        {
            _repository = repository;
            _messageSender = messageSender;
            _boltFeatures = boltFeatures;
        }

        public async Task ProcessMessageAsync(PeerMessage<InitMessage> request)
        {
            if (!_boltFeatures.ValidateRemoteFeatureAreCompatible(request.Message.Features,request.Message.GlobalFeatures))
                throw new ArgumentException(nameof(request.Message.Features)); //TODO David we need to define the way to close a connection gracefully 
         
            var peer = new Peer
            {
                Featurs = request.Message.Features,
                GlobalFeatures = request.Message.GlobalFeatures,
                NodeId = request.NodeId
            };
            
            await _repository.AddNewPeerAsync(peer);

            await _messageSender.SendMessageAsync(new PeerMessage<InitMessage>(request.NodeId, CreateInitMessage()));
            
            //TODO David add sending the gossip timestamp filter *init message MUST be sent first
        }

        private InitMessage CreateInitMessage()
        {
            return new InitMessage
            {
                GlobalFeatures = _boltFeatures.GetSupportedGlobalFeatures(),
                Features = _boltFeatures.GetSupportedFeatures(),
                Extension = new TlVStream
                {
                    Records = new List<TlvRecord>
                    {
                        new NetworksTlvRecord {Type = 1, Payload = ChainHashes.Bitcoin}
                    }
                }
            };
        }
    }
}