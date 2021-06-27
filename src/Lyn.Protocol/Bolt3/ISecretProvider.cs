namespace Lyn.Protocol.Bolt3
{
    public interface ISecretProvider
    {
        Lyn.Types.Fundamental.Secret GetSeed();
    }
}