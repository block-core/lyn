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
            private readonly IParseFeatureFlags _featureFlags;

            public InitMessageService(IPeerRepository repository, IBoltMessageSender<InitMessage> messageSender, 
            IBoltFeatures features, IParseFeatureFlags featureFlags)
        {
            _repository = repository ?? throw new ArgumentNullException();
            _messageSender = messageSender ?? throw new ArgumentNullException();
            _boltFeatures = features ?? throw new ArgumentNullException();
            _featureFlags = featureFlags ?? throw new ArgumentNullException();
        }

        public async Task ProcessMessageAsync(PeerMessage<InitMessage> request)
        {
            var peer = new Peer
            {
                Featurs = request.Message.Features,
                GlobalFeatures = request.Message.GlobalFeatures,
                NodeId = request.NodeId
            };

            var remoteNodeFeatures = _featureFlags.ParseFeatures(request.Message.Features) |
                                     _featureFlags.ParseFeatures(request.Message.GlobalFeatures);

            var remoteFeaturesBytes = _featureFlags.ParseFeatures(remoteNodeFeatures);

            if (!_boltFeatures.ValidateRemoteFeatureAreCompatible(remoteFeaturesBytes))
                throw new ArgumentException(nameof(remoteNodeFeatures)); //TODO David we need to define the way to close a connection gracefully 
                
            await _repository.AddNewPeerAsync(peer);

            await _messageSender.SendMessageAsync(new PeerMessage<InitMessage>
            {
                Message = CreateInitMessage(),
                NodeId = request.NodeId
            });
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