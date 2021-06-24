using Lyn.Protocol.Common.Blockchain;
using Lyn.Types.Fundamental;
using System.Threading.Tasks;

namespace Lyn.Protocol.Bolt2.ChannelEstablishment
{
    public interface IStartOpenChannelService
    {
        Task StartOpenChannel(PublicKey nodeId,
            ChainParameters chainParameters,
            Satoshis fundingAmount,
            MiliSatoshis pushOnOpen,
            Satoshis channelReserveSatoshis);
    }
}