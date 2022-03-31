using System.Threading.Tasks;
using Apocryph.Ipfs;
using Apocryph.Ipfs.MerkleTree;
using Apocryph.Model;

namespace Apocryph.Tests.Executor
{
    public static class ExecutorFakes
    {
        /*
        public static (Hash<string>, Func<(Hash<Chain>, AgentState, Message), Task<(AgentState, Message[])>>)[] TestAgents = new (Hash<string>, Func<(Hash<Chain>, AgentState, Message), Task<(AgentState, Message[])>>)[]
        {
            (Hash.From("AgentInc"), ((Hash<Chain>, AgentState state, Message message) input) =>
            {
                var target = input.state.Data.Deserialize<Reference>();
                var result = input.message.Data.Deserialize<int>() + 1;
                return Task.FromResult((input.state, new[] { new Message(target, ReferenceData.From(result)) }));
            }),

            (Hash.From("AgentDec"), ((Hash<Chain>, AgentState state, Message message) input) =>
            {
                var target = input.state.Data.Deserialize<Reference>();
                var result = input.message.Data.Deserialize<int>() - 1;
                return Task.FromResult((input.state, new[] { new Message(target, ReferenceData.From(result)) }));
            })
        };*/


        public static async Task<(Chain chain, AgentMessage[] input, AgentMessage[] output)> GetTestAgentScenario(IHashResolver hashResolver, string consensusType, Hash<object>? consensusParameters, int slotsCount)
        {
            var messageFilter = new [] { typeof(int).FullName! };
            var fakeChainId = Hash.From("123").Cast<Chain>();

            var agentStates = new[] {
                new AgentState(0, AgentReferenceData.From(new AgentReference(fakeChainId, 0, messageFilter)), Hash.From("AgentInc")),
                new AgentState(1, AgentReferenceData.From(new AgentReference(fakeChainId, 1, messageFilter)), Hash.From("AgentDec"))
            };

            var agentStatesTree = await MerkleTreeBuilder.CreateRootFromValues(hashResolver, agentStates, 2);

            var chain = new Chain(new ChainState(agentStatesTree, agentStates.Length), consensusType, consensusParameters, slotsCount);

            var chainId = await hashResolver.StoreAsync(chain);

            var inputMessages = new[]
            {
                new AgentMessage(new AgentReference(chainId, 0, messageFilter), AgentReferenceData.From(4)),
                new AgentMessage(new AgentReference(chainId, 1, messageFilter), AgentReferenceData.From(3)),
            };

            var expectedOutputMessages = new[]
            {
                new AgentMessage(new AgentReference(fakeChainId, 0, messageFilter), AgentReferenceData.From(5)),
                new AgentMessage(new AgentReference(fakeChainId, 1, messageFilter), AgentReferenceData.From(2)),
            };

            return (chain, inputMessages, expectedOutputMessages);

        }
    }
}