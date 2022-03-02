using Apocryph.Ipfs;
using Perper.Extensions;
using Perper.Model;

namespace Apocryph.Consensus.Dummy.Agent.Streams;

public class ExecutionStream
{
    private readonly Chain _chain;
    private readonly IHashResolver _hashResolver;

    public ExecutionStream(Chain chain, IHashResolver hashResolver)
    {
        _chain = chain;
        _hashResolver = hashResolver;
    }

    public async IAsyncEnumerable<Message> RunAsync(IAsyncEnumerable<Message> messages)
    {
        var agentStates = await _chain.GenesisState.AgentStates.EnumerateItems(_hashResolver).ToDictionaryAsync(x => x.Nonce, x => x);
        var self = Hash.From(_chain);

        await foreach (var message in messages)
        {
            if (!message.Target.AllowedMessageTypes.Contains(message.Data.Type)) // NOTE: Should probably get handed by routing/execution instead
                continue;

            var state = agentStates[message.Target.AgentNonce];

            var (newState, resultMessages) = await PerperContext.CallAsync<(AgentState, Message[])>("Execute", (self, state, message));

            agentStates[message.Target.AgentNonce] = newState;

            foreach (var resultMessage in resultMessages)
            {
                yield return resultMessage;
            }
        }
    }
}