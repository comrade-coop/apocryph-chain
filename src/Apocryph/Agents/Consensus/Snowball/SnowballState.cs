using System.Security.Cryptography;
using Apocryph.Ipfs;

namespace Apocryph.Agents.Consensus.Snowball
{
    public class SnowballState
    {
        public Hash<Block>? CurrentValue { get; protected set; }
        public Hash<Block>? LastValue { get; protected set; }
        public Dictionary<Hash<Block>, int> Memory { get; protected set; } = new();
        public int Confidence { get; protected set; }

        public void ProcessQuery(Hash<Block>? requestSuggestion)
        {
            if (CurrentValue == null)
            {
                CurrentValue = requestSuggestion;
                if (CurrentValue != null)
                {
                    Memory[CurrentValue] = 0;
                    Confidence = 0;
                }
            }
        }

        public IEnumerable<Peer> SamplePeers(SnowballParameters parameters, Peer[] peers)
        {
            return peers.OrderBy(_ => RandomNumberGenerator.GetInt32(peers.Length)).Take(parameters.K);
        }

        public bool ProcessResponses(SnowballParameters parameters, IEnumerable<Hash<Block>?> responses)
        {
            if (Confidence > parameters.Beta) return true;

            var threshold = parameters.Alpha * parameters.K;

            var responseValues = responses.GroupBy(response => response)
                .Where(group => group.Count() > threshold)
                .Select(group => group.Key);

            foreach (var responseValue in responseValues)
            {
                if (responseValue == null) continue; // NOTE: Should probably be retrying erroring results as per original Snowball paper

                Memory[responseValue] = Memory.TryGetValue(responseValue, out var answerCount) ? answerCount + 1 : 1;

                if (CurrentValue == null)
                {
                    CurrentValue = responseValue;
                }

                if (Memory[responseValue] > Memory[CurrentValue])
                {
                    CurrentValue = responseValue;
                }

                if (responseValue != LastValue)
                {
                    Confidence = 0;
                    LastValue = responseValue;
                }
                else
                {
                    if (Confidence++ > parameters.Beta) return true;
                }
            }

            return false;
        }
    }
}