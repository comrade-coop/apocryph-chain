using System;

namespace Apocryph.Consensus
{
    public class AgentMessage : IEquatable<AgentMessage>
    {
        //public Reference Source { get; private set; }
        public AgentReference Target { get; private set; }  // NOTE: Currently publications are encoded as negative Target.AgentNonce values
        public AgentReferenceData Data { get; private set; }
        // public DateTime SendTime { get; private set; }

        public AgentMessage()
        {
        }

        public AgentMessage(AgentReference target, AgentReferenceData data)
        {
            Target = target;
            Data = data;
        }

        public override bool Equals(object? other)
        {
            return other is AgentMessage otherMessage && Equals(otherMessage);
        }

        public bool Equals(AgentMessage? other)
        {
            return other != null && Target.Equals(other.Target) && Data.Equals(other.Data);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Target, Data);
        }
    }
}