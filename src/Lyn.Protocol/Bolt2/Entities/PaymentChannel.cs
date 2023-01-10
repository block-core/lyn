using System;
using System.Collections.Generic;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Types.Bitcoin;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt2.Entities
{
    public class PaymentChannel
    {
        public PaymentChannel(UInt256 channelId, ShortChannelId shortChannelId,CompressedSignature compressedSignature,  PublicKey perCommitmentPoint, 
            PublicKey[] previousPerCommitmentPoints, Satoshis fundingSatoshis, OutPoint inPoint, Satoshis localDustLimitSatoshis, 
            Satoshis remoteDustLimitSatoshis, Satoshis feeRatePerKw, PublicKey localFundingKey, PublicKey remoteFundingKey, 
            MiliSatoshis pushMsat, Basepoints localBasePoints, Basepoints remoteBasePoints)
        {
            ChannelId = channelId;
            ShortChannelId = shortChannelId;
            PerCommitmentPoint = perCommitmentPoint;
            PreviousPerCommitmentPoints = previousPerCommitmentPoints;
            FundingSatoshis = fundingSatoshis;
            InPoint = inPoint;
            LocalDustLimitSatoshis = localDustLimitSatoshis;
            RemoteDustLimitSatoshis = remoteDustLimitSatoshis;
            FeeratePerKw = feeRatePerKw;
            LocalFundingKey = localFundingKey;
            RemoteFundingKey = remoteFundingKey;
            PushMsat = pushMsat;
            LocalBasePoints = localBasePoints;
            RemoteBasePoints = remoteBasePoints;
            FundingRemoteSignature = compressedSignature;
            PreviousPerCommitmentSecrets = Array.Empty<Secret>();
        }

        public UInt256 ChannelId { get; set; }

        public ShortChannelId ShortChannelId { get; set; } //TODO David

        public PublicKey PerCommitmentPoint { get; set; }

        public PublicKey[] PreviousPerCommitmentPoints { get; set; } //TODO David

        public Secret[] PreviousPerCommitmentSecrets { get; set; }

        public CompressedSignature FundingRemoteSignature { get; set; }

        
        
        public Satoshis FundingSatoshis { get; set; }
        
        public ulong LocalCommitmentNumber { get; set; }
        public ulong RemoteCommitmentNumber { get; set; }
        public OutPoint InPoint { get; set; }
        
        public Satoshis LocalDustLimitSatoshis { get; set; }
        public Satoshis RemoteDustLimitSatoshis { get; set; }
        
        public Satoshis FeeratePerKw { get; set; }
        
        public PublicKey LocalFundingKey { get; set; }
        public PublicKey RemoteFundingKey { get; set; }
        
        public bool OptionAnchorOutputs { get; set; }
        
        public MiliSatoshis PushMsat { get; set; }
        
        public ushort LocalToSelfDelay { get; set; }
        public ushort RemoteToSelfDelay { get; set; }

        public bool WasChannelInitiatedLocally { get; set; }

        public ulong CnObscurer { get; set; }

        
        public Basepoints LocalBasePoints { get; set; }
        public Basepoints RemoteBasePoints { get; set; }


        public bool ChannelShutdownTriggered { get; set; }
        public bool ChannelClosingSignSent  => CloseChannelDetails != null;

        // public Keyset Keyset { get; set; }
        // public MiliSatoshis SelfPayMsat { get; set; }
        // public MiliSatoshis OtherPayMsat { get; set; }
         public List<Htlc> Htlcs { get; set; }
         public List<Htlc> PendingHtlcs { get; set; }

         public CloseChannelDetails? CloseChannelDetails { get; set; }
    }
}