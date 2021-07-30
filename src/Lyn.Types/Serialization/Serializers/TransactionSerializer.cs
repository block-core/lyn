using System.Buffers;
using Lyn.Types.Bitcoin;

namespace Lyn.Types.Serialization.Serializers
{
    public class TransactionSerializer : IProtocolTypeSerializer<Transaction>
    {
        private readonly IProtocolTypeSerializer<TransactionInput> _transactionInputSerializer;
        private readonly IProtocolTypeSerializer<TransactionOutput> _transactionOutputSerializer;
        private readonly IProtocolTypeSerializer<TransactionWitness> _transactionWitnessSerializer;

        public TransactionSerializer(IProtocolTypeSerializer<TransactionInput> transactionInputSerializer,
                                     IProtocolTypeSerializer<TransactionOutput> transactionOutputSerializer,
                                     IProtocolTypeSerializer<TransactionWitness> transactionWitnessSerializer)
        {
            _transactionInputSerializer = transactionInputSerializer;
            _transactionOutputSerializer = transactionOutputSerializer;
            _transactionWitnessSerializer = transactionWitnessSerializer;
        }

        public Transaction Deserialize(ref SequenceReader<byte> reader, ProtocolTypeSerializerOptions? options = null)
        {
            bool defaultAllowWitness = true; // all our trx use witness we enable it by default
            bool allowWitness = options?.Get(SerializerOptions.SERIALIZE_WITNESS, defaultAllowWitness) ?? defaultAllowWitness;
            byte flags = 0;

            var tx = new Transaction { Version = reader.ReadInt() };

            /// Try to read the inputs. In case the dummy byte is, this will be read as an empty list of transaction inputs.
            TransactionInput[] inputs = reader.ReadArray(_transactionInputSerializer);

            if (inputs.Length == 0)
            {
                //we don't expect an empty transaction inputs list, so we treat this as having witness data
                flags = reader.ReadByte();
                if (flags != 0 && allowWitness)
                {
                    tx.Inputs = reader.ReadArray(_transactionInputSerializer);
                    tx.Outputs = reader.ReadArray(_transactionOutputSerializer);
                }
            }
            else
            {
                tx.Inputs = inputs;
                // otherwise we read valid inputs, now we have to read outputs
                tx.Outputs = reader.ReadArray(_transactionOutputSerializer);
            }

            if ((flags & 1) != 0 && allowWitness)
            {
                /* The witness flag is present, and we support witnesses. */
                flags ^= 1;

                for (int i = 0; i < tx.Inputs!.Length; i++)
                {
                    tx.Inputs[i].ScriptWitness = reader.ReadWithSerializer(_transactionWitnessSerializer);
                }

                if (!tx.HasWitness())
                {
                    // It's illegal to encode witnesses when all witness stacks are empty.
                    ThrowHelper.ThrowNotSupportedException("Superfluous witness record");
                }
            }

            if (flags != 0)
            {
                /* Unknown flag in the serialization */
                ThrowHelper.ThrowNotSupportedException("Unknown transaction optional data");
            }

            tx.LockTime = reader.ReadUInt();

            return tx;
        }

        public int Serialize(Transaction tx, IBufferWriter<byte> writer, ProtocolTypeSerializerOptions? options = null)
        {
            bool defaultAllowWitness = true; // all our trx use witness we enable it by default
            bool allowWitness = options?.Get(SerializerOptions.SERIALIZE_WITNESS, defaultAllowWitness) ?? defaultAllowWitness;
            byte flags = 0;
            int size = 0;

            size += writer.WriteInt(tx.Version);

            // Consistency check.
            if (allowWitness)
            {
                // Check whether witnesses need to be serialized.
                if (tx.HasWitness())
                {
                    flags |= 1;
                }
            }

            if (flags != 0)
            {
                // Use extended format in case witnesses are to be serialized.
                size += writer.WriteVarInt(0);
                size += writer.WriteByte(flags);
            }

            size += writer.WriteArray(tx.Inputs, _transactionInputSerializer);
            size += writer.WriteArray(tx.Outputs, _transactionOutputSerializer);

            if ((flags & 1) != 0)
            {
                if (tx.Inputs != null)
                {
                    for (int i = 0; i < tx.Inputs.Length; i++)
                    {
                        size += writer.WriteWithSerializer(tx.Inputs[i].ScriptWitness!, _transactionWitnessSerializer);
                    }
                }
            }

            size += writer.WriteUInt(tx.LockTime);

            return size;
        }
    }
}