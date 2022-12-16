using System;
using System.Buffers;
using System.Linq;
using Lyn.Types.Serialization;

namespace Lyn.Protocol.Bolt4
{

    [Flags]
    public enum FailureMessageFlags : ushort
    {
        // 0x8000 (BADONION) - the onion was invalid.
        BadOnion = 0x8000,
        // 0x4000 (PERM) - the failure is permanent.
        Permenant = 0x4000,
        // 0x2000 (NODE) - the failure was at the final node.
        Node = 0x2000,
        // 0x1000 (UPDATE) - there is a new channel_update enclosed.
        Update = 0x1000
    }

    public record FailureMessage(string Message, ushort Code)
    {
        // todo: is this the best way to do this? i prolly also need deserialize...
        public virtual int Serialize(IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            return writer.WriteUShort(Code);
        }
    }

    public record PermenantFailureMessage(string Message, ushort Code) : FailureMessage(Message, Code);

    // note: all onion failures are permenant as far as I can tell...
    // note: this is the closest we can get to the scala Perm and BadOnion traits
    public record BadOnionMessage(string Message, ushort Code, byte[] OnionHash) : PermenantFailureMessage(Message, Code)
    {
        public override int Serialize(IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            var bytesWritten = base.Serialize(writer, options);
            bytesWritten += writer.WriteBytes(OnionHash);
            return bytesWritten;
        }
    }

    public record NodeFailureMessage(string Message, ushort Code) : FailureMessage(Message, Code);

    public record PermenantNodeFailureMessage(string Message, ushort Code) : PermenantFailureMessage(Message, Code);
    
    // TODO: Add channel_update
    // public record ChannelUpdateFailureMessage(string Message, ushort Code, ChannelUpdate update) : FailureMessage(Message, Code);

    public record InvalidRealmMessage() : PermenantFailureMessage("realm was not understood by the processing node", ((ushort)FailureMessageFlags.Permenant | 1));

    public record TemporaryNodeFailureMessage() : NodeFailureMessage("general temporary failure of the processing node", ((ushort)FailureMessageFlags.Node | 2));

    public record PermanentNodeFailureMessage() : PermenantNodeFailureMessage("general permanent failure of the processing node", ((ushort)FailureMessageFlags.Permenant | (ushort)FailureMessageFlags.Node | 2));

    public record RequiredNodeFeatureMissingMessage() : PermenantNodeFailureMessage("the processing node does not support the required feature bit", ((ushort)FailureMessageFlags.Permenant | (ushort)FailureMessageFlags.Node | 3));

    public record InvalidOnionVersionMessage(byte[] OnionHash) : BadOnionMessage("onion version was not understood by the processing node", ((ushort)FailureMessageFlags.BadOnion | (ushort)FailureMessageFlags.Permenant | 4), OnionHash);

    public record InvalidOnionHmacMessage(byte[] OnionHash) : BadOnionMessage("onion HMAC was incorrect when it reached the processing node", ((ushort)FailureMessageFlags.BadOnion | (ushort)FailureMessageFlags.Permenant | 5), OnionHash);

    public record InvalidOnionKeyMessage(byte[] OnionHash) : BadOnionMessage("onion key was unparsable by the processing node", ((ushort)FailureMessageFlags.BadOnion | (ushort)FailureMessageFlags.Permenant | 6), OnionHash);

    public record InvalidOnionBlindingMessage(byte[] OnionHash) : BadOnionMessage("the blinded onion didn't match the processing node's requirements", ((ushort)FailureMessageFlags.BadOnion | (ushort)FailureMessageFlags.Permenant | 7), OnionHash);

    // todo: we need to add the channel_update logic to Lyn before we can implement this
    // public record TemporaryChannelFailureMessage(ChannelUpdate update) : ChannelUpdateFailureMessage("channel ${update.shortChannelId} is currently unavailable", ((ushort)FailureMessageFlags.Update | 8), update);

    public record PermanentChannelFailureMessage() : PermenantFailureMessage("channel is permanently unavailable", ((ushort)FailureMessageFlags.Permenant | (ushort)FailureMessageFlags.Update | 9));

    public record RequiredChannelFeatureMissingMessage() : PermenantFailureMessage("channel requires features not present in the onion", ((ushort)FailureMessageFlags.Permenant | (ushort)FailureMessageFlags.Update | 10));

    public record UnknownNextPeerMessage() : PermenantFailureMessage("the next peer in the route was not known", ((ushort)FailureMessageFlags.Permenant | (ushort)FailureMessageFlags.Update | 11));

    // todo: we need to add the channel_update logic to Lyn before we can implement this
    // public record AmountBelowMinimumMessage(MilliSatoshis amount, ChannelUpdate update) : ChannelUpdateFailureMessage("amount is below the minimum amount allowed", ((ushort)FailureMessageFlags.Update | 12), update);
    // public record FeeInsufficientMessage(MilliSatoshis amount, ChannelUpdate update) : ChannelUpdateFailureMessage("fee is insufficient", ((ushort)FailureMessageFlags.Update | 13), update);

    public record TrampolineFeeInsufficientMessage() : PermenantFailureMessage("payment fee was below the minimum required by the trampoline node", ((ushort)FailureMessageFlags.Permenant | (ushort)FailureMessageFlags.Update | 14));

    // public record ChannelUpdateFailureMessage() : PermenantFailureMessage("channel is currently disabled", ((ushort)FailureMessageFlags.Permenant | (ushort)FailureMessageFlags.Channel | 15));

    // public record IncorrectCltvExpiryMessage(CltvExpiry expiry, ChannelUpdate update) : ChannelUpdateFailureMessage("incorrect CLTV expiry", ((ushort)FailureMessageFlags.Update | 16), update);

    // public record ExpiryTooSoonMessage(CltvExpiry expiry, ChannelUpdate update) : ChannelUpdateFailureMessage("expiry is too close to the current block height for safe handling by the relaying node", ((ushort)FailureMessageFlags.Update | 17), update);

    public record TrampolineExpiryTooSoonMessage() : NodeFailureMessage("expiry is too close to the current block height for safe handling by the relaying node", 18);

    // public record FinalIncorrectCltvExpiryMessage(CltvExpiry expiry) : NodeFailureMessage("payment expiry doesn't match the value in the onion", 19);

    // public record FinalIncorrectHtlcAmountMessage(MiliSatoshi amount) : NodeFailureMessage("payment amount doesn't match the value in the onion", 20);

    public record ExpiryTooFarMessage() : NodeFailureMessage("payment expiry is too fari nt he future", 21);

    public record InvaldOnionPayloadMessage(ulong Tag, ushort Offset) : PermenantFailureMessage("nion per-hop payload is invalid", ((ushort)FailureMessageFlags.Permenant | 22));

    public record PaymentTimeoutMessage() : FailureMessage("the complete payment amount was not received within a reasonable time", 23);

}