namespace Lyn.Protocol.Bolt1.Messages
{
    public enum MessageType : ushort
    {
        //Setup & Control
        Init = 16,
        Error = 17,
        Ping = 18,
        Pong = 19,
        
        //Channel
        OpenChannel = 32,
        AcceptChannel = 33,
        FundingCreated = 34,
        FundingSigned = 35,
        FundingLocked = 36,
        Shutdown = 38,
        ClosingSigned = 39,
        
        //Commitment
        UpdateAddHtlc = 128,
        UpdateFulfillHtlc = 130,
        UpdateFailHtlc = 131,
        CommitmentSigned = 132,
        RevokeAndAck = 133,
        UpdateFee = 134,
        UpdateFailMalformedHtlc = 135,
        ChannelReestablish = 136,
        
        //Routing
        ChannelAnnouncement = 256,
        NodeAnnouncement = 257,
        AnnouncementSignatures = 259,
        QueryShortChannelIds = 261,
        GossipTimestampFilter = 265,
    }
}