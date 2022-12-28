using Lyn.Protocol.Bolt4;
using Lyn.Protocol.Common.Crypto;
using System;
using System.Linq;
using Xunit;

using Lyn.Protocol.Tests.Bolt4.Data;
using Lyn.Protocol.Bolt4.Entities;
using System.Collections.Generic;
using Lyn.Types.Fundamental;
using Lyn.Types.Onion;
using Lyn.Protocol.Bolt3;
using System.Diagnostics;

namespace Lyn.Protocol.Tests.Bolt4
{
    public class RouteBlindingTests
    {

        [Fact]
        public void RouteBlinding_CanCreatedBlindedRoute_ReferenceTestVector()
        {
            // note: this is really ugly rn, will clean up once e2e works

            var lightningKeyDerivation = new LightningKeyDerivation();

            var alice = new PrivateKey(Convert.FromHexString("4141414141414141414141414141414141414141414141414141414141414141"));
            var alicePubKey = lightningKeyDerivation.PublicKeyFromPrivateKey(alice);
            var bob = new PrivateKey(Convert.FromHexString("4242424242424242424242424242424242424242424242424242424242424242"));
            var bobPubKey = lightningKeyDerivation.PublicKeyFromPrivateKey(bob);
            var bobPayload = Convert.FromHexString("01200000000000000000000000000000000000000000000000000000000000000000020800000000000000010a0800320000000027100c05000b7246320e00");
            var carol = new PrivateKey(Convert.FromHexString("4343434343434343434343434343434343434343434343434343434343434343"));
            var carolPubKey = lightningKeyDerivation.PublicKeyFromPrivateKey(carol);
            var carolPayload = Convert.FromHexString("020800000000000000020821031b84c5567b126440995d3ed5aaba0565d71e1834604819ff9c17f5e9d5dd078f0a07004b00000096640c05000b7214320e00");
            var dave = new PrivateKey(Convert.FromHexString("4444444444444444444444444444444444444444444444444444444444444444"));
            var davePubKey = lightningKeyDerivation.PublicKeyFromPrivateKey(dave);
            var davePayload = Convert.FromHexString("012200000000000000000000000000000000000000000000000000000000000000000000020800000000000000030a060019000000640c05000b71c9320e00");
            var eve = new PrivateKey(Convert.FromHexString("4545454545454545454545454545454545454545454545454545454545454545"));
            var evePubKey = lightningKeyDerivation.PublicKeyFromPrivateKey(eve);
            var evePayload = Convert.FromHexString("011c000000000000000000000000000000000000000000000000000000000616c9cf92f45ade68345bc20ae672e2012f4af487ed44150c05000b71b0320e00");

            var curveActions = new EllipticCurveActions();
            var sphinx = new Sphinx(curveActions);

            var routeBlinding = new RouteBlinding(sphinx, lightningKeyDerivation, curveActions);

            // Eve creates a blinded route to herself through Dave
            var eveSessionKey = new PrivateKey(Convert.FromHexString("0101010101010101010101010101010101010101010101010101010101010101"));
            var daveEveHops = new List<(PublicKey PublicKey, byte[] Payload)>() {
                                (PublicKey: davePubKey, Payload: davePayload),
                                (PublicKey: evePubKey, Payload: evePayload)
                            };
            var (blindedRouteEnd, lastBlinding) = routeBlinding.Create(eveSessionKey, daveEveHops);
            Assert.Equal(new PublicKey(Convert.FromHexString("031b84c5567b126440995d3ed5aaba0565d71e1834604819ff9c17f5e9d5dd078f")), blindedRouteEnd.BlindingKey);
            Assert.Equal(new PublicKey(Convert.FromHexString("03e09038ee76e50f444b19abf0a555e8697e035f62937168b80adf0931b31ce52a")), lastBlinding);

            // Bob also wants to use route blinding:
            var bobSessionKey = new PrivateKey(Convert.FromHexString("0202020202020202020202020202020202020202020202020202020202020202"));
            var bobCarolHops = new List<(PublicKey PublicKey, byte[] Payload)>() {
                                (PublicKey: lightningKeyDerivation.PublicKeyFromPrivateKey(bob), Payload: bobPayload),
                                (PublicKey: lightningKeyDerivation.PublicKeyFromPrivateKey(carol), Payload: carolPayload)
                            };
            var blindedRouteStart = routeBlinding.Create(bobSessionKey, bobCarolHops).Route;
            Assert.Equal(new PublicKey(Convert.FromHexString("024d4b6cd1361032ca9bd2aeb9d900aa4d45d9ead80ac9423374c451a7254d0766")), blindedRouteStart.BlindingKey);

            // We now have a blinded route Bob -> Carol -> Dave -> Eve
            var blindedRoute = new BlindedRoute(bobPubKey,
                                                blindedRouteStart.BlindingKey,
                                                blindedRouteStart.BlindedNodes.Concat(blindedRouteEnd.BlindedNodes).ToArray());

            Assert.Equal(new List<PublicKey>() {
                new PublicKey(Convert.FromHexString("03da173ad2aee2f701f17e59fbd16cb708906d69838a5f088e8123fb36e89a2c25")),
                new PublicKey(Convert.FromHexString("02e466727716f044290abf91a14a6d90e87487da160c2a3cbd0d465d7a78eb83a7")),
                new PublicKey(Convert.FromHexString("036861b366f284f0a11738ffbf7eda46241a8977592878fe3175ae1d1e4754eccf")),
                new PublicKey(Convert.FromHexString("021982a48086cb8984427d3727fe35a03d396b234f0701f5249daa12e8105c8dae")),
            }, blindedRoute.BlindedNodeIds);

            Assert.Equal(new List<byte[]>() {
                Convert.FromHexString("cd7b00ff9c09ed28102b210ac73aa12d63e90852cebc496c49f57c499a2888b49f2e72b19446f7e60a818aa2938d8c625415b992b8928a7321edb8f7cea40de362bed082ad51acc6156dca5532fb68"),
                Convert.FromHexString("cc0f16524fd7f8bb0f4e8d40ad71709ef140174c76faa574cac401bb8992fef76c4d004aa485dd599ed1cf2715f570f656a5aaecaf1ee8dc9d0fa1d424759be1932a8f29fac08bc2d2a1ed7159f28b"),
                Convert.FromHexString("0fa1a72cff3b64a3d6e1e4903cf8c8b0a17144aeb249dcb86561adee1f679ee8db3e561d9e49895fd4bcebf6f58d6f61a6d41a9bf5aa4b0453437856632e8255c351873143ddf2bb2b0832b091e1b4"),
                Convert.FromHexString("da1c7e5f7881219884beae6ae68971de73bab4c3055d9865b1afb60722a63c688768042ade22f2c22f5724767d171fd221d3e579e43b354cc72e3ef146ada91a892d95fc48662f5b158add0af457da")
            }, blindedRoute.EncryptedPayloads.ToList());

            // After generating the blinded route, Eve is able to derive the private key corresponding to her blinded payload
            var eveBlindedPrivKey = routeBlinding.DerivePrivateKey(eve, lastBlinding);
            var eveBlindedPubKey = lightningKeyDerivation.PublicKeyFromPrivateKey(eveBlindedPrivKey);
            Assert.Equal(eveBlindedPubKey, blindedRoute.BlindedNodeIds.LastOrDefault());
        }

    }
}