using Apocryph.Ipfs;

namespace Apocryph.Agents.Consensus.Snowball
{
    public record Query(Hash<Block>? Value, int Round);
}