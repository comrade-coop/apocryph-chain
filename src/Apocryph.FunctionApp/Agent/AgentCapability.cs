using System;
using System.Diagnostics.CodeAnalysis;

namespace Apocryph.FunctionApp.Agent
{
    public class AgentCapability: IEquatable<AgentCapability>
    {
        public string AgentId { get; }

        // Stub implementation
        public AgentCapability(string agentId)
        {
            AgentId = agentId;
        }

        public AgentCapability(object agent)
        {
        }

        public bool Equals([AllowNull] AgentCapability other)
        {
            throw new NotImplementedException();
        }
    }
}