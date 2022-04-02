using Apocryph.Ipfs;
using Apocryph.Model;
using Perper.Extensions;
using Perper.Model;

namespace Apocryph.Agents.Consensus.Local;

/// <summary>
/// Local consensus implementation. Use for development/testing purposes only.
/// </summary>
public class LocalConsensus
{
    private readonly IPerperContext _context;
    private readonly IHashResolver _hashResolver;

    private PerperStream ConsensusStream
    {
        get =>
            _context.CurrentState
                .GetOrDefaultAsync<PerperStream>("ConsensusStream")
                .GetAwaiter()
                .GetResult();
        
        set => _context.CurrentState.SetAsync("ConsensusStream", value)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }
    
    public LocalConsensus(IPerperContext context, IHashResolver hashResolver)
    {
        _context = context;
        _hashResolver = hashResolver;
    }
    
    /// <summary>
    /// Starts up the Agent
    /// </summary>
    /// <param name="messages">stream of AgentMessages</param>
    /// <param name="subscriptionsStreamName">subscription stream name</param>
    /// <param name="chain">chain data</param>
    /// <param name="kothStates">stream of kothStates</param>
    /// <param name="executor">agent executing </param>
    /// <returns>stream of AgentMessages</returns>
    public async Task<PerperStream> StartupAsync(PerperStream messages, string subscriptionsStreamName, Chain chain, PerperStream kothStates, PerperAgent executor)
    {
        var stream = ConsensusStream =
            await PerperContext.Stream("LocalConsensusStream")
                .Persistent()
                .StartAsync(messages, subscriptionsStreamName, chain, kothStates, executor)
                .ConfigureAwait(false);
        
        return stream;
    }

    /// <summary>
    /// Local stream
    /// </summary>
    /// <param name="messages">stream of AgentMessages</param>
    /// <param name="subscriptionsStreamName">subscription name</param>
    /// <param name="chain">chain</param>
    /// <param name="kothStates">stream of kothStates</param>
    /// <param name="executor">executing agent</param>
    /// <returns></returns>
    public async IAsyncEnumerable<AgentMessage> LocalConsensusStream(PerperStream messages, string subscriptionsStreamName, Chain chain, PerperStream kothStates, PerperAgent executor)
    {
        var agentStates = await chain.GenesisState
            .AgentStates
            .EnumerateItems(_hashResolver)
            .ToDictionaryAsync(x => x.Nonce, x => x);

        var self = Hash.From(chain);

        await foreach (var message in messages.EnumerateAsync<AgentMessage>())
        {
            if (!message.Target.AllowedMessageTypes.Contains(message.Data.Type)) // NOTE: Should probably get handed by routing/execution instead
                continue;

            var state = agentStates[message.Target.AgentNonce];
            var (newState, resultMessages) = await executor.CallAsync<(AgentState, AgentMessage[])>("Execute", self, state, message);

            agentStates[message.Target.AgentNonce] = newState;

            foreach (var resultMessage in resultMessages)
            {
                yield return resultMessage;
            }
        }
    }
}