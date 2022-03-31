using Apocryph.Ipfs;
using Apocryph.Ipfs.MerkleTree;
using Apocryph.Model;

namespace Apocryph.Agents.Consensus.Snowball
{
    public record Block(
        Hash<Block>? Previous,
        IMerkleTree<AgentMessage> InputMessages,
        IMerkleTree<AgentMessage> OutputMessages,
        ChainState State);
}