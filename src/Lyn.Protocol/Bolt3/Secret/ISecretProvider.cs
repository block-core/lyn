namespace Lyn.Protocol.Bolt3.Secret
{
    public interface ISecretProvider
    {
        Lyn.Types.Fundamental.Secret GetSeed();
    }
}