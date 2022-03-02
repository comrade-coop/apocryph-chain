using System;
using System.Threading.Tasks;
using Apocryph.Consensus;
using Apocryph.Ipfs;
using Apocryph.Ipfs.MerkleTree;

namespace Apocryph.Executor.Test
{
    public static class ExecutorFakes
    {
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
        };


        public static async Task<(Chain chain, Message[] input, Message[] output)> GetTestAgentScenario(IHashResolver hashResolver, string consensusType, Hash<object>? consensusParameters, int slotsCount)
        {
            var messageFilter = new string[] { typeof(int).FullName! };
            var fakeChainId = Hash.From("123").Cast<Chain>();

            var agentStates = new[] {
                new AgentState(0, ReferenceData.From(new Reference(fakeChainId, 0, messageFilter)), Hash.From("AgentInc")),
                new AgentState(1, ReferenceData.From(new Reference(fakeChainId, 1, messageFilter)), Hash.From("AgentDec"))
            };

            var agentStatesTree = await MerkleTreeBuilder.CreateRootFromValues(hashResolver, agentStates, 2);

            var chain = new Chain(new ChainState(agentStatesTree, agentStates.Length), consensusType, consensusParameters, slotsCount);

            var chainId = await hashResolver.StoreAsync(chain);

            var inputMessages = new Message[]
            {
                new Message(new Reference(chainId, 0, messageFilter), ReferenceData.From(4)),
                new Message(new Reference(chainId, 1, messageFilter), ReferenceData.From(3)),
            };

            var expectedOutputMessages = new Message[]
            {
                new Message(new Reference(fakeChainId, 0, messageFilter), ReferenceData.From(5)),
                new Message(new Reference(fakeChainId, 1, messageFilter), ReferenceData.From(2)),
            };

            return (chain, inputMessages, expectedOutputMessages);

        }
    }
}