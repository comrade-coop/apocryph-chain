using System;
using System.Diagnostics.CodeAnalysis;

namespace Apocryph.Agents.Testbed.Api
{
    [System.Serializable]
    public class AgentCapability: IEquatable<AgentCapability>
    {
        public string Issuer { get; set; }
        public Type[] MessageTypes { get; set; }

        public AgentCapability(string issuer, Type types)
        {
            Issuer = issuer;
            MessageTypes = new[] { types };
        }

        public AgentCapability(string issuer, Type[] types)
        {
            Issuer = issuer;
            MessageTypes = types;
        }

        public bool Equals([AllowNull] AgentCapability other)
        {
            throw new NotImplementedException();
        }
    }
}