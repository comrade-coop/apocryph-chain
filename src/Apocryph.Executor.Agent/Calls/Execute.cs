using System.Diagnostics.CodeAnalysis;
using Apocryph.Consensus;
using Apocryph.Ipfs;
using Perper.Extensions;
using Perper.Model;

namespace Apocryph.Executor.Agent.Calls;

[SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse")]
public static class Execute
{
    public static async Task<(AgentState, AgentMessage[])> RunAsync(Hash<Chain> chainId, AgentState agentState, AgentMessage agentMessage)
    {
        var (handlerAgent, handlerFunction) = await PerperState.GetOrDefaultAsync<(PerperAgent, string)>(agentState.CodeHash.ToString());

        if (handlerAgent != null && handlerFunction != null)
        {
            return await handlerAgent.CallAsync<(AgentState, AgentMessage[])>(handlerFunction, chainId, agentState, agentMessage);
        }

        return (default, default);
    }
}