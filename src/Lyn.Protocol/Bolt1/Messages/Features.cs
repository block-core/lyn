using System;

namespace Lyn.Protocol.Bolt1.Messages
{
    [Flags]
    public enum Features : ulong 
    {
        OptionDataLossProtect = 0,
        OptionDataLossProtectRequired = 1 << 0,
        InitialRoutingSync = 1 << 3,
        OptionUpfrontShutdownScript = 1 << 4,
        OptionUpfrontShutdownScriptRequired = 1 << 5,
        GossipQueries = 1 << 6,
        GossipQueriesRequired = 1 << 7,
        VarOnionOptin = 1 << 8,
        VarOnionOptinRequired = 1 << 9,
        GossipQueriesEx = 1 << 10,
        GossipQueriesExRequired = 1 << 11,
        OptionStaticRemotekey = 1 << 12,
        OptionStaticRemotekeyRequired = 1 << 13,
        PaymentSecret = 1 << 14,
        PaymentSecretRequired = 1 << 15,
        BasicMpp = 1 << 16,
        BasicMppRequired = 1 << 17,
        OptionSupportLargeChannel = 1 << 18,
        OptionSupportLargeChannelRequired = 1 << 19,
        OptionAnchorOutputs = 1 << 20,
        OptionAnchorOutputsRequired = 1 << 21,
        OptionAnchorsZeroFeeHtlcTx = 1 << 22,
        OptionAnchorsZeroFeeHtlcTxRequired = 1 << 23,
    }
}