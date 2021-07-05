using System.Buffers;
using System.IO;
using System.Text.Json;
using Lyn.Types;
using Lyn.Types.Serialization;
using Xunit;
using SequenceReaderExtensions = Lyn.Types.Serialization.SequenceReaderExtensions;

namespace Lyn.Protocol.Tests.Bolt1
{
    public class TlvBigSizeTest
    {
        [Fact]
        public void BigSizeDecodingDataTest()
        {
            string rawData = File.ReadAllText("Bolt1/Data/BigSizeDecodingData.json");
            TlvData[]? data = JsonSerializer.Deserialize<TlvData[]>(rawData);

            foreach (TlvData tlvData in data)
            {
                byte[] dataBytes = Hex.FromString(tlvData.bytes) ?? new byte[0];

                if (tlvData.exp_error != null)
                {
                    Assert.Throws<MessageSerializationException>(() =>
                    {
                        var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(dataBytes));
                        return SequenceReaderExtensions.ReadBigSize(ref reader);
                    });
                }
                else
                {
                    var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(dataBytes));
                    ulong res = SequenceReaderExtensions.ReadBigSize(ref reader);
                    Assert.Equal(tlvData.value, res);
                }
            }
        }

        [Fact]
        public void BigSizeEncodingDataTest()
        {
            string rawData = File.ReadAllText("Bolt1/Data/BigSizeEncodingData.json");
            TlvData[]? data = JsonSerializer.Deserialize<TlvData[]>(rawData);

            foreach (TlvData tlvData in data)
            {
                byte[] dataBytes = Hex.FromString(tlvData.bytes) ?? new byte[0]; ;

                var writer = new ArrayBufferWriter<byte>();
                writer.WriteBigSize((ulong)tlvData.value);
                Assert.Equal(dataBytes, writer.WrittenSpan.ToArray());
            }
        }

        internal class TlvData
        {
            public string? name { get; set; }
            public ulong? value { get; set; }
            public string? bytes { get; set; }
            public string? exp_error { get; set; }
        }
    }
}