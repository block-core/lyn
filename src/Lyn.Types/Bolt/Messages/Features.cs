using System;

namespace Lyn.Types.Bolt.Messages
{
    [Flags]
    public enum Features : ulong 
    {
        OptionDataLossProtect = 0,
        OptionDataLossProtectRequired = 1 << 0,
        InitialRoutingSync = 1 << 2,
        OptionUpfrontShutdownScript = 1 << 3,
        OptionUpfrontShutdownScriptRequired = 1 << 4,
        GossipQueries = 1 << 5,
        GossipQueriesRequired = 1 << 6,
        VarOnionOptin = 1 << 7,
        VarOnionOptinRequired = 1 << 8,
        GossipQueriesEx = 1 << 9,
        GossipQueriesExRequired = 1 << 10,
        OptionStaticRemotekey = 1 << 11,
        OptionStaticRemotekeyRequired = 1 << 12,
        PaymentSecret = 1 << 13,
        PaymentSecretRequired = 1 << 14,
        BasicMpp = 1 << 15,
        BasicMppRequired = 1 << 16,
        OptionSupportLargeChannel = 1 << 17,
        OptionSupportLargeChannelRequired = 1 << 18,
        OptionAnchorOutputs = 1 << 19,
        OptionAnchorOutputsRequired = 1 << 20,
        OptionAnchorsZeroFeeHtlcTx = 1 << 21,
        OptionAnchorsZeroFeeHtlcTxRequired = 1 << 22,
    }
}