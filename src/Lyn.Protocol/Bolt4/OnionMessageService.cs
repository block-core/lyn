using Lyn.Protocol.Bolt1.Messages;
using Lyn.Protocol.Bolt4.Entities;
using Lyn.Protocol.Bolt4.Messages;
using Lyn.Protocol.Bolt8;
using Lyn.Protocol.Bolt9;
using Lyn.Protocol.Common.Messages;
using Lyn.Protocol.Connection;
using Lyn.Types.Fundamental;
using NaCl.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NBitcoin.Secp256k1;
using Lyn.Protocol.Common.Crypto;
using System.Buffers;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt4
{
    public class OnionMessageService : IBoltMessageService<OnionMessage>
    {
        private readonly IBoltFeatures _boltFeatures;
        private readonly IEllipticCurveActions _ellipticCurveActions;
        private readonly ICipherFunction _cipherFunctions;
        private readonly ISphinx _sphinx;

        // todo: need an actual way to surface this!
        private PrivateKey _nodePrivatekey;

        public OnionMessageService(IBoltFeatures boltFeatures, 
                                   IEllipticCurveActions ellipticCurveActions, 
                                   ICipherFunction cipherFunctions,
                                   ISphinx sphinx)
        {
            _boltFeatures = boltFeatures;
            _ellipticCurveActions = ellipticCurveActions;
            _cipherFunctions = cipherFunctions;
            _sphinx = sphinx;
        }

        public Task<MessageProcessingOutput> ProcessMessageAsync(PeerMessage<OnionMessage> message)
        {
            if (_boltFeatures.SupportedFeatures.HasFlag(Features.OptionOnionMessagesRequired))
            {
                // todo: verify payload bytes aren't too large

                // else, if payload is correct sized
                var blindedPrivateKey = _sphinx.DeriveBlindedPrivateKey(_nodePrivatekey, message.MessagePayload.BlindingKey);

                // peel the onion
                _sphinx.PeelOnion(blindedPrivateKey, null, message.MessagePayload.OnionPacket);
            }

            throw new NotImplementedException();
        }
    }
}
