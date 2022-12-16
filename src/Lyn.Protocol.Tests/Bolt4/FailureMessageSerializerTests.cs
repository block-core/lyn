using Lyn.Protocol.Bolt4;
using Lyn.Protocol.Bolt4.Entities;
using Lyn.Protocol.Bolt4.Messages;
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
    // need to implement tests so we have parity with FailureMessageCodecsSpec.scala from eclair
    public class FailureMessageSerializerTests
    {

        [Fact]
        public void EncodeAllFailureMessages()
        {
            var failureMessages = new List<FailureMessage>() {
                new InvalidRealmMessage(),
                new TemporaryNodeFailureMessage(),
                new PermanentNodeFailureMessage(),
                new RequiredNodeFeatureMissingMessage()
            };

            var serializer = new FailureMessageSerializer();
            // loop through all failure messages, serialize them, and then deserialize them and compare the result
            foreach (var failureMessage in failureMessages)
            {
                var bufferWriter = new ArrayBufferWriter<byte>(256);
                int bytesWritten = serializer.Serialize(failureMessage, bufferWriter, null);
                var failureMessageBytes = bufferWriter.WrittenSpan.ToArray();

                Assert.Equal(bytesWritten, failureMessageBytes.Length);

                var failureMessaegSequence = new ReadOnlySequence<byte>(failureMessageBytes);
                var failureMessageSequenceReader = new SequenceReader<byte>(failureMessaegSequence);

                var failureMessageDeserialized = serializer.Deserialize(ref failureMessageSequenceReader, null);

                Assert.Equal(failureMessage, failureMessageDeserialized);
            }
        }

    }
}
