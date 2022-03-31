using Apocryph.Ipfs;

namespace Apocryph.Model
{
    public class AgentState
    {
        // public Reference Creation { get; private set; }
        public int Nonce { get; private set; }
        public AgentReferenceData Data { get; private set; }
        // public IMerkleTree<Reference> Subscriptions { get; private set; }
        public Hash<string> CodeHash { get; private set; }

        public AgentState(int nonce, AgentReferenceData data, Hash<string> codeHash)
        {
            Nonce = nonce;
            Data = data;
            CodeHash = codeHash;
        }
    }
}