using Apocryph.Ipfs;
using Apocryph.Model;
using Perper.Extensions;
using Perper.Model;
using Serilog;

namespace Apocryph.Agents.Consensus.Local;

/// <summary>
/// Local consensus implementation. Use for development/testing purposes only.
/// </summary>
public class LocalConsensus : DependencyAgent
{
    //private readonly ILogger _logger;
    //private readonly IHashResolver _hashResolver;

   /* public LocalConsensus(ILogger logger, IHashResolver hashResolver)
    {
        _logger = logger;
        _hashResolver = hashResolver;
    }*/

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
        var stream = await PerperContext.Stream("LocalStream").Persistent().StartAsync(messages, subscriptionsStreamName, chain, kothStates, executor).ConfigureAwait(false);
        await PerperState.SetAsync("LocalStream", stream.Replay()).ConfigureAwait(false);
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
    public async IAsyncEnumerable<AgentMessage> LocalStream(PerperStream messages, string subscriptionsStreamName, Chain chain, PerperStream kothStates, PerperAgent executor)
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