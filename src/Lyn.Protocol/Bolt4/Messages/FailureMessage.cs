using System;
using System.Buffers;
using System.Linq;

namespace Lyn.Protocol.Bolt4
{

    [Flags]
    internal enum FailureMessageFlags : int
    {
        // 0x8000 (BADONION) - the onion was invalid.
        BadOnion = 0x8000,
        // 0x4000 (PERM) - the failure is permanent.
        Permenant = 0x4000,
        // 0x2000 (NODE) - the failure was at the final node.
        Node = 0x2000,
        // 0x1000 (UPDATE) - there is a new channel_update enclosed.
        Update = 0x1000,
        // 0x0800 (IGNORED) - the processing node does not wish to reveal the reason for the failure.
        Ignored = 0x0800,
        // 0x0400 (CHANNEL) - the failure was caused by an invalid channel_update.
        Channel = 0x0400
    }

    public record FailureMessage(string Message, int Code);

    public record PermenantFailureMessage(string Message, int Code) : FailureMessage(Message, Code);

    // note: all onion failures are permenant as far as I can tell...
    // note: this is the closest we can get to the scala Perm and BadOnion traits
    public record BadOnionMessage(string Message, int Code, byte[] OnionHash) : PermenantFailureMessage(Message, Code);

    public record NodeFailureMessage(string Message, int Code) : FailureMessage(Message, Code);

    public record PermenantNodeFailureMessage(string Message, int Code) : PermenantFailureMessage(Message, Code);
    
    // TODO: Add channel_update
    // public record ChannelUpdateFailureMessage(string Message, int Code, ChannelUpdate update) : FailureMessage(Message, Code);

    public record InvalidRealmMessage() : PermenantFailureMessage("realm was not understood by the processing node", ((int)FailureMessageFlags.Permenant | 1));

    public record TemporaryNodeFailureMessage() : NodeFailureMessage("general temporary failure of the processing node", ((int)FailureMessageFlags.Node | 2));

    public record PermanentNodeFailureMessage() : PermenantNodeFailureMessage("general permanent failure of the processing node", ((int)FailureMessageFlags.Permenant | (int)FailureMessageFlags.Node | 2));

    public record RequiredNodeFeatureMissingMessage() : PermenantNodeFailureMessage("the processing node does not support the required feature bit", ((int)FailureMessageFlags.Permenant | (int)FailureMessageFlags.Node | 3));

    public record InvalidOnionVersionMessage(byte[] OnionHash) : BadOnionMessage("onion version was not understood by the processing node", ((int)FailureMessageFlags.BadOnion | (int)FailureMessageFlags.Permenant | 4), OnionHash);

    public record InvalidOnionHmacMessage(byte[] OnionHash) : BadOnionMessage("onion HMAC was incorrect when it reached the processing node", ((int)FailureMessageFlags.BadOnion | (int)FailureMessageFlags.Permenant | 5), OnionHash);

    public record InvalidOnionKeyMessage(byte[] OnionHash) : BadOnionMessage("onion key was unparsable by the processing node", ((int)FailureMessageFlags.BadOnion | (int)FailureMessageFlags.Permenant | 6), OnionHash);

    public record InvalidOnionBlindingMessage(byte[] OnionHash) : BadOnionMessage("the blinded onion didn't match the processing node's requirements", ((int)FailureMessageFlags.BadOnion | (int)FailureMessageFlags.Permenant | 7), OnionHash);

    // todo: we need to add the channel_update logic to Lyn before we can implement this
    // public record TemporaryChannelFailureMessage(ChannelUpdate update) : ChannelUpdateFailureMessage("channel ${update.shortChannelId} is currently unavailable", ((int)FailureMessageFlags.Update | 8), update);

    public record PermanentChannelFailureMessage() : PermenantFailureMessage("channel is permanently unavailable", ((int)FailureMessageFlags.Permenant | (int)FailureMessageFlags.Channel | 9));

    public record RequiredChannelFeatureMissingMessage() : PermenantFailureMessage("channel requires features not present in the onion", ((int)FailureMessageFlags.Permenant | (int)FailureMessageFlags.Channel | 10));

    public record UnknownNextPeerMessage() : PermenantFailureMessage("the next peer in the route was not known", ((int)FailureMessageFlags.Permenant | (int)FailureMessageFlags.Channel | 11));

    // todo: we need to add the channel_update logic to Lyn before we can implement this
    // public record AmountBelowMinimumMessage(MilliSatoshis amount, ChannelUpdate update) : ChannelUpdateFailureMessage("amount is below the minimum amount allowed", ((int)FailureMessageFlags.Update | 12), update);
    // public record FeeInsufficientMessage(MilliSatoshis amount, ChannelUpdate update) : ChannelUpdateFailureMessage("fee is insufficient", ((int)FailureMessageFlags.Update | 13), update);

    public record TrampolineFeeInsufficientMessage() : PermenantFailureMessage("payment fee was below the minimum required by the trampoline node", ((int)FailureMessageFlags.Permenant | (int)FailureMessageFlags.Channel | 14));

    // public record ChannelUpdateFailureMessage() : PermenantFailureMessage("channel is currently disabled", ((int)FailureMessageFlags.Permenant | (int)FailureMessageFlags.Channel | 15));

    // public record IncorrectCltvExpiryMessage(CltvExpiry expiry, ChannelUpdate update) : ChannelUpdateFailureMessage("incorrect CLTV expiry", ((int)FailureMessageFlags.Update | 16), update);

    // public record ExpiryTooSoonMessage(CltvExpiry expiry, ChannelUpdate update) : ChannelUpdateFailureMessage("expiry is too close to the current block height for safe handling by the relaying node", ((int)FailureMessageFlags.Update | 17), update);

    public record TrampolineExpiryTooSoonMessage() : NodeFailureMessage("expiry is too close to the current block height for safe handling by the relaying node", 18);

    // public record FinalIncorrectCltvExpiryMessage(CltvExpiry expiry) : NodeFailureMessage("payment expiry doesn't match the value in the onion", 19);

    // public record FinalIncorrectHtlcAmountMessage(MiliSatoshi amount) : NodeFailureMessage("payment amount doesn't match the value in the onion", 20);

    public record ExpiryTooFarMessage() : NodeFailureMessage("payment expiry is too fari nt he future", 21);

    public record InvaldOnionPayloadMessage(ulong Tag, int Offset) : PermenantFailureMessage("nion per-hop payload is invalid", ((int)FailureMessageFlags.Permenant | 22));

    public record PaymentTimeoutMessage() : FailureMessage("the complete payment amount was not received within a reasonable time", 23);

}