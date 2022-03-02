using Apocryph.Consensus.Snowball.FunctionApp;
using Apocryph.Ipfs;
using Apocryph.Ipfs.MerkleTree;
using Microsoft.Extensions.Logging;
using Perper.Extensions;
using Perper.Model;

namespace Apocryph.Consensus.Snowball.Agent;

public class SnowballStream
{
    private readonly IHashResolver _hashResolver;
    private readonly IPeerConnector _peerConnector;
    private readonly ILogger _logger;

    public SnowballStream(IHashResolver hashResolver, IPeerConnector peerConnector,  ILogger logger)
    {
        _hashResolver = hashResolver;
        _peerConnector = peerConnector;
        _logger = logger;
    }

    public async Task RunAsync(Hash<Chain> self, CancellationToken cancellationToken)
    {
        var selfPeer = await _peerConnector.Self;
        var chain = await _hashResolver.RetrieveAsync(self, cancellationToken);
        var parameters = await _hashResolver.RetrieveAsync(chain.ConsensusParameters!.Cast<SnowballParameters>(), cancellationToken);
        var emptyMessagesTree = await MerkleTreeBuilder.CreateRootFromValues(_hashResolver, new Message[] { }, 3);
        var genesisBlock = new Block(null, emptyMessagesTree, emptyMessagesTree, chain.GenesisState);
        var genesis = await _hashResolver.StoreAsync(genesisBlock);

        var queryPath = $"snowball/{self}";

        await _peerConnector.ListenQuery<Query, Query>(queryPath, async (peer, request) =>
        {
            var currentRound = await PerperState.GetOrDefaultAsync("currentRound", 0);
            // NOTE: Should be using some locking here
            var snowballState = await PerperState.GetOrDefaultAsync<SnowballState>($"snowballState-{request.Round}");
            snowballState.ProcessQuery(request.Value);
            var result = new Query(currentRound < request.Round ? null : snowballState.CurrentValue!, request.Round);
            await PerperState.SetAsync($"snowballState-{request.Round}", snowballState);

            return result;
        }, cancellationToken);

        async Task<(ChainState, IMerkleTree<Message>)> ExecuteBlock(ChainState chainState, IMerkleTree<Message> inputMessages)
        {
            var agentStates = await chainState.AgentStates.EnumerateItems(_hashResolver).ToDictionaryAsync(x => x.Nonce, x => x, cancellationToken: cancellationToken);
            var outputMesages = new List<Message>();

            await foreach (var message in inputMessages.EnumerateItems(_hashResolver).WithCancellation(cancellationToken))
            {
                var state = agentStates[message.Target.AgentNonce];

                var (newState, resultMessages) = await  PerperContext.CallAsync<(AgentState, Message[])>("Execute", (self, state, message));

                agentStates[message.Target.AgentNonce] = newState;
                outputMesages.AddRange(resultMessages);
            }

            var outputStatesTree = await MerkleTreeBuilder.CreateRootFromValues(_hashResolver, agentStates.Values.OrderBy(x => x.Nonce), 3);
            var outputState = new ChainState(outputStatesTree, chainState.NextAgentNonce);
            var outputMessagesTree = await MerkleTreeBuilder.CreateRootFromValues(_hashResolver, outputMesages, 3);

            return (outputState, outputMessagesTree);
        }

        async Task<bool> ValidateBlock(Block block, Hash<Block> expectedPrevious)
        {
            if (block.Previous != expectedPrevious)
            {
                return false;
            }

            var inputMessagesSet = await PerperState.GetOrDefaultAsync<List<Message>>("messagePool");
            await foreach (var inputMessage in block.InputMessages.EnumerateItems(_hashResolver).WithCancellation(cancellationToken))
            {
                if (!inputMessagesSet.Remove(inputMessage))
                    return false;
            }

            var previous = await _hashResolver.RetrieveAsync(expectedPrevious, cancellationToken);
            var (outputState, outputMessages) = await ExecuteBlock(previous.State, block.InputMessages);

            return Hash.From(block.State) == Hash.From(outputState) && Hash.From(block.OutputMessages) == Hash.From(outputMessages);
        }

        async Task<Block?> ProposeBlock(Hash<Block> previousHash)
        {
            var previous = await _hashResolver.RetrieveAsync(previousHash);
            var inputMessagesList = (await PerperState.GetOrDefaultAsync<List<Message>>("messagePool")).ToArray();

            if (inputMessagesList.Length == 0 && await PerperState.GetOrDefaultAsync("finished", false))
            {
                return null; // DEBUG: Used for testing purposes mainly
            }

            var inputMessages = await MerkleTreeBuilder.CreateRootFromValues(_hashResolver, inputMessagesList, 3);

            var (outputStates, outputMessages) = await ExecuteBlock(previous.State, inputMessages);

            return new Block(previousHash, inputMessages, outputMessages, outputStates);
        }

        var currentRound = await PerperState.GetOrDefaultAsync("currentRound", 0);

        // NOTE: Might benefit from locking
        while (true)
        {
            IEnumerable<Task<Query>> replyTasks;
            {
                var kothPeers = await PerperState.GetOrDefaultAsync<Peer[]>("kothPeers", new Peer[] { });
                if (kothPeers.Length == 0)
                {
                    await Task.Delay(100, cancellationToken);
                    continue;
                }

                var snowball = await PerperState.GetOrDefaultAsync<SnowballState>($"snowballState-{currentRound}");
                if (snowball.CurrentValue == null)
                {
                    // NOTE: Can have a mode to sync to current state first for extra speed
                    // NOTE: Can lower CPU usage pressure by calculating proposer order from (previousHash, currentRound)
                    if (kothPeers.Contains(selfPeer))
                    {
                        var previousHash = await PerperState.GetOrDefaultAsync<Hash<Block>>("lastBlock", genesis);
                        var newBlock = await ProposeBlock(previousHash);
                        if (newBlock == null) yield break; // DEBUG: Used for testing purposes mainly

                        var newBlockHash = await _hashResolver.StoreAsync(newBlock);

                        snowball = await PerperState.GetOrDefaultAsync<SnowballState>($"snowballState-{currentRound}");
                        snowball.ProcessQuery(newBlockHash);
                        await PerperState.SetAsync($"snowballState-{currentRound}", snowball);
                    }
                    await Task.Delay(100, cancellationToken);
                    continue;
                }

                var sampledPeers = snowball.SamplePeers(parameters, kothPeers);
                var query = new Query(snowball.CurrentValue, currentRound);
                replyTasks = sampledPeers.Select(peer => _peerConnector.SendQuery<Query, Query>(peer, queryPath, query));
            }

            var responses = (await Task.WhenAll(replyTasks)).Select(reply => reply.Value);

            Hash<Block>? finishedHash = null;
            {
                var snowball = await _state.GetValue<SnowballState>($"snowballState-{currentRound}");
                var finished = snowball.ProcessResponses(parameters, responses);

                if (finished)
                {
                    finishedHash = snowball.CurrentValue;
                }

                await _state.SetValue($"snowballState-{currentRound}", snowball);
            }

            if (finishedHash != null)
            {
                var finishedBlock = await hashResolver.RetrieveAsync(finishedHash);

                var previousHash = await _state.GetValue<Hash<Block>>("lastBlock", () => genesis);
                if (await ValidateBlock(finishedBlock, previousHash))
                {
                    previousHash = finishedHash;
                    await _state.SetValue("lastBlock", previousHash);

                    await foreach (var outputMessage in finishedBlock.OutputMessages.EnumerateItems(hashResolver))
                    {
                        yield return outputMessage;
                    }

                    var messagePool = await _state.GetValue<List<Message>>("messagePool");
                    await foreach (var processedMessage in finishedBlock.InputMessages.EnumerateItems(hashResolver))
                    {
                        messagePool.Remove(processedMessage);
                    }
                    await _state.SetValue("messagePool", messagePool);
                }

                logger?.LogDebug("Finished round {currentRound}; block: {previousHash}", currentRound, previousHash.ToString().Substring(0, 16));

                await _state.SetValue("currentRound", ++currentRound);
            }
        }
    }
}