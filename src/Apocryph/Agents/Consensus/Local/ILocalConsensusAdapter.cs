using Apocryph.Ipfs;
using Apocryph.Model;
using Perper.Extensions;
using Perper.Model;
using PerperState = Perper.Extensions.PerperState;

namespace Apocryph.Agents.Consensus.Local;

public interface ILocalConsensusAdapter
{
    Task<(AgentState, AgentMessage[])> ExecutorAgentExecuteCall(PerperAgent executor, Hash<Chain> self, AgentState state, AgentMessage message)
    {
        return executor.CallAsync<(AgentState, AgentMessage[])>("Execute", self, state, message);    
    }
    
    PerperStream ConsensusStream
    {
        get =>
            PerperState
                .GetOrDefaultAsync<PerperStream>("ConsensusStream")
                .GetAwaiter()
                .GetResult();
        
        set => PerperState.SetAsync("ConsensusStream", value)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    async Task<PerperStream> StartLocalConsensusStream(PerperStream messages, string subscriptionsStreamName, Chain chain,
        PerperStream kothStates, PerperAgent executor)
    {
        return await PerperContext.Stream("LocalConsensusStream")
            .Persistent()
            .StartAsync(messages, subscriptionsStreamName, chain, kothStates, executor)
            .ConfigureAwait(false);
    }

    IAsyncEnumerable<AgentMessage> EnumerateMessages(PerperStream messages)
    {
        return messages.EnumerateAsync<AgentMessage>();
    }
}