using System;
using System.Linq;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Protocol.Common.Hashing;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;
using NBitcoin;
using NBitcoin.Secp256k1;

namespace Lyn.Protocol.Bolt3
{
    public class LightningKeyDerivation : ILightningKeyDerivation
    {
        public LightningKeyDerivation()
        {
        }

        public Secrets DeriveSecrets(Secret seed)
        {
            // To derive out private keys we use BIP32 key derivation with hardened derivation
            // todo: check this is secure enough

            ExtKey keyDerivation = new ExtKey(seed);

            var secrets = new Secrets
            {
                FundingPrivkey = new Secret(keyDerivation.Derive(1, true).PrivateKey.ToBytes()),
                RevocationBasepointSecret = new Secret(keyDerivation.Derive(2, true).PrivateKey.ToBytes()),
                PaymentBasepointSecret = new Secret(keyDerivation.Derive(3, true).PrivateKey.ToBytes()),
                HtlcBasepointSecret = new Secret(keyDerivation.Derive(4, true).PrivateKey.ToBytes()),
                DelayedPaymentBasepointSecret = new Secret(keyDerivation.Derive(5, true).PrivateKey.ToBytes()),
                Shaseed = new UInt256(keyDerivation.Derive(6, true).PrivateKey.ToBytes())
            };

            return secrets;
        }

        public Basepoints DeriveBasepoints(Secrets secrets)
        {
            var basepoints = new Basepoints
            {
                Revocation = PublicKeyFromPrivateKey(secrets.RevocationBasepointSecret),
                Payment = PublicKeyFromPrivateKey(secrets.PaymentBasepointSecret),
                Htlc = PublicKeyFromPrivateKey(secrets.HtlcBasepointSecret),
                DelayedPayment = PublicKeyFromPrivateKey(secrets.DelayedPaymentBasepointSecret),
            };

            return basepoints;
        }

        public Secret PerCommitmentSecret(UInt256 shaseed, ulong perCommitIndex)
        {
            Shachain.Shachain shachain = new();

            var secret = shachain.GenerateFromSeed(shaseed, perCommitIndex);

            return new Secret(secret.GetBytes().ToArray());
        }

        public PublicKey PerCommitmentPoint(UInt256 shaseed, ulong perCommitIndex)
        {
            Shachain.Shachain shachain = new();

            var secret = shachain.GenerateFromSeed(shaseed, perCommitIndex);

            return PublicKeyFromPrivateKey(new PrivateKey(secret.GetBytes().ToArray())); //TODO Dan confirm this is correct
        }

        public bool IsValidPublicKey(PublicKey publicKey)
        {
            if (ECPubKey.TryCreate(publicKey, Context.Instance, out bool compressed, out ECPubKey? ecpubkey))
            {
                return compressed;
            }

            return false;
        }

        public PublicKey PublicKeyFromPrivateKey(PrivateKey privateKey)
        {
            if (ECPrivKey.TryCreate(privateKey, Context.Instance, out ECPrivKey? ecprvkey))
            {
                if (ecprvkey != null)
                {
                    ECPubKey ecpubkey = ecprvkey.CreatePubKey();
                    Span<byte> pub = stackalloc byte[33];
                    ecpubkey.WriteToSpan(true, pub, out _);
                    return new PublicKey(pub.ToArray());
                }
            }

            return null;
        }

        /// <summary>
        /// derive_simple_key
        /// pubkey = basepoint + SHA256(per_commitment_point || basepoint) * G
        /// </summary>
        public PublicKey DerivePublickey(PublicKey basepoint, PublicKey perCommitmentPoint)
        {
            Span<byte> toHash = stackalloc byte[PublicKey.LENGTH * 2];
            perCommitmentPoint.GetSpan().CopyTo(toHash);
            basepoint.GetSpan().CopyTo(toHash.Slice(PublicKey.LENGTH));
            byte[] hashed = NBitcoin.Crypto.Hashes.SHA256(toHash);

            if (ECPubKey.TryCreate(basepoint, Context.Instance, out _, out ECPubKey? ecpubkey))
            {
                if (ecpubkey.TryAddTweak(hashed.AsSpan(), out ECPubKey? ecpubkeytweaked))
                {
                    if (ecpubkeytweaked != null)
                    {
                        Span<byte> pub = stackalloc byte[33];
                        ecpubkeytweaked.WriteToSpan(true, pub, out _);
                        return new PublicKey(pub.ToArray());
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// derive_simple_privkey
        /// privkey = basepoint_secret + SHA256(per_commitment_point || basepoint)
        /// </summary>
        public PrivateKey DerivePrivatekey(PrivateKey basepointSecret, PublicKey basepoint, PublicKey perCommitmentPoint)
        {
            Span<byte> toHash = stackalloc byte[PublicKey.LENGTH * 2];
            perCommitmentPoint.GetSpan().CopyTo(toHash);
            basepoint.GetSpan().CopyTo(toHash.Slice(PublicKey.LENGTH));
            byte[] hashed = NBitcoin.Crypto.Hashes.SHA256(toHash);

            if (ECPrivKey.TryCreate(basepointSecret, Context.Instance, out ECPrivKey? ecprvkey))
            {
                if (ecprvkey.TryTweakAdd(hashed.AsSpan(), out ECPrivKey? ecprvkeytweaked))
                {
                    if (ecprvkeytweaked != null)
                    {
                        Span<byte> prv = stackalloc byte[32];
                        ecprvkeytweaked.WriteToSpan(prv);
                        return new PrivateKey(prv.ToArray());
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// revocationpubkey = revocation_basepoint * SHA256(revocation_basepoint || per_commitment_point) + per_commitment_point * SHA256(per_commitment_point || revocation_basepoint)
        /// </summary>
        public PublicKey DeriveRevocationPublicKey(PublicKey basepoint, PublicKey perCommitmentPoint)
        {
            Span<byte> toHash1 = stackalloc byte[PublicKey.LENGTH * 2];
            basepoint.GetSpan().CopyTo(toHash1);
            perCommitmentPoint.GetSpan().CopyTo(toHash1.Slice(PublicKey.LENGTH));
            byte[] hashed1 = NBitcoin.Crypto.Hashes.SHA256(toHash1);

            ECPubKey? revocationBasepointTweaked = null;
            if (ECPubKey.TryCreate(basepoint, Context.Instance, out _, out ECPubKey? ecbasepoint))
            {
                if (ecbasepoint.TryTweakMul(hashed1.AsSpan(), out ECPubKey? ecpubkeytweaked))
                {
                    if (ecpubkeytweaked != null)
                    {
                        revocationBasepointTweaked = ecpubkeytweaked;
                    }
                }
            }

            Span<byte> toHash2 = stackalloc byte[PublicKey.LENGTH * 2];
            perCommitmentPoint.GetSpan().CopyTo(toHash2);
            basepoint.GetSpan().CopyTo(toHash2.Slice(PublicKey.LENGTH));
            byte[] hashed2 = NBitcoin.Crypto.Hashes.SHA256(toHash2);

            ECPubKey? perCommitmentPointTweaked = null;
            if (ECPubKey.TryCreate(perCommitmentPoint, Context.Instance, out _, out ECPubKey? ecperCommitmentPoint))
            {
                if (ecperCommitmentPoint.TryTweakMul(hashed2.AsSpan(), out ECPubKey? ecperCommitmentPointtweaked))
                {
                    if (ecperCommitmentPointtweaked != null)
                    {
                        perCommitmentPointTweaked = ecperCommitmentPointtweaked;
                    }
                }
            }

            if (revocationBasepointTweaked != null && perCommitmentPointTweaked != null)
            {
                var keys = new ECPubKey[] { revocationBasepointTweaked, perCommitmentPointTweaked };

                if (ECPubKey.TryCombine(Context.Instance, keys, out ECPubKey? revocationpubkey))
                {
                    if (revocationpubkey != null)
                    {
                        Span<byte> pub = stackalloc byte[33];
                        revocationpubkey.WriteToSpan(true, pub, out _);
                        return new PublicKey(pub.ToArray());
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// revocationpubkey = revocation_basepoint * SHA256(revocation_basepoint || per_commitment_point) + per_commitment_point * SHA256(per_commitment_point || revocation_basepoint)
        /// </summary>
        public PrivateKey DeriveRevocationPrivatekey(PublicKey basepoint, PrivateKey basepointSecret, PrivateKey perCommitmentSecret, PublicKey perCommitmentPoint)
        {
            Span<byte> toHash1 = stackalloc byte[PublicKey.LENGTH * 2];
            basepoint.GetSpan().CopyTo(toHash1);
            perCommitmentPoint.GetSpan().CopyTo(toHash1.Slice(PublicKey.LENGTH));
            byte[] hashed1 = NBitcoin.Crypto.Hashes.SHA256(toHash1);

            ECPrivKey? revocationBasepointSecretTweaked = null;
            if (ECPrivKey.TryCreate(basepointSecret, Context.Instance, out ECPrivKey? ecbasepointsecret))
            {
                if (ecbasepointsecret.TryTweakMul(hashed1.AsSpan(), out ECPrivKey? ecprivtweaked))
                {
                    if (ecprivtweaked != null)
                    {
                        revocationBasepointSecretTweaked = ecprivtweaked;
                    }
                }
            }

            Span<byte> toHash2 = stackalloc byte[PublicKey.LENGTH * 2];
            perCommitmentPoint.GetSpan().CopyTo(toHash2);
            basepoint.GetSpan().CopyTo(toHash2.Slice(PublicKey.LENGTH));
            byte[] hashed2 = NBitcoin.Crypto.Hashes.SHA256(toHash2);

            ECPrivKey? perCommitmentSecretTweaked = null;
            if (ECPrivKey.TryCreate(perCommitmentSecret, Context.Instance, out ECPrivKey? ecpercommitmentsecret))
            {
                if (ecpercommitmentsecret.TryTweakMul(hashed2.AsSpan(), out ECPrivKey? ecprivtweaked))
                {
                    if (ecprivtweaked != null)
                    {
                        perCommitmentSecretTweaked = ecprivtweaked;
                    }
                }
            }

            if (revocationBasepointSecretTweaked != null && perCommitmentSecretTweaked != null)
            {
                Span<byte> prvtpadd = stackalloc byte[32];
                perCommitmentSecretTweaked.WriteToSpan(prvtpadd);

                if (revocationBasepointSecretTweaked.TryTweakAdd(prvtpadd, out ECPrivKey? revocationprvkey))
                {
                    if (revocationprvkey != null)
                    {
                        Span<byte> prv = stackalloc byte[32];
                        revocationprvkey.WriteToSpan(prv);
                        return new PrivateKey(prv.ToArray());
                    }
                }
            }

            return null;
        }
    }
}