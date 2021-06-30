using Lyn.Protocol.Bolt2.Entities;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Types.Bitcoin;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Bolt3
{
    public interface ILightningKeyDerivation
    {
        Secrets DeriveSecrets(Secret seed);

        Basepoints DeriveBasepoints(Secrets secrets);

        Secret PerCommitmentSecret(UInt256 shaseed, ulong perCommitIndex);

        PublicKey PerCommitmentPoint(UInt256 shaseed, ulong perCommitIndex);

        bool IsValidPublicKey(PublicKey publicKey);

        PublicKey PublicKeyFromPrivateKey(PrivateKey privateKey);

        /// <summary>
        /// derive_simple_key
        /// pubkey = basepoint + SHA256(per_commitment_point || basepoint) * G
        /// </summary>
        PublicKey DerivePublickey(PublicKey basepoint, PublicKey perCommitmentPoint);

        /// <summary>
        /// derive_simple_privkey
        /// privkey = basepoint_secret + SHA256(per_commitment_point || basepoint)
        /// </summary>
        PrivateKey DerivePrivatekey(PrivateKey basepointSecret, PublicKey basepoint, PublicKey perCommitmentPoint);

        /// <summary>
        /// revocationpubkey = revocation_basepoint * SHA256(revocation_basepoint || per_commitment_point) + per_commitment_point * SHA256(per_commitment_point || revocation_basepoint)
        /// </summary>
        PublicKey DeriveRevocationPublicKey(PublicKey basepoint, PublicKey perCommitmentPoint);

        /// <summary>
        /// revocationpubkey = revocation_basepoint * SHA256(revocation_basepoint || per_commitment_point) + per_commitment_point * SHA256(per_commitment_point || revocation_basepoint)
        /// </summary>
        PrivateKey DeriveRevocationPrivatekey(PublicKey basepoint, PrivateKey basepointSecret, PrivateKey perCommitmentSecret, PublicKey perCommitmentPoint);
    }
}