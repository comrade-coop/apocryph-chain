using Apocryph.Agents;
using Apocryph.Agents.Consensus.Snowball;
using Apocryph.Ipfs;
using Apocryph.Ipfs.MerkleTree;
using Apocryph.Model;
using Microsoft.Extensions.Logging;
using Perper.Extensions;
using Perper.Model;
using Serilog;

namespace SampleAgents.Agents
{
    public class PingPong : DependencyAgent
    {
        //private readonly ILogger _logger;
        //private readonly IHashResolver _hashResolver;

        /*public PingPong(ILogger logger, IHashResolver hashResolver)
        {
            _logger = logger;
            _hashResolver = hashResolver;
        }*/

        public class PlayerOneState
        {
            public int Accumulator { get; set; } = 0;
        }

        public class PlayerTwoState
        {
            public int Accumulator { get; set; } = 0;
        }

        public async Task Startup(string sampleAgentsConsensus)
        {
            var (executorAgent, _) = await PerperContext.StartAgentAsync<object?>("Executor");
            await executorAgent.CallAsync("Register", Hash.From("PlayerOne"), PerperContext.Agent, "PlayerOne");
            await executorAgent.CallAsync("Register", Hash.From("PlayerTwo"), PerperContext.Agent, "PlayerTwo");
          
            var agentStates = new[] {
                new AgentState(0, AgentReferenceData.From(new PlayerOneState()), Hash.From("PlayerOne")),
                new AgentState(1, AgentReferenceData.From(new PlayerTwoState()), Hash.From("PlayerTwo"))
            };

            var agentStatesTree = await MerkleTreeBuilder.CreateRootFromValues(_hashResolver, agentStates, 2);

            Chain chain;

            if (sampleAgentsConsensus == "Local")
            {
                chain = new Chain(new ChainState(agentStatesTree, agentStates.Length), "LocalConsensus", null, 1);
            }
            else
            {
                var snowballParameters = await _hashResolver.StoreAsync<object>(new SnowballParameters(15, 0.8, 25));
                chain = new Chain(new ChainState(agentStatesTree, agentStates.Length), "SnowballConsensus", snowballParameters, 60);
            }

            var chainId = await _hashResolver.StoreAsync(chain);
            
            var (_, kothStates) = await PerperContext.StartAgentAsync<PerperStream>("KoTH");
            var kothMiner = await PerperContext.StartAgentAsync("KoTHSimpleMiner");
            var minerTask =  kothMiner.CallAsync("Mine", kothStates, new [] { chainId });

            var collectorStream = await PerperContext.BlankStream().StartAsync();
            
            var (routingAgent, _) = await PerperContext.StartAgentAsync<object?>("Routing", kothStates, executorAgent, collectorStream);

            var (chainInput, chainOutputs) = await routingAgent.CallAsync<(string, PerperStream)>("GetChainInstance", chainId);

            System.Console.WriteLine(chainInput);
            
            await routingAgent.CallAsync("PostMessage",
                new AgentMessage(new AgentReference(chainId, 0, new[] { typeof(PingPongMessage).FullName! }),
                    AgentReferenceData.From(
                        new PingPongMessage(
                            callback: new AgentReference(chainId, 1, new[] { typeof(PingPongMessage).FullName! }),
                            content: "START! ",
                            accumulatedValue: 0
                        ))
                )
            );
        }

        public (AgentState, AgentMessage[]) PlayerOne(Hash<Chain> chain, AgentState agentState, AgentMessage agentMessage)
        {
            var state = agentState.Data.Deserialize<PlayerOneState>();
            var outputMessages = new List<AgentMessage>();
            if (agentMessage.Data.Type == typeof(PingPongMessage).FullName)
            {
                var message = agentMessage.Data.Deserialize<PingPongMessage>();
                _logger.LogDebug("AgentOne {Content}", message.Content);
                _logger.LogDebug("AgentOneState {Accumulator}", state.Accumulator);

                state.Accumulator += message.Content.Length;
                
                outputMessages.Add(new AgentMessage(
                    message.Callback,
                    AgentReferenceData.From(new PingPongMessage(
                        callback: new AgentReference(chain, agentState.Nonce, new[] { typeof(PingPongMessage).FullName! }),
                        content: "PONG! " + message.Content,
                        accumulatedValue: state.Accumulator
                    ))
                ));
            }
            return (new AgentState(agentState.Nonce, AgentReferenceData.From(state), agentState.CodeHash), outputMessages.ToArray());
        }

        public (AgentState, AgentMessage[]) PlayerTwo(Hash<Chain> chain, AgentState agentState, AgentMessage agentMessage)
        {
            var state = agentState.Data.Deserialize<PlayerTwoState>();
            var outputMessages = new List<AgentMessage>();
            if (agentMessage.Data.Type == typeof(PingPongMessage).FullName)
            {
                var message = agentMessage.Data.Deserialize<PingPongMessage>();
                _logger.LogDebug("AgentTwo {Content}", message.Content);
                _logger.LogDebug("AgentTwoState {Accumulator}", state.Accumulator);
                
                state.Accumulator += message.AccumulatedValue;
                
                outputMessages.Add(new AgentMessage(
                    message.Callback,
                    AgentReferenceData.From(new PingPongMessage(
                        callback: new AgentReference(chain, agentState.Nonce, new[] { typeof(PingPongMessage).FullName! }),
                        content: state.Accumulator.ToString(),
                        accumulatedValue: state.Accumulator
                    ))
                ));
            }

            return (new AgentState(agentState.Nonce, AgentReferenceData.From(state), agentState.CodeHash), outputMessages.ToArray());
        }
    }

}