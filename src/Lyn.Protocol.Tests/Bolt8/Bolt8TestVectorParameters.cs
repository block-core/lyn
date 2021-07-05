using Lyn.Types;

namespace Lyn.Protocol.Tests.Bolt8
{
    public static class Bolt8TestVectorParameters
    {
        private const string INITIATOR_PRIVATE_KEY_HEX = "0x1111111111111111111111111111111111111111111111111111111111111111";
        private const string INITIATOR_PUBLIC_KEY_HEX = "0x034f355bdcb7cc0af728ef3cceb9615d90684bb5b2ca5f859ab0f0b704075871aa";

        private const string RESPONDER_PRIVATE_KEY_HEX = "0x2121212121212121212121212121212121212121212121212121212121212121";
        private const string RESPONDER_PUBLIC_KEY_HEX = "0x028d7500dd4c12685d1f568b4c2b5048e8534b873319f3a8daa612b469132ec7f7";
        
        private const string INITIATOR_EPHEMERAL_PRIVATE_KEY_HEX = "0x1212121212121212121212121212121212121212121212121212121212121212";
        private const string INITIATOR_EPHEMERAL_PUBLIC_KEY_HEX = "0x036360e856310ce5d294e8be33fc807077dc56ac80d95d9cd4ddbd21325eff73f7";

        private const string RESPONDER_EPHEMERAL_PRIVATE_KEY_HEX = "0x2222222222222222222222222222222222222222222222222222222222222222";
        private const string RESPONDER_EPHEMERAL_PUBLIC_KEY_HEX = "0x02466d7fcae563e5cb09a0d1870bb580344804617879a14949cf22285f1bae3f27";

        public class Key
        {
           public byte[] PublicKey { get; }
           public byte[] PrivateKey { get; }

           public Key(byte[] privatKey, byte[] publicKey)
           {
              PrivateKey = privatKey;
              PublicKey = publicKey;
           }
        }
        
        public static Key Initiator =>
                new Key(Hex.FromString(INITIATOR_PRIVATE_KEY_HEX),
                    Hex.FromString(INITIATOR_PUBLIC_KEY_HEX));

        public static Key InitiatorEphemeralKeyPair =>
            new Key(Hex.FromString(INITIATOR_EPHEMERAL_PRIVATE_KEY_HEX),
                Hex.FromString(INITIATOR_EPHEMERAL_PUBLIC_KEY_HEX));
        
        public static Key Responder =>
            new Key(Hex.FromString(RESPONDER_PRIVATE_KEY_HEX),
                Hex.FromString(RESPONDER_PUBLIC_KEY_HEX));
        
        public static Key ResponderEphemeralKeyPair =>
            new Key(Hex.FromString(RESPONDER_EPHEMERAL_PRIVATE_KEY_HEX),
                Hex.FromString(RESPONDER_EPHEMERAL_PUBLIC_KEY_HEX));

        public static class ActOne
        {
            public const string END_STATE_HASH =
                "0x9d1ffbb639e7e20021d9259491dc7b160aab270fb1339ef135053f6f2cebe9ce";

            public const string INITIATOR_OUTPUT =
                "0x00036360e856310ce5d294e8be33fc807077dc56ac80d95d9cd4ddbd21325eff73f70df6086551151f58b8afe6c195782c6a";
        }

        public static class ActTwo
        {
            public const string END_STATE_HASH =
                "0x90578e247e98674e661013da3c5c1ca6a8c8f48c90b485c0dfa1494e23d56d72";

            public const string RESPONDER_OUTPUT =
                "0x0002466d7fcae563e5cb09a0d1870bb580344804617879a14949cf22285f1bae3f276e2470b93aac583c9ef6eafca3f730ae";
        }

        public static class ActThree
        {
            public const string END_STATE_HASH =
                "0x5dcb5ea9b4ccc755e0e3456af3990641276e1d5dc9afd82f974d90a47c918660";

            public const string INITIATOR_OUTPUT =
                "0x00b9e3a702e93e3a9948c2ed6e5fd7590a6e1c3a0344cfc9d5b57357049aa22355361aa02e55a8fc28fef5bd6d71ad0c38228dc68b1c466263b47fdf31e560e139ba";
        }
    }
}