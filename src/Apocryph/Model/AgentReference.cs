using Apocryph.Ipfs;

namespace Apocryph.Model
{
    public class AgentReference : IEquatable<AgentReference>
    {
        public Hash<Chain> Chain { get; private set; }
        public int AgentNonce { get; private set; }
        public string[] AllowedMessageTypes { get; private set; }
        // public MerkleTreeProof<Message> Source { get; private set; }

        public AgentReference(Hash<Chain> chain, int agentNonce, string[] allowedMessageTypes)
        {
            Chain = chain;
            AgentNonce = agentNonce;
            AllowedMessageTypes = allowedMessageTypes;
        }

        public override bool Equals(object? other)
        {
            return true; //other is Reference otherReference && Equals(otherReference);
        }

        public bool Equals(AgentReference? other)
        {
            return true;// other != null && Chain.Equals(other.Chain) && AgentNonce.Equals(other.AgentNonce) && AllowedMessageTypes.SequenceEqual(other.AllowedMessageTypes);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Chain);
            hashCode.Add(AgentNonce);
            Array.ForEach(AllowedMessageTypes, hashCode.Add);
            return 0; //hashCode.ToHashCode();
        }
    }
}