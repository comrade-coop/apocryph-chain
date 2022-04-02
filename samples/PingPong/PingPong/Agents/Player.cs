using Apocryph.Ipfs;
using Apocryph.Ipfs.MerkleTree;
using Apocryph.Model;
using Perper.Extensions;
using Perper.Model;
using Serilog;

namespace PingPong.Agents;

public class Player
{
    private readonly IPerperContext _context;
    private readonly IHashResolver _hashResolver;

    public Player(IPerperContext context, IHashResolver hashResolver)
    {
        _context = context;
        _hashResolver = hashResolver;
    }

    public async Task Startup(string sampleAgentsConsensus)
    {
        var executorAgent = await PerperContext.StartAgentAsync("Executor");
        await executorAgent.CallAsync("Register", Hash.From("PlayerOne"), _context.CurrentAgent, "PlayerOneHitsTheBall");
        await executorAgent.CallAsync("Register", Hash.From("PlayerTwo"), _context.CurrentAgent, "PlayerTwoHitsTheBall");
        
        var agentStates = new[]
        {
            new AgentState(0, AgentReferenceData.From(new PlayerState()), Hash.From("PlayerOne")),
            new AgentState(1, AgentReferenceData.From(new PlayerState()), Hash.From("PlayerTwo"))
        };
        
        var agentStatesTree = await MerkleTreeBuilder.CreateRootFromValues(_hashResolver, agentStates, 2);

        var chain = new Chain(new ChainState(agentStatesTree, agentStates.Length), "LocalConsensus", null, 1);
        var chainId = await _hashResolver.StoreAsync(chain);
        
        var (_, kothStates) = await PerperContext.StartAgentAsync<PerperStream>("KoTH");
        var kothMiner = await PerperContext.StartAgentAsync("KoTHSimpleMiner");
        var minerTask = kothMiner.CallAsync("Mine", kothStates, new[] { chainId });

        var collectorStream = await PerperContext.BlankStream().StartAsync();

        var (routingAgent, _) = await PerperContext.StartAgentAsync<object?>("Routing", kothStates, executorAgent, collectorStream);

        var (chainInput, chainOutputs) = await routingAgent.CallAsync<(string, PerperStream)>("GetChainInstance", chainId);

        Log.Debug("{ChainInput}", chainInput);

        await routingAgent.CallAsync("PostMessage",
            new AgentMessage(new AgentReference(chainId, 0, new[] { typeof(HitTheBallMessage).FullName! }),
                AgentReferenceData.From(new HitTheBallMessage(
                    new AgentReference(chainId, 1, new[] { typeof(HitTheBallMessage).FullName! }), "START! ", 0))));
    }
    
    public (AgentState, AgentMessage[]) PlayerOneHitsTheBall(Hash<Chain> chain, AgentState agentState, AgentMessage agentMessage)
    {
        var state = agentState.Data.Deserialize<PlayerState>();
        var outputMessages = new List<AgentMessage>();
        
        if (agentMessage.Data.Type == typeof(HitTheBallMessage).FullName)
        {
            var message = agentMessage.Data.Deserialize<HitTheBallMessage>();
            
            Log.Debug("AgentOne {Content}", message.Content);
            Log.Debug("AgentOneState {Accumulator}", state.Accumulator);

            state.Accumulator += message.Content.Length;

            outputMessages.Add(new AgentMessage(
                message.Callback,
                AgentReferenceData.From(new HitTheBallMessage(
                    new AgentReference(chain, agentState.Nonce, new[] { typeof(HitTheBallMessage).FullName! }),
                    "PONG! " + message.Content,
                    state.Accumulator
                ))
            ));
        }

        return (new AgentState(agentState.Nonce, AgentReferenceData.From(state), agentState.CodeHash), outputMessages.ToArray());
    }

    public (AgentState, AgentMessage[]) PlayerTwoHitsTheBall(Hash<Chain> chain, AgentState agentState, AgentMessage agentMessage)
    {
        var state = agentState.Data.Deserialize<PlayerState>();
        var outputMessages = new List<AgentMessage>();
        if (agentMessage.Data.Type == typeof(HitTheBallMessage).FullName)
        {
            var message = agentMessage.Data.Deserialize<HitTheBallMessage>();
            Log.Debug("AgentTwo {Content}", message.Content);
            Log.Debug("AgentTwoState {Accumulator}", state.Accumulator);

            state.Accumulator += message.AccumulatedValue;

            outputMessages.Add(new AgentMessage(
                message.Callback,
                AgentReferenceData.From(new HitTheBallMessage(
                    new AgentReference(chain, agentState.Nonce, new[] { typeof(HitTheBallMessage).FullName! }),
                    state.Accumulator.ToString(),
                    state.Accumulator
                ))
            ));
        }

        return (new AgentState(agentState.Nonce, AgentReferenceData.From(state), agentState.CodeHash), outputMessages.ToArray());
    }
}