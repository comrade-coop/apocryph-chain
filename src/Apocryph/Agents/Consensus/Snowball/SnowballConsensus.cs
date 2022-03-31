using System.Runtime.CompilerServices;
using Apocryph.Ipfs;
using Apocryph.Ipfs.MerkleTree;
using Apocryph.Model;
using Perper.Extensions;
using Perper.Model;
using Serilog;

namespace Apocryph.Agents.Consensus.Snowball
{
    public class SnowballConsensus : DependencyAgent
    {
        //private readonly ILogger _logger;
        //private readonly IHashResolver _hashResolver;
        //private readonly IPeerConnector _peerConnector;

        /*public SnowballConsensus(ILogger logger, IHashResolver hashResolver, IPeerConnector peerConnector)
        {
            _logger = logger;
            _hashResolver = hashResolver;
            _peerConnector = peerConnector;
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
        public async Task<PerperStream> StartupAsync(PerperStream messages, string subscriptionsStreamName, Chain chain, PerperStream kothStates, PerperAgent executor, CancellationToken cancellationToken = default)
        {
            var self = await _hashResolver.StoreAsync(chain, cancellationToken);
            var messagePoolTask = PerperContext.Stream("MessagePool").StartAsync(messages, self);
            var kothTask = PerperContext.Stream("KothProcessor").StartAsync(self, kothStates);
            return await PerperContext.Stream("SnowballStream").Action().StartAsync(self, executor, cancellationToken);
        }

        /// <summary>
        /// Snowball stream
        /// </summary>
        /// <param name="self"></param>
        /// <param name="executor"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<AgentMessage> SnowballStream(Hash<Chain> self, PerperAgent executor, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var selfPeer = await _peerConnector.Self;
            var chain = await _hashResolver.RetrieveAsync(self, cancellationToken);
            var parameters = await _hashResolver.RetrieveAsync(chain.ConsensusParameters!.Cast<SnowballParameters>(), cancellationToken);
            var emptyMessagesTree = await MerkleTreeBuilder.CreateRootFromValues(_hashResolver, Array.Empty<AgentMessage>(), 3);
            var genesisBlock = new Block(null, emptyMessagesTree, emptyMessagesTree, chain.GenesisState);
            var genesis = await _hashResolver.StoreAsync(genesisBlock, cancellationToken);

            var queryPath = $"snowball/{self}";

            await _peerConnector.ListenQuery<Query, Query>(queryPath, async (peer, request) =>
            {
                var currentRound = await PerperState.GetOrDefaultAsync<int>("currentRound");
                // NOTE: Should be using some locking here
                var snowballState = await PerperState.GetOrDefaultAsync<SnowballState>($"snowballState-{request.Round}");
                snowballState.ProcessQuery(request.Value);
                var result = new Query(currentRound < request.Round ? null : snowballState.CurrentValue!, request.Round);
                await PerperState.SetAsync($"snowballState-{request.Round}", snowballState);

                return result;
            }, cancellationToken);

            async Task<(ChainState, IMerkleTree<AgentMessage>)> ExecuteBlock(ChainState chainState, IMerkleTree<AgentMessage> inputMessages)
            {
                var agentStates = await chainState.AgentStates.EnumerateItems(_hashResolver).ToDictionaryAsync(x => x.Nonce, x => x, cancellationToken: cancellationToken);
                var outputMessages = new List<AgentMessage>();

                await foreach (var message in inputMessages.EnumerateItems(_hashResolver).WithCancellation(cancellationToken))
                {
                    var state = agentStates[message.Target.AgentNonce];

                    var (newState, resultMessages) = await executor.CallAsync<(AgentState, AgentMessage[])>("Execute", self, state, message);

                    agentStates[message.Target.AgentNonce] = newState;
                    outputMessages.AddRange(resultMessages);
                }

                var outputStatesTree = await MerkleTreeBuilder.CreateRootFromValues(_hashResolver, agentStates.Values.OrderBy(x => x.Nonce), 3);
                var outputState = new ChainState(outputStatesTree, chainState.NextAgentNonce);
                var outputMessagesTree = await MerkleTreeBuilder.CreateRootFromValues(_hashResolver, outputMessages, 3);

                return (outputState, outputMessagesTree);
            }

            async Task<bool> ValidateBlock(Block block, Hash<Block> expectedPrevious)
            {
                if (block.Previous != expectedPrevious)
                {
                    return false;
                }

                var inputMessagesSet = await PerperState.GetOrDefaultAsync<List<AgentMessage>>("messagePool");
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
                var previous = await _hashResolver.RetrieveAsync(previousHash, cancellationToken);
                var inputMessagesList = (await PerperState.GetOrDefaultAsync<List<AgentMessage>>("messagePool")).ToArray();

                if (inputMessagesList.Length == 0 && await PerperState.GetOrDefaultAsync<bool>("finished"))
                {
                    return null; // DEBUG: Used for testing purposes mainly
                }

                var inputMessages = await MerkleTreeBuilder.CreateRootFromValues(_hashResolver, inputMessagesList, 3);

                var (outputStates, outputMessages) = await ExecuteBlock(previous.State, inputMessages);

                return new Block(previousHash, inputMessages, outputMessages, outputStates);
            }

            var currentRound = await PerperState.GetOrDefaultAsync<int>("currentRound");

            // NOTE: Might benefit from locking
            while (true)
            {
                IEnumerable<Task<Query>> replyTasks;
                {
                    var kothPeers = await PerperState.GetOrDefaultAsync<Peer[]>("kothPeers");
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

                            var newBlockHash = await _hashResolver.StoreAsync(newBlock, cancellationToken);

                            snowball = await PerperState.GetOrDefaultAsync<SnowballState>($"snowballState-{currentRound}");
                            snowball.ProcessQuery(newBlockHash);
                            await PerperState.SetAsync($"snowballState-{currentRound}", snowball);
                        }

                        await Task.Delay(100, cancellationToken);
                        continue;
                    }
                    var sampledPeers = snowball.SamplePeers(parameters, kothPeers);
                    var query = new Query(snowball.CurrentValue, currentRound);
                    replyTasks = sampledPeers.Select(peer => _peerConnector.SendQuery<Query, Query>(peer, queryPath, query, cancellationToken));
                }

                var responses = (await Task.WhenAll(replyTasks)).Select(reply => reply.Value);

                Hash<Block>? finishedHash = null;
                {
                    var snowball = await PerperState.GetOrDefaultAsync<SnowballState>($"snowballState-{currentRound}");
                    var finished = snowball.ProcessResponses(parameters, responses);

                    if (finished)
                    {
                        finishedHash = snowball.CurrentValue;
                    }

                    await PerperState.SetAsync($"snowballState-{currentRound}", snowball);
                }

                if (finishedHash != null)
                {
                    var finishedBlock = await _hashResolver.RetrieveAsync(finishedHash, cancellationToken);

                    var previousHash = await PerperState.GetOrDefaultAsync("lastBlock", genesis);
                    if (await ValidateBlock(finishedBlock, previousHash))
                    {
                        previousHash = finishedHash;
                        await PerperState.SetAsync("lastBlock", previousHash);

                        await foreach (var outputMessage in finishedBlock.OutputMessages.EnumerateItems(_hashResolver).WithCancellation(cancellationToken))
                        {
                            yield return outputMessage;
                        }

                        var messagePool = await PerperState.GetOrDefaultAsync<List<AgentMessage>>("messagePool");
                        await foreach (var processedMessage in finishedBlock.InputMessages.EnumerateItems(_hashResolver).WithCancellation(cancellationToken))
                        {
                            messagePool.Remove(processedMessage);
                        }
                        await PerperState.SetAsync("messagePool", messagePool);
                    }

                    Log.Debug("Finished round {CurrentRound}; block: {PreviousHash}", currentRound, previousHash.ToString().Substring(0, 16));

                    await PerperState.SetAsync("currentRound", ++currentRound);
                }
            }
        }

        /// <summary>
        /// Call starts task for consuming agent messages from stream and appending them to messagePool state
        /// </summary>
        /// <param name="messages">stream of agent messages</param>
        /// <param name="token">cancellation token</param>
        public async Task MessagePool(PerperStream messages, CancellationToken token = default)
        {
            await foreach (var message in messages.EnumerateAsync<AgentMessage>(cancellationToken: token))
            {
                if (!message.Target.AllowedMessageTypes.Contains(message.Data.Type)) // NOTE: Should probably get handed by routing/execution instead
                    continue;

                var messagePool = await PerperState.GetOrDefaultAsync<List<AgentMessage>>("messagePool");
                messagePool.Add(message);
                await PerperState.SetAsync("messagePool", messagePool);
            }

            await PerperState.SetAsync("finished", true); // DEBUG: Used for testing purposes mainly
        }

        /// <summary>
        /// Call starts a task for processing kothStates stream. Updates kothPeers for given chain
        /// </summary>
        /// <param name="chain">chain</param>
        /// <param name="kothStates">stream of kothStates</param>
        /// <param name="token">cancellation token</param>
        public async Task KothProcessor(Hash<Chain> chain, PerperStream kothStates, CancellationToken token = default)
        {
            await foreach (var (_chain, _slot) in kothStates.EnumerateAsync<(Hash<Chain>, Slot?[])>(cancellationToken: token))
            {
                if (chain != _chain)
                    continue;

                var peers = _slot.Where(x => x != null).Select(x => x!.Peer).ToArray();

                await PerperState.SetAsync("kothPeers", peers);
            }
        }
    }
}