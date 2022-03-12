using Lyn.Protocol.Bolt4.Entities;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt4
{
    public class OnionRoutingPacketSerializerTests
    {

        [Fact]
        public void SmallOnion_Serialization()
        {
            var onionToSerialize = new OnionRoutingPacket();
            onionToSerialize.Version = 1;
            onionToSerialize.EphemeralKey = ByteArray.FromHex("032c0b7cf95324a07d05398b240174dc0c2be444d96b159aa6c7f7b1e668680991");
            onionToSerialize.PayloadData = ByteArray.FromHex("0012345679abcdef");
            onionToSerialize.Hmac = ByteArray.FromHex("0000111122223333444455556666777788889999aaaabbbbccccddddeeee0000");

            var bufferWriter = new ArrayBufferWriter<byte>(256);

            var serializer = new OnionRoutingPacketSerializer();
            int bytesWritten = serializer.Serialize(onionToSerialize, bufferWriter, null);
            var onionBytes = bufferWriter.WrittenSpan.ToArray();

            Assert.Equal(bytesWritten, onionBytes.Length);
            // todo: figure out what to do about varint length here??
            Assert.Equal(ByteArray.FromHex("004a01032c0b7cf95324a07d05398b240174dc0c2be444d96b159aa6c7f7b1e6686809910012345679abcdef0000111122223333444455556666777788889999aaaabbbbccccddddeeee0000"), onionBytes);
        }

    }
}
