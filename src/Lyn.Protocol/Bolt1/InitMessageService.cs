using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lyn.Protocol.Bolt1.Entities;
using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Bolt1.Messages.TlvRecords;
using Lyn.Protocol.Bolt9;
using Lyn.Protocol.Common;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt1
{
    public class InitMessageService : IBoltMessageService<InitMessage>, IInitMessageAction
    {
        private readonly IPeerRepository _repository;
        private readonly IBoltMessageSender<InitMessage> _messageSender;
        private readonly IBoltFeatures _boltFeatures;
        private readonly IParseFeatureFlags _featureFlags;

        public InitMessageService(IPeerRepository repository, IBoltMessageSender<InitMessage> messageSender,
        IBoltFeatures boltFeatures, IParseFeatureFlags featureFlags)
        {
            _repository = repository;
            _messageSender = messageSender;
            _boltFeatures = boltFeatures;
            _featureFlags = featureFlags;
        }

        public async Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<InitMessage> request)
        {
            if (!_boltFeatures.ValidateRemoteFeatureAreCompatible(request.MessagePayload.Features, request.MessagePayload.GlobalFeatures))
                throw new ArgumentException(nameof(request.MessagePayload.Features)); //TODO David we need to define the way to close a connection gracefully

            var peer = _repository.TryGetPeerAsync(request.NodeId);
            
            var peerExists = peer != null;

            peer ??= new Peer {NodeId = request.NodeId};

            peer.Featurs = _featureFlags.ParseFeatures(request.MessagePayload.Features);
            peer.GlobalFeatures = _featureFlags.ParseFeatures(request.MessagePayload.GlobalFeatures);

            await _repository.AddOrUpdatePeerAsync(peer);

            return new MessageProcessingOutput
            {
                Success = true,
                ResponseMessages = peerExists ? null : new[] {CreateInitMessage()}
            };

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
                        new NetworksTlvRecord {Type = 1, Payload = ChainHashes.Bitcoin, Size = 32}
                    }
                }
            };
        }

        public async Task SendInitAsync(PublicKey nodeId, CancellationToken token)
        {
            if (!_repository.PeerExists(nodeId)) //Completed handshake and sending first init or replying to init from responder
            {
                var peer = new Peer {NodeId = nodeId};

                await _repository.AddNewPeerAsync(peer);                
            }
            
            var peerMessage = new PeerMessage<InitMessage>(nodeId, CreateInitMessage());

            await _messageSender.SendMessageAsync(peerMessage);
        }
    }
}