using System.Diagnostics.CodeAnalysis;
using Apocryph.Consensus;
using Apocryph.Ipfs;
using Perper.Extensions;
using Perper.Model;

namespace Apocryph.Executor.Agent.Calls;

[SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse")]
public static class Execute
{
    public static async Task<(AgentState, Message[])> RunAsync(Hash<Chain> chain, AgentState agentState, Message message)
    {
        var (handlerAgent, handlerFunction) = await PerperState.GetOrDefaultAsync<(PerperAgent, string)>(agentState.CodeHash.ToString());

        if (handlerAgent != null && handlerFunction != null)
        {
            return await handlerAgent.CallAsync<(AgentState, Message[])>(handlerFunction, (chain, agentState, message));
        }

        return (default, default);
    }
}