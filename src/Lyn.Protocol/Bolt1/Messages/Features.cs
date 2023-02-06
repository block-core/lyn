using System;

namespace Lyn.Protocol.Bolt1.Messages
{
    [Flags]
    public enum Features : ulong
    {
        OptionDataLossProtectRequired = 1 << 0,
        OptionDataLossProtect = 1 << 1,
        NotSupported = 1 << 2,
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
        
        optionShutdownAnysegwitRequired = 1 << 26,
        optionShutdownAnysegwit = 1 << 27,
        
        
        optionChannelTypeRequired = (ulong)1 << 44,
        optionChannelType = (ulong)1 << 45,
        optionScidAliasRequired = (ulong)1 << 46,
        optionScidAlias = (ulong)1 << 47,
        optionPaymentMetadataRequired = (ulong)1 << 48,
        optionPaymentMetadata = (ulong)1 << 49,
        optionZeroconfRequired = (ulong)1 << 50,
        optionZeroconf = (ulong)1 << 51
    }
}