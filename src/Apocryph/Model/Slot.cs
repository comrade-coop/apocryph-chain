using Apocryph.Ipfs;

namespace Apocryph.Model
{
    public record Slot(Peer Peer, byte[] MinedData);

    public record KothStates(Hash<Chain> ChainHash, Slot?[] Slots);
}