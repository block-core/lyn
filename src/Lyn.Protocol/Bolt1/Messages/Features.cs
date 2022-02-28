using System;

namespace Lyn.Protocol.Bolt1.Messages
{
    [Flags]
    public enum Features : ulong 
    {
        OptionDataLossProtect = 0,
        OptionDataLossProtectRequired = 1 << 0,
        InitialRoutingSync = 1 << 3,
        OptionUpfrontShutdownScriptRequired = 1 << 4,
        OptionUpfrontShutdownScript = 1 << 5,
        GossipQueriesRequired = 1 << 6,
        GossipQueries = 1 << 7,
        VarOnionOptinRequired = 1 << 8,
        VarOnionOptin = 1 << 9,
        GossipQueriesExRequired = 1 << 10,
        GossipQueriesEx = 1 << 11,
        OptionStaticRemotekeyRequired = 1 << 12,
        OptionStaticRemotekey = 1 << 13,
        PaymentSecretRequired = 1 << 14,
        PaymentSecret = 1 << 15,
        BasicMppRequired = 1 << 16,
        BasicMpp = 1 << 17,
        OptionSupportLargeChannelRequired = 1 << 18,
        OptionSupportLargeChannel = 1 << 19,
        OptionAnchorOutputsRequired = 1 << 20,
        OptionAnchorOutputs = 1 << 21,
        OptionAnchorsZeroFeeHtlcTxRequired = 1 << 22,
        OptionAnchorsZeroFeeHtlcTx = 1 << 23,
        OptionOnionMessagesRequired = 1 << 38,
        OptionOnionMessages = 1 << 39
    }
}