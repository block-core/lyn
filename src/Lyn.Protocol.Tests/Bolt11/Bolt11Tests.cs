using System;
using FluentAssertions;
using Lyn.Protocol.Bolt11;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using Xunit;

namespace Lyn.Protocol.Tests.Bolt11
{
    /// <summary>
    /// Tests based on the official BOLT 11 test vectors, see Bolt11TestVectors.
    /// All example invoices are signed with private key
    /// e126f68f7eafcc8b74f54d269fe206be715000f94dac067d1c04a8ca3b2db734
    /// and use payment secret 0x1111111111111111111111111111111111111111111111111111111111111111.
    /// </summary>
    public class Bolt11Tests
    {
        private const string PRIVATE_KEY =
            "e126f68f7eafcc8b74f54d269fe206be715000f94dac067d1c04a8ca3b2db734";

        private const string PAYEE_PUBKEY =
            "03e7156ae33b0a208d0744199163177e909e80176e55d97a2f221ede0f934dd9ad";

        [Fact]
        public void CanParseDonationInvoiceWithNoAmount()
        {
            Bolt11Invoice parsed = Bolt11InvoiceParser.Parse(Bolt11TestVectors.DonationInvoice);

            parsed.Prefix.Should().Be("lnbc");
            parsed.AmountMsat.Should().BeNull();
            parsed.Timestamp.Should().Be(1496314658);
            parsed.PayeeNodeId.ToString().Should().Be(PAYEE_PUBKEY);
            parsed.PaymentHash.Should().Be(new UInt256(
                Convert.FromHexString("0001020304050607080900010203040506070809000102030405060708090102")));
            parsed.Description.Should().Be("Please consider supporting this project");
            parsed.Expiry.Should().Be(3600);
            parsed.MinFinalCltvExpiry.Should().Be(18);
            parsed.Features.Should().Equal(Convert.FromHexString("4100"));
        }

        [Fact]
        public void CanParseCoffeeInvoiceWithExpiry()
        {
            Bolt11Invoice parsed = Bolt11InvoiceParser.Parse(Bolt11TestVectors.CoffeeInvoice);

            parsed.AmountMsat.Should().Be(new MiliSatoshis(250000000));
            parsed.Description.Should().Be("1 cup coffee");
            parsed.Expiry.Should().Be(60);
            parsed.Timestamp.Should().Be(1496314658);
            parsed.PayeeNodeId.ToString().Should().Be(PAYEE_PUBKEY);
        }

        [Fact]
        public void CanParseTestnetInvoiceWithP2pkhFallback()
        {
            Bolt11Invoice parsed = Bolt11InvoiceParser.Parse(Bolt11TestVectors.TestnetP2pkhFallbackInvoice);

            parsed.Prefix.Should().Be("lntb");
            parsed.AmountMsat.Should().Be(new MiliSatoshis(2000000000));
            parsed.DescriptionHash.Should().NotBeNull();
            parsed.Description.Should().BeNull();
            parsed.FallbackAddresses.Should().HaveCount(1);
            parsed.FallbackAddresses[0].Version.Should().Be((byte)17);
            parsed.FallbackAddresses[0].Address.Should().HaveCount(20);
        }

        [Fact]
        public void CanParseUppercaseInvoice()
        {
            Bolt11Invoice fromLower = Bolt11InvoiceParser.Parse(Bolt11TestVectors.CoffeeBeansInvoice);
            Bolt11Invoice fromUpper = Bolt11InvoiceParser.Parse(Bolt11TestVectors.CoffeeBeansUppercaseInvoice);

            fromUpper.AmountMsat.Should().Be(fromLower.AmountMsat);
            fromUpper.Timestamp.Should().Be(fromLower.Timestamp);
            fromUpper.PaymentHash.Should().Be(fromLower.PaymentHash);
            fromUpper.Description.Should().Be("coffee beans");

            // 25 mBTC
            fromUpper.AmountMsat.Should().Be(new MiliSatoshis(2500000000));
        }

        [Theory]
        [InlineData("BadChecksumInvoice")]
        [InlineData("NoSeparatorInvoice")]
        [InlineData("TooShortInvoice")]
        [InlineData("InvalidMultiplierInvoice")]
        [InlineData("SubMillisatoshiInvalidInvoice")]
        public void InvalidInvoicesFailToParse(string vectorName)
        {
            string invoice = (string)typeof(Bolt11TestVectors).GetField(vectorName)!.GetValue(null)!;

            Action act = () => Bolt11InvoiceParser.Parse(invoice);

            act.Should().Throw<Exception>($"invoice '{vectorName}' must not parse");
        }

        [Fact]
        public void MixedCaseInvoicesAreRejected()
        {
            Action act = () => Bech32.Decode(Bolt11TestVectors.MixedCaseInvalidInvoice);

            act.Should().Throw<Bech32Exception>();
        }

        [Fact]
        public void CanRebuildOfficialInvoiceByteForByte()
        {
            var request = new Bolt11InvoiceRequest
            {
                Prefix = "lnbc",
                AmountMsat = null,
                Timestamp = 1496314658,
                PayeePrivateKey = new PrivateKey(Convert.FromHexString(PRIVATE_KEY)),
                PaymentSecret = new Secret(Convert.FromHexString(
                    "1111111111111111111111111111111111111111111111111111111111111111")),
                PaymentHash = new UInt256(Convert.FromHexString(
                    "0001020304050607080900010203040506070809000102030405060708090102")),
                Description = "Please consider supporting this project",
                Features = Convert.FromHexString("4100")
            };

            Bolt11Invoice built = Bolt11InvoiceBuilder.Build(request);

            built.RawInvoice.Should().Be(
                Bolt11TestVectors.DonationInvoice,
                "the builder must reproduce the official test vector exactly");
        }

        [Fact]
        public void BuiltInvoiceRoundTripsThroughParser()
        {
            var request = new Bolt11InvoiceRequest
            {
                Prefix = "lnbcrt",
                AmountMsat = new MiliSatoshis(12345678),
                Timestamp = 1600000000,
                PayeePrivateKey = new PrivateKey(Convert.FromHexString(PRIVATE_KEY)),
                PaymentSecret = new Secret(Convert.FromHexString(
                    "2222222222222222222222222222222222222222222222222222222222222222")),
                Description = "round trip test",
                Expiry = 7200,
                MinFinalCltvExpiry = 24
            };

            Bolt11Invoice built = Bolt11InvoiceBuilder.Build(request);
            Bolt11Invoice parsed = Bolt11InvoiceParser.Parse(built.RawInvoice!);

            parsed.Prefix.Should().Be("lnbcrt");
            parsed.AmountMsat.Should().Be(request.AmountMsat);
            parsed.Timestamp.Should().Be(request.Timestamp);
            parsed.PaymentHash.Should().Be(request.PaymentHash);
            parsed.PaymentSecret.Should().Be(request.PaymentSecret);
            parsed.Description.Should().Be(request.Description);
            parsed.Expiry.Should().Be(7200);
            parsed.MinFinalCltvExpiry.Should().Be(24);
            parsed.PayeeNodeId.ToString().Should().Be(PAYEE_PUBKEY);
            parsed.IsExpired(1600100000).Should().BeTrue();
            parsed.IsExpired(1600007000).Should().BeFalse();
        }

        [Theory]
        [InlineData("", null)]
        [InlineData("2500u", "250000000")]
        [InlineData("20m", "2000000000")]
        [InlineData("9678785340p", "967878534")]
        [InlineData("10n", "1000")]
        public void AmountParsingMatchesSpec(string amount, string? expectedMsat)
        {
            MiliSatoshis? result = Bolt11InvoiceParser.ParseAmount(amount);

            if (expectedMsat is null)
                result.Should().BeNull();
            else
                result.Should().Be(new MiliSatoshis(ulong.Parse(expectedMsat)));
        }
    }
}
