using System.Collections.Generic;
using Lyn.Protocol.Bolt3.Types;
using Lyn.Types.Fundamental;

namespace Lyn.Protocol.Tests.Bolt3
{
    public class Bolt3CommitmentTestVectors
    {
        public string TestName;
        public bool LocalAnchorOutputs;
        public bool RemoteAnchorOutputs;
        public MiliSatoshis ToLocalMsat;
        public MiliSatoshis ToRemoteMsat;
        public Satoshis FeeratePerKw;
        public string OutputCommitTx;
        public (List<Htlc> htlcs, List<Htlc> invertedhtlcs) Htlcs;
        public List<string> HtlcTx;
    }
}