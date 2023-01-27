using System.Collections.Generic;
using Lyn.Protocol.Bolt2.ChannelEstablishment.Messages;
using Lyn.Protocol.Bolt3;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment
{
    public class CommitmentTransactionBuilder : ICommitmentTransactionBuilder
    {
        private readonly ILightningKeyDerivation _lightningKeyDerivation;
        private readonly ILightningScripts _lightningScripts;
        private readonly ILightningTransactions _lightningTransactions;
        
        private OpenChannel _openChannel;
        private AcceptChannel _acceptChannel;
        private bool _anchorOutputs;
        private bool _statisRemoteKey;
        private ChannelSide _channelFundgingSide;
        private OutPoint _outpoint;

        public CommitmentTransactionBuilder(ILightningKeyDerivation lightningKeyDerivation, ILightningScripts lightningScripts, ILightningTransactions lightningTransactions)
        {
            _lightningKeyDerivation = lightningKeyDerivation;
            _lightningScripts = lightningScripts;
            _lightningTransactions = lightningTransactions;
        }

        public ICommitmentTransactionBuilder WithOpenChannel(OpenChannel openChannel)
        {
            _openChannel = openChannel;
            return this;
        }

        public ICommitmentTransactionBuilder WithAcceptChannel(AcceptChannel acceptChannel)
        {
            _acceptChannel = acceptChannel;
            return this;
        }

        public ICommitmentTransactionBuilder WithAnchorOutputs()
        {
            _anchorOutputs = true;
            return this;
        }

        public ICommitmentTransactionBuilder WithStaticRemoteKey()
        {
            _statisRemoteKey = true;
            return this;
        }

        public ICommitmentTransactionBuilder WithFundingSide(ChannelSide side)
        {
            _channelFundgingSide = side;
            return this;
        }

        public ICommitmentTransactionBuilder WithFundingOutpoint(OutPoint point)
        {
            _outpoint = point;
            return this;
        }

        public CommitmenTransactionOut BuildRemoteCommitmentTransaction()
        {
            // generate the commitment transaction how it will look like for the other side

            var isFundingNodeRemote = _channelFundgingSide == ChannelSide.Remote;
            
            var commitmentTransactionIn = new CommitmentTransactionIn
            {
                Funding = _openChannel.FundingSatoshis,
                Htlcs = new List<Htlc>(),
                Opener = _channelFundgingSide,
                Side = ChannelSide.Remote,
                CommitmentNumber = 0,
                FundingTxout = _outpoint,
                DustLimitSatoshis = isFundingNodeRemote ? _openChannel.DustLimitSatoshis : _acceptChannel.DustLimitSatoshis,
                FeeratePerKw = _openChannel.FeeratePerKw,
                LocalFundingKey = isFundingNodeRemote ? _openChannel.FundingPubkey :_acceptChannel.FundingPubkey,
                OptionAnchorOutputs = _anchorOutputs,
                OtherPayMsat = ((MiliSatoshis)_openChannel.FundingSatoshis) - _openChannel.PushMsat,
                RemoteFundingKey = isFundingNodeRemote ? _acceptChannel.FundingPubkey : _openChannel.FundingPubkey,
                SelfPayMsat = _openChannel.PushMsat,
                ToSelfDelay = isFundingNodeRemote ? _openChannel.ToSelfDelay : _acceptChannel.ToSelfDelay,
                CnObscurer = _lightningScripts.CommitNumberObscurer(_openChannel.PaymentBasepoint,
                    _acceptChannel.PaymentBasepoint)
            };

            var localBasePoints = isFundingNodeRemote ? _openChannel.GetBasePoints() : _acceptChannel.GetBasePoints();
            
            var remoteBaseOoints = isFundingNodeRemote ? _acceptChannel.GetBasePoints() : _openChannel.GetBasePoints();

            var perCommitmentPoint = isFundingNodeRemote ? _openChannel.FirstPerCommitmentPoint : _acceptChannel.FirstPerCommitmentPoint;

            commitmentTransactionIn.Keyset = GetKeysDerivations(localBasePoints, remoteBaseOoints, perCommitmentPoint, _statisRemoteKey);

            return _lightningTransactions.CommitmentTransaction(commitmentTransactionIn);
        }

        public CommitmenTransactionOut BuildLocalCommitmentTransaction()
        {
            var isFundingNodeLocal = _channelFundgingSide == ChannelSide.Local;
            
            var commitmentTransactionIn = new CommitmentTransactionIn
            {
                Funding = _openChannel.FundingSatoshis,
                Htlcs = new List<Htlc>(),
                Opener = _channelFundgingSide,
                Side = ChannelSide.Local,
                CommitmentNumber = 0,
                FundingTxout = _outpoint,
                DustLimitSatoshis = isFundingNodeLocal ? _openChannel.DustLimitSatoshis : _acceptChannel.DustLimitSatoshis,
                FeeratePerKw = _openChannel.FeeratePerKw,
                LocalFundingKey = isFundingNodeLocal ? _openChannel.FundingPubkey : _acceptChannel.FundingPubkey,
                RemoteFundingKey = isFundingNodeLocal ? _acceptChannel.FundingPubkey : _openChannel.FundingPubkey,
                OptionAnchorOutputs = _anchorOutputs,
                OtherPayMsat = _openChannel.PushMsat,
                SelfPayMsat = ((MiliSatoshis)_openChannel.FundingSatoshis) - _openChannel.PushMsat,
                ToSelfDelay = isFundingNodeLocal ? _acceptChannel.ToSelfDelay : _openChannel.ToSelfDelay,
                CnObscurer = _lightningScripts.CommitNumberObscurer(
                    _openChannel.PaymentBasepoint,
                    _acceptChannel.PaymentBasepoint)
            };

            var localBasePoints = isFundingNodeLocal ? _openChannel.GetBasePoints() : _acceptChannel.GetBasePoints();
            
            var remoteBasePoints = isFundingNodeLocal ? _acceptChannel.GetBasePoints() : _openChannel.GetBasePoints();

            var firstPerCommitmentPoint = isFundingNodeLocal
                ? _openChannel.FirstPerCommitmentPoint
                : _acceptChannel.FirstPerCommitmentPoint;

            commitmentTransactionIn.Keyset = GetKeysDerivations(localBasePoints, remoteBasePoints,
                firstPerCommitmentPoint, _statisRemoteKey);

            return _lightningTransactions.CommitmentTransaction(commitmentTransactionIn);
        }
        
        private Keyset GetKeysDerivations(Basepoints localBasePoints, Basepoints remoteBasePoints, PublicKey perCommitmentPoint, bool optionStaticRemoteKey)
        {
            var remoteRevocationKey = _lightningKeyDerivation.DeriveRevocationPublicKey(remoteBasePoints.Revocation, perCommitmentPoint);

            var localDelayedPaymentKey = _lightningKeyDerivation.DerivePublickey(localBasePoints.DelayedPayment, perCommitmentPoint);

            var remotePaymentKey = optionStaticRemoteKey 
                ? remoteBasePoints.Payment 
                : _lightningKeyDerivation.DerivePublickey(remoteBasePoints.Payment, perCommitmentPoint);

            var remoteHtlckey = _lightningKeyDerivation.DerivePublickey(remoteBasePoints.Htlc, perCommitmentPoint);
            var localHtlckey = _lightningKeyDerivation.DerivePublickey(localBasePoints.Htlc, perCommitmentPoint);

            return new Keyset(remoteRevocationKey, localHtlckey, remoteHtlckey, localDelayedPaymentKey, remotePaymentKey);
        }
    }
}