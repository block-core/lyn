﻿namespace Lyn.Types.Serialization
{
    public class SerializerOptions
    {
        /// <summary>
        /// Specifies if a transaction has to be serialized with witness information
        /// </summary>
        public const string SERIALIZE_WITNESS = "serialize_witness";

        /// <summary>
        /// Specify if the header that is going to be serialized is in Headers payload or in Block payload.
        /// Header in block, causes the header to not serialize the transaction count because will be serialized by the block itself
        /// </summary>
        public const string HEADER_IN_BLOCK = "serialize_header_transaction_count";

        /// <summary>
        /// The serialization version that cab be overridden by serialization options.
        /// </summary>
        public int ProtocolVersion { get; set; } = 0;

        protected SerializerOptions()
        {
        } //prevent to be instantiated, but can be inherited so can extend constants defined here
    }
}