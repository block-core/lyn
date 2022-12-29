using Lyn.Types.Serialization;
using System;
using System.Buffers;

namespace Lyn.Protocol.Bolt4.Messages
{
    public class FailureMessageSerializer : IProtocolTypeSerializer<FailureMessage>
    {

        public int Serialize(FailureMessage typeInstance, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            int bytesWritten = 0;
            bytesWritten += typeInstance.Serialize(writer);
            return bytesWritten;
        }

        public FailureMessage Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {

            var failureMessageType = reader.ReadUShort();

            // todo: this seems so clunky?
            switch (failureMessageType)
            {
                case ((ushort)FailureMessageFlags.Permenant | 1):
                    return new InvalidRealmMessage();
                case ((ushort)FailureMessageFlags.Node | 2):
                    return new TemporaryNodeFailureMessage();
                case ((ushort)FailureMessageFlags.Permenant | (ushort)FailureMessageFlags.Node | 2):
                    return new PermanentNodeFailureMessage();
                case ((ushort)FailureMessageFlags.Permenant | (ushort)FailureMessageFlags.Node | 3):
                    return new RequiredNodeFeatureMissingMessage();
                case ((ushort)FailureMessageFlags.BadOnion | (ushort)FailureMessageFlags.Permenant | 4):
                    return new InvalidOnionVersionMessage(reader.ReadBytes(32).ToArray());
                    case ((ushort)FailureMessageFlags.BadOnion | (ushort)FailureMessageFlags.Permenant | 5):
                    return new InvalidOnionHmacMessage(reader.ReadBytes(32).ToArray());
                    case ((ushort)FailureMessageFlags.BadOnion | (ushort)FailureMessageFlags.Permenant | 6):
                    return new InvalidOnionKeyMessage(reader.ReadBytes(32).ToArray());
                    case ((ushort)FailureMessageFlags.BadOnion | (ushort)FailureMessageFlags.Permenant | 7):
                    return new InvalidOnionBlindingMessage(reader.ReadBytes(32).ToArray());
                default:
                    // todo: return UnknownFailureMessage
                    throw new NotImplementedException();
            }
        }

    }

}