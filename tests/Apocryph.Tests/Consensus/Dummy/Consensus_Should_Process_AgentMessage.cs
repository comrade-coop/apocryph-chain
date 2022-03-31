using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apocryph.Agents;
using Apocryph.Agents.Consensus;
using Apocryph.Agents.Consensus.Dummy;
using Apocryph.Agents.Executor;
using Apocryph.Ipfs;
using Apocryph.Ipfs.Fake;
using Apocryph.Ipfs.MerkleTree;
using Apocryph.Model;
using NUnit.Framework;
using Perper.Application;

namespace Apocryph.Tests.Consensus.Dummy
{
    [TestFixture]
    public class Consensus_Should_Process_AgentMessage
    {
        private static readonly ManualResetEvent ManualResetEvent = new(false);

        public static async Task<(Chain chain, AgentMessage[] input, AgentMessage[] output)> GetTestAgentScenario(Hash<object>? consensusParameters, int slotsCount)
        {
            var hashResolver = new FakeHashResolver();
            var consensusType = "Dummy";
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

        static async Task Init()
        {
            // Arrange
            var callbackAgentProxy = new CallbackAgentProxy();
            var executorAgentProxy = new ExecutorAgentProxy();
            var consensusAgentProxy = new DummyConsensusAgentProxy();

            await callbackAgentProxy.StartAgent();
            await executorAgentProxy.StartAgent();


            /*
             * (
        PerperStream messages,    --> from where are comming those messages ?
        string subscriptionsStream,
        Chain chain,
        PerperStream kothStates,
        PerperAgent executor) input
             *
             */

            await consensusAgentProxy.StartAgent(

                executorAgentProxy.Instance);

            // Act
            var testData = await new ConsensusTestDataBuilder("Dummy", null, 2).GetTestAgentScenario();
            await executorAgentProxy.Register(
                testData.AgentStates.First().CodeHash.ToString(),
                callbackAgentProxy.Instance,
                "Callback");

            var (resultState, resultMessages) = await executorAgentProxy.Execute(
                testData.ChainId,
                testData.AgentStates.First(),
                testData.Input.First());

            // Teardown
            await callbackAgentProxy.DestroyAgent();
            await executorAgentProxy.DestroyAgent();

            // Assert
            Assert.NotNull(resultState);
            Assert.NotNull(resultMessages);

            ManualResetEvent.Set();
        }

        [Test]
        public void Should_Pass()
        {
            ManualResetEvent.WaitOne(TimeSpan.FromMinutes(1));
        }

        [OneTimeSetUp]
        public Task OneTimeSetup()
        {
            new PerperStartup()
                .AddClassHandlers<ExecutorAgent>()
                .AddClassHandlers<CallbackAgent>()
                .AddClassHandlers<DummyConsensus>()
                .AddInitHandler("", Init)
                .RunAsync(default)
                .ConfigureAwait(false);

            return Task.CompletedTask;
        }

        public class CallbackAgentProxy : AgentProxy<CallbackAgent> { }

        public class CallbackAgent
        {
            public (AgentState, AgentMessage[]) Callback(Hash<Chain> chainId, AgentState agentState, AgentMessage inputAgentMessage) => new(agentState, Array.Empty<AgentMessage>());
        }
    }
}



