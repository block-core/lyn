using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Bolt1.Messages.TlvRecords;
using Lyn.Protocol.Bolt7;
using Lyn.Protocol.Bolt7.Entities;
using Lyn.Protocol.Bolt9;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt1
{
    public class InitMessageService : IBoltMessageService<InitMessage>, IInitMessageAction
    {
        private readonly IPeerRepository _repository;
        private readonly IBoltFeatures _boltFeatures;
        private readonly IParseFeatureFlags _featureFlags;

        private readonly IGossipRepository _gossipRepository;

        public InitMessageService(IPeerRepository repository,
        IBoltFeatures boltFeatures, IParseFeatureFlags featureFlags, IGossipRepository gossipRepository)
        {
            _repository = repository;
            _boltFeatures = boltFeatures;
            _featureFlags = featureFlags;
            _gossipRepository = gossipRepository;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<InitMessage> request)
        {
            if (!_boltFeatures.ValidateRemoteFeatureAreCompatible(request.MessagePayload.Features, request.MessagePayload.GlobalFeatures))
                throw new ArgumentException(nameof(request.MessagePayload.Features)); //TODO David we need to define the way to close a connection gracefully

            var peer = await _repository.TryGetPeerAsync(request.NodeId);
            
            peer ??= new Peer {NodeId = request.NodeId};

            peer.Features = _featureFlags.ParseFeatures(request.MessagePayload.Features);
            peer.GlobalFeatures = _featureFlags.ParseFeatures(request.MessagePayload.GlobalFeatures);
            peer.MutuallySupportedFeatures = peer.Features & _boltFeatures.SupportedFeatures;
            
            await _repository.AddOrUpdatePeerAsync(peer);

            await _gossipRepository.AddNodeAsync(new GossipNode(request.NodeId));
            
            return new SuccessWithOutputResponse(CreateInitMessage());

            //TODO David add sending the gossip timestamp filter *init message MUST be sent first
        }

        private BoltMessage CreateInitMessage()
        {
            return new()
            {
                Payload = new InitMessage
                {
                    GlobalFeatures = _boltFeatures.GetSupportedGlobalFeatures(),
                    Features = _boltFeatures.GetSupportedFeatures(),    
                },
                Extension = new TlVStream
                {
                    Records = new List<TlvRecord>
                    {
                        new NetworksTlvRecord {Type = 1, Payload = ChainHashes.BitcoinRegTest.GetBytes().ToArray(), Size = 32}
                    }
                }
            };
        }

        public async Task<MessageProcessingOutput> GenerateInitAsync(PublicKey nodeId, CancellationToken token)
        {
            var response = new SuccessWithOutputResponse(CreateInitMessage());

            if (_repository.PeerExists(nodeId)) 
                return response;

            var peer = new Peer {NodeId = nodeId};

            await _repository.AddNewPeerAsync(peer);

            return response;
        }
    }
}