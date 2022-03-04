using System;
using System.Threading.Tasks;
using Apocryph.Consensus;
using Apocryph.Ipfs;

namespace Apocryph.Executor.Agent.Tests.Agents.StubAgent.Calls
{
    public static class Callback
    {
        public static Task<(AgentState, AgentMessage[])> RunAsync(Hash<Chain> chainId, AgentState agentState, AgentMessage inputAgentMessage)
        {
            return Task.FromResult<(AgentState, AgentMessage[])>(new(agentState, Array.Empty<AgentMessage>()));
        }
    }
}