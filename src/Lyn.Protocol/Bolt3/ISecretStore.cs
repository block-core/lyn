namespace Lyn.Protocol.Bolt3
{
    public interface ISecretStore
    {
        Lyn.Types.Fundamental.Secret GetSeed();
    }
}