using System.Threading.Tasks;
using Apocryph.Ipfs;
using Apocryph.Ipfs.Fake;
using Apocryph.Ipfs.MerkleTree;
using Apocryph.Model;

namespace Apocryph.Tests;

public class ConsensusTestDataBuilder
{
    private readonly string _consensusType;
    private readonly Hash<object>? _consensusParameters;
    private readonly int _slotsCount;
    private readonly FakeHashResolver _hashResolver = new();

    public ConsensusTestDataBuilder(string consensusType, Hash<object>? consensusParameters, int slotsCount)
    {
        _consensusType = consensusType;
        _consensusParameters = consensusParameters;
        _slotsCount = slotsCount;
    }

    public async Task<(Hash<Chain> ChainId, Chain Chain, AgentState[]AgentStates, AgentMessage[] Input)> GetTestAgentScenario()
    {
        var fakeChainId = Hash.From("100").Cast<Chain>();
        var messageFilter = new string[] {  };

        var agentStates = new[] {
            new AgentState(0, AgentReferenceData.From(new AgentReference(fakeChainId, 0, messageFilter)), Hash.From("AgentInc")),
        };

        var agentStatesTree = await MerkleTreeBuilder.CreateRootFromValues(_hashResolver, agentStates, 2);
        var chain = new Chain(new ChainState(agentStatesTree, agentStates.Length), _consensusType, _consensusParameters, _slotsCount);

        var chainId = await _hashResolver.StoreAsync(chain);

        var inputMessages = new[]
        {
            new AgentMessage(new AgentReference(chainId, 0, messageFilter), AgentReferenceData.From(4)),
            new AgentMessage(new AgentReference(chainId, 1, messageFilter), AgentReferenceData.From(3)),
        };

        return (chainId, chain, agentStates, inputMessages);
    }

}