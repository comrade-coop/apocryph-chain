using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apocryph.Consensus;
using Apocryph.Executor.Test;
using Apocryph.Ipfs;
using Apocryph.Ipfs.Fake;
using Apocryph.Ipfs.MerkleTree;
using NUnit.Framework;
using Perper.Extensions;
using Perper.Model;

namespace Apocryph.Executor.Agent.Tests
{
    public class ExecutorTests
    {
        private static Hash<string> codeHash = null!;

        [Test, Order(1)]
        public async Task Register_Should_Pass()
        {
            var hashResolver = new FakeHashResolver();
            codeHash = Hash.From("CallbackAgent");

            PerperAgent agentToStore = null!;
            var handlerFunction = "Callback";

            ExecutionContext.Run(Agents.StubAgent.Calls.Startup.ExecutionContext, _ =>
                {
                    agentToStore = PerperContext.Agent;
                },
                null);

            ExecutionContext.Run(Apocryph.Executor.Agent.Calls.Startup.ExecutionContext, _ =>
            {
                PerperContext.Agent.CallAsync("Register", codeHash.ToString(), agentToStore, handlerFunction)
                    .GetAwaiter()
                    .GetResult();

                var (agent, functionName) = PerperState.GetOrDefaultAsync<(PerperAgent, string)>(codeHash.ToString())
                    .GetAwaiter()
                    .GetResult();

                Assert.That(agent.Agent ==  agentToStore.Agent);
                Assert.That(agent.Instance ==  agentToStore.Instance);
                Assert.That(functionName == handlerFunction);

            }, null);
        }

        [Test, Order(2)]
        public async Task Execute_Should_Pass()
        {
            var hashResolver = new FakeHashResolver();

            var (chain, inputMessages, expectedOutputMessages) = await ExecutorFakes.GetTestAgentScenario(hashResolver, "-", null, 1);
            var chainId = await hashResolver.StoreAsync(chain);
            var agentStates = await chain.GenesisState.AgentStates.EnumerateItems(hashResolver).ToDictionaryAsync(x => x.Nonce, x => x);
            var outputMessages = new List<Message>();

            foreach (var inputMessage in inputMessages)
            {
                var inputState = agentStates[inputMessage.Target.AgentNonce];

                ExecutionContext.Run(Apocryph.Executor.Agent.Calls.Startup.ExecutionContext, _ =>
                {
                    var (resultState, resultMessages) = PerperContext.Agent.CallAsync<(AgentState, Message[])>("Execute", chainId, inputState, inputMessage).GetAwaiter()
                        .GetResult();
                }, null);

                //var (resultState, resultMessages) = await executor.CallFunctionAsync<(AgentState, Message[])>("Execute", (chainId, inputState, inputMessage));
                //Assert.Equal(inputState, resultState, SerializedComparer.Instance);

                //outputMessages.AddRange(resultMessages);
            }



        }
    }
}



