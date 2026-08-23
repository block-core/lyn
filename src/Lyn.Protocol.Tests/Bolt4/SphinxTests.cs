using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Lyn.Protocol.Bolt4;
using Lyn.Types.Bolt;
using Lyn.Types.Fundamental;
using NBitcoin.Secp256k1;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt4
{
    /// <summary>
    /// Tests based on the BOLT 4 specification test vectors.
    /// https://github.com/lightning/bolts/blob/master/04-onion-routing.md#test-vector
    /// </summary>
    public class SphinxTests
    {
        private static readonly string[] TestVectorPubKeys =
        {
            "02eec7245d6b7d2ccb30380bfbe2a3648cd7a942653f5aa340edcea1f283686619",
            "0324653eac434488002cc06bbfb7f10fe18991e35f9fe4302dbea6d2353dc0ab1c",
            "027f31ebc5462c1fdce1b737ecff52d37d75dea43ce11c74d25aa297165faa2007",
            "032c0b7cf95324a07d05398b240174dc0c2be444d96b159aa6c7f7b1e668680991",
            "02edabbd16b41c8371b92ef2f04c1185b4f03b6dcd52ba9b78d9d7c89c8f221145"
        };

        private const string TEST_SESSION_KEY =
            "4141414141414141414141414141414141414141414141414141414141414141";

        // Shared secrets from the official test vector, in reverse route order.
        private static readonly string[] ExpectedSharedSecretsLastToFirst =
        {
            "b5756b9b542727dbafc6765a49488b023a725d631af688fc031217e90770c328", // node 4
            "21e13c2d7cfe7e18836df50872466117a295783ab8aab0e7ecc8c725503ad02d", // node 3
            "3a6b412548762f0dbccce5c7ae7bb8147d1caf9b5471c34120b30bc9c04891cc", // node 2
            "a6519e98832a0b179f62123b3567c106db99ee37bef036e783263602f3488fae", // node 1
            "53eb63ea8a3fec3b3cd433b85cd62a4b145e1dda09391b348c4e1cd36a03ea66", // node 0
        };

        private static ECPrivKey SessionKey()
        {
            ECPrivKey.TryCreate(Convert.FromHexString(TEST_SESSION_KEY), Context.Instance, out ECPrivKey? key);

            return key!;
        }

        private static PublicKey[] HopPublicKeys() =>
            TestVectorPubKeys.Select(k => new PublicKey(Convert.FromHexString(k))).ToArray();

        [Fact]
        public void SharedSecretsMatchOfficialTestVector()
        {
            ECPrivKey ephemeralKey = SessionKey();
            PublicKey[] hopPublicKeys = HopPublicKeys();

            var sharedSecrets = new byte[hopPublicKeys.Length][];

            for (int i = 0; i < hopPublicKeys.Length; i++)
            {
                ECPubKey.TryCreate(hopPublicKeys[i], Context.Instance, out _, out ECPubKey? hopPubKey);
                sharedSecrets[i] = SphinxSharedSecrets.ComputeSharedSecret(ephemeralKey, hopPubKey!);

                byte[] blindingFactor =
                    SphinxSharedSecrets.ComputeBlindingFactor(ephemeralKey.CreatePubKey(), sharedSecrets[i]);

                ephemeralKey = SphinxSharedSecrets.BlindEphemeralPrivateKey(ephemeralKey, blindingFactor);
            }

            // The test vector lists secrets starting at the final node.
            for (int i = 0; i < hopPublicKeys.Length; i++)
            {
                string expected = ExpectedSharedSecretsLastToFirst[hopPublicKeys.Length - 1 - i];

                Convert.ToHexString(sharedSecrets[i]).ToLowerInvariant().Should().Be(expected,
                    $"shared secret {i} must match the BOLT 4 test vector");
            }
        }

        [Fact]
        public void UmAndAmmagKeysMatchOfficialTestVector()
        {
            var sharedSecret = Convert.FromHexString(ExpectedSharedSecretsLastToFirst[0]);

            byte[] umKey = SphinxKeys.GenerateKey(SphinxKeys.UM, sharedSecret);
            byte[] ammagKey = SphinxKeys.GenerateKey(SphinxKeys.AMMAG, sharedSecret);

            Convert.ToHexString(umKey).ToLowerInvariant().Should()
                .Be("4da7f2923edce6c2d85987d1d9fa6d88023e6c3a9c3d20f07d3b10b61a78d646");

            Convert.ToHexString(ammagKey).ToLowerInvariant().Should()
                .Be("2f36bb8822e1f0d04c27b7d8bb7d7dd586e032a3218b8d414afbba6f169a4d68");
        }

        [Fact]
        public void OnionPacketRoundTripsThroughEveryHop()
        {
            // Build our own route so we hold the private keys of every hop.
            var hopPrivateKeys = Enumerable.Range(1, 5)
                .Select(i => System.Text.Encoding.ASCII.GetBytes($"hop-private-key-{i:D2}xxxxxxxxxxxxxx"))
                .Select(b => new PrivateKey(b))
                .ToArray();

            PublicKey[] hopPublicKeys = hopPrivateKeys
                .Select(k =>
                {
                    ECPrivKey.TryCreate(k, Context.Instance, out ECPrivKey? priv);
                    return new PublicKey(priv!.CreatePubKey().ToBytes());
                })
                .ToArray();

            byte[] associatedData = Convert.FromHexString(
                "44028e3f5c7d0d0b3a2f4c9d8b6a5e4f3c2d1e0f9a8b7c6d5e4f3a2b1c0d9e8f");

            byte[][] payloads =
            {
                HopPayload.BuildForwardingPayload(100000, 700000, ShortChannelId.FromUInt64(0x0102030405060708)),
                HopPayload.BuildForwardingPayload(90000, 650000, ShortChannelId.FromUInt64(0x1122334455667788)),
                HopPayload.BuildForwardingPayload(80000, 600000, ShortChannelId.FromUInt64(0x21a2b3c4d5e6f708)),
                HopPayload.BuildFinalPayload(70000, 550000),
                HopPayload.BuildFinalPayload(70000, 550000, Convert.FromHexString("1111111111111111111111111111111111111111111111111111111111111111"), 70000)
            };

            OnionPacket packet = OnionPacketBuilder.CreateOnionPacket(
                hopPrivateKeys[0], hopPublicKeys, payloads, associatedData);

            packet.PacketLengthIsCorrect();

            var processor = new OnionPacketProcessor();

            for (int hop = 0; hop < hopPrivateKeys.Length - 1; hop++)
            {
                ProcessedOnionPacket processed = processor.ProcessOnionPacket(packet, hopPrivateKeys[hop], associatedData);

                processed.IsFinalNode.Should().BeFalse($"hop {hop} is an intermediate hop");
                processed.Payload.Should().Equal(payloads[hop], $"hop {hop} payload must round trip");

                packet = processed.ForwardPacket!;
                packet.Version.Should().Be(OnionPacket.VERSION);
            }

            // Final hop sees its own payload and no forward packet.
            ProcessedOnionPacket finalHop = processor.ProcessOnionPacket(packet, hopPrivateKeys[^1], associatedData);

            finalHop.IsFinalNode.Should().BeTrue();
            finalHop.ForwardPacket.Should().BeNull();
            finalHop.Payload.Should().Equal(payloads[^1]);
        }

        [Fact]
        public void SingleHopRouteWorks()
        {
            var privateKey = new PrivateKey(System.Text.Encoding.ASCII.GetBytes("single-hop-key-xxxxxxxxxxxxxxxxx"));

            ECPrivKey.TryCreate(privateKey, Context.Instance, out ECPrivKey? priv);
            var publicKey = new PublicKey(priv!.CreatePubKey().ToBytes());

            byte[] payload = HopPayload.BuildFinalPayload(123456, 500000);

            OnionPacket packet = OnionPacketBuilder.CreateOnionPacket(
                privateKey, new[] { publicKey }, new[] { payload });

            ProcessedOnionPacket result = new OnionPacketProcessor()
                .ProcessOnionPacket(packet, privateKey);

            result.IsFinalNode.Should().BeTrue();
            result.Payload.Should().Equal(payload);
        }

        [Fact]
        public void TamperingWithHmacFailsVerification()
        {
            var privateKey = new PrivateKey(System.Text.Encoding.ASCII.GetBytes("tamper-test-key-xxxxxxxxxxxxxxxx"));

            ECPrivKey.TryCreate(privateKey, Context.Instance, out ECPrivKey? priv);
            var publicKey = new PublicKey(priv!.CreatePubKey().ToBytes());

            OnionPacket packet = OnionPacketBuilder.CreateOnionPacket(
                privateKey, new[] { publicKey }, new[] { HopPayload.BuildFinalPayload(1000, 500000) });

            packet.Hmac[0] ^= 0xFF;

            Action act = () => new OnionPacketProcessor().ProcessOnionPacket(packet, privateKey);

            act.Should().Throw<SphinxException>();
        }

        [Fact]
        public void ErrorPacketsCanBeReturnedThroughTheWholeRoute()
        {
            int numHops = 5;

            var hopPrivateKeys = Enumerable.Range(1, numHops)
                .Select(i => System.Text.Encoding.ASCII.GetBytes($"error-route-hop-{i:D2}-private-keyxx"))
                .Select(b => new PrivateKey(b))
                .ToArray();

            byte[][] sharedSecrets = ComputeSharedSecrets(hopPrivateKeys);

            // failure_message from the BOLT 4 test vector:
            // incorrect_or_unknown_payment_details, htlc_msat=100 height=800000 plus a tlv record.
            byte[] failureMessage = BuildIncorrectOrUnknownPaymentDetails();

            // The erring node is the final hop of the route.
            byte[] errorPacket = FailureObfuscator.CreateErrorPacket(sharedSecrets[^1], failureMessage);

            // Each intermediate hop obfuscates the error again on the way back.
            for (int hop = numHops - 2; hop >= 0; hop--)
                errorPacket = FailureObfuscator.Obfuscate(sharedSecrets[hop], errorPacket);

            // The origin node iteratively peels layers until it finds the failing hop.
            byte[] peeled = errorPacket;
            bool found = false;
            byte[]? recoveredMessage = null;

            for (int hop = 0; hop < numHops && !found; hop++)
                found = FailureObfuscator.Peel(sharedSecrets[hop], ref peeled, out recoveredMessage);

            found.Should().BeTrue("the origin node must identify the failing hop");

            recoveredMessage.Should().NotBeNull();
            recoveredMessage!.Should().Equal(failureMessage);
        }

        private byte[][] ComputeSharedSecrets(PrivateKey[] hopPrivateKeys)
        {
            // Derive public keys then run the same blinding chain as the builder.
            var publicKeys = hopPrivateKeys.Select(k =>
            {
                ECPrivKey.TryCreate(k, Context.Instance, out ECPrivKey? priv);
                return new PublicKey(priv!.CreatePubKey().ToBytes());
            }).ToArray();

            ECPrivKey.TryCreate(hopPrivateKeys[0], Context.Instance, out ECPrivKey? ephemeral);

            var sharedSecrets = new byte[hopPrivateKeys.Length][];

            for (int i = 0; i < hopPrivateKeys.Length; i++)
            {
                ECPubKey.TryCreate(publicKeys[i], Context.Instance, out _, out ECPubKey? hopPubKey);

                sharedSecrets[i] = SphinxSharedSecrets.ComputeSharedSecret(ephemeral!, hopPubKey!);

                byte[] blindingFactor =
                    SphinxSharedSecrets.ComputeBlindingFactor(ephemeral!.CreatePubKey(), sharedSecrets[i]);

                ephemeral = SphinxSharedSecrets.BlindEphemeralPrivateKey(ephemeral, blindingFactor);
            }

            return sharedSecrets;
        }

        private static byte[] BuildIncorrectOrUnknownPaymentDetails()
        {
            // type 400f (incorrect_or_unknown_payment_details), u64 htlc_msat=100,
            // u32 height=800000, tlv record with bigsize type 34001, bigsize length 300, value 300x0x80.
            byte[] tlvValue = Enumerable.Repeat((byte)0x80, 300).ToArray();

            var message = new byte[2 + 8 + 4 + 3 + 3 + tlvValue.Length];

            int offset = 0;
            message[offset++] = 0x40;
            message[offset++] = 0x0F;

            const ulong htlcMsat = 100;
            for (int i = 0; i < 8; i++)
                message[offset++] = (byte)(htlcMsat >> (56 - i * 8));

            const uint height = 800000;
            for (int i = 0; i < 4; i++)
                message[offset++] = (byte)(height >> (24 - i * 8));

            // bigsize(34001) = fd84d1
            message[offset++] = 0xFD;
            message[offset++] = 0x84;
            message[offset++] = 0xD1;

            // bigsize(300) = fd012c
            message[offset++] = 0xFD;
            message[offset++] = 0x01;
            message[offset++] = 0x2C;

            tlvValue.CopyTo(message, offset);

            return message;
        }

        [Fact]
        public void HopPayloadEncodesAndDecodes()
        {
            ShortChannelId scid = ShortChannelId.FromUInt64(0x00_00_A1_B2_C3_D4_E5_F6);

            byte[] encoded = HopPayload.BuildForwardingPayload(1500000, 633000, scid);

            IReadOnlyList<HopTlvRecord> decoded = HopPayload.Decode(encoded);

            decoded.Count(r => r.Type == HopPayload.AMT_TO_FORWARD).Should().Be(1);
            decoded.Count(r => r.Type == HopPayload.OUTGOING_CLTV_VALUE).Should().Be(1);
            decoded.Count(r => r.Type == HopPayload.SHORT_CHANNEL_ID).Should().Be(1);

            ulong amt = ReadBigEndian(decoded.First(r => r.Type == HopPayload.AMT_TO_FORWARD).Value);
            amt.Should().Be(1500000UL);

            uint cltv = ReadBigEndian32(decoded.First(r => r.Type == HopPayload.OUTGOING_CLTV_VALUE).Value);
            cltv.Should().Be(633000U);
        }

        private static ulong ReadBigEndian(byte[] value)
        {
            ulong result = 0;

            foreach (byte b in value)
                result = (result << 8) | b;

            return result;
        }

        private static uint ReadBigEndian32(byte[] value)
        {
            uint result = 0;

            foreach (byte b in value)
                result = (result << 8) | b;

            return result;
        }
    }
}

internal static class OnionPacketTestExtensions
{
    public static void PacketLengthIsCorrect(this Lyn.Protocol.Bolt4.OnionPacket packet)
    {
        packet.ToByteArray().Length.Should().Be(Lyn.Protocol.Bolt4.OnionPacket.PACKET_LENGTH);
    }
}
