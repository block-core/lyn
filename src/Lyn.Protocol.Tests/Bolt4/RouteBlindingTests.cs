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

namespace Lyn.Protocol.Tests.Bolt4
{
    public class RouteBlindingTests
    {

        [Fact]
        public void RouteBlinding_CanCreatedBlindedRoute_ReferenceTestVector()
        {
            var alice = new PrivateKey(Convert.FromHexString("4141414141414141414141414141414141414141414141414141414141414141"));
            var bob = new PrivateKey(Convert.FromHexString("4242424242424242424242424242424242424242424242424242424242424242"));
            var bobPayload = Convert.FromHexString("01200000000000000000000000000000000000000000000000000000000000000000020800000000000000010a0800320000000027100c05000b7246320e00");
            var carol = new PrivateKey(Convert.FromHexString("4343434343434343434343434343434343434343434343434343434343434343"));
            var carolPayload = Convert.FromHexString("020800000000000000020821031b84c5567b126440995d3ed5aaba0565d71e1834604819ff9c17f5e9d5dd078f0a07004b00000096640c05000b7214320e00");
            var dave = new PrivateKey(Convert.FromHexString("4444444444444444444444444444444444444444444444444444444444444444"));
            var davePayload = Convert.FromHexString("012200000000000000000000000000000000000000000000000000000000000000000000020800000000000000030a060019000000640c05000b71c9320e00");
            var eve = new PrivateKey(Convert.FromHexString("4545454545454545454545454545454545454545454545454545454545454545"));
            var evePayload = Convert.FromHexString("011c000000000000000000000000000000000000000000000000000000000616c9cf92f45ade68345bc20ae672e2012f4af487ed44150c05000b71b0320e00");

            var curveActions = new EllipticCurveActions();
            var lightningKeyDerivation = new LightningKeyDerivation();
            var sphinx = new Sphinx(curveActions);

            var routeBlinding = new RouteBlinding(sphinx, lightningKeyDerivation, curveActions);

            // Eve creates a blinded route to herself through Dave
            var eveSelfSessionKey = new PrivateKey(Convert.FromHexString("0101010101010101010101010101010101010101010101010101010101010101"));
            var daveEveHops = new List<(PublicKey PublicKey, byte[] Payload)>() { 
                                (PublicKey: lightningKeyDerivation.PublicKeyFromPrivateKey(dave), Payload: davePayload), 
                                (PublicKey: lightningKeyDerivation.PublicKeyFromPrivateKey(eve), Payload: evePayload) 
                            };
            var (BlindedRoute, LastBlinding) = routeBlinding.Create(eveSelfSessionKey, daveEveHops);
            // this passes, hooray!
            Assert.Equal(new PublicKey(Convert.FromHexString("031b84c5567b126440995d3ed5aaba0565d71e1834604819ff9c17f5e9d5dd078f")), BlindedRoute.BlindingKey);
            // this fails, boo! 
            // the failure stems from the result of ```e*PrivKey(sha256(blindingPubKeyBytes.Concat(sharedSecret)))``` being incorrect
            Assert.Equal(new PublicKey(Convert.FromHexString("03e09038ee76e50f444b19abf0a555e8697e035f62937168b80adf0931b31ce52a")), LastBlinding);
        }

    }
}