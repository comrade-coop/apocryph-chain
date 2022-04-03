using System.Collections.Generic;
using Apocryph.Ipfs.Fake;
using Apocryph.Ipfs.Test;
using Apocryph.Model;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Moq;
using Perper.Model;
using Xunit;

namespace Apocryph.Executor.Test
{
    public class ExecutorTests
    {
        [Fact]
        public async void TestAgentScenario_ProducesExpectedMessages()
        {
            var hashResolver = new FakeHashResolver();
            
            var perperContextMock = new Mock<IPerperContext>();
            
            var executor = new Agents.Executor.Executor(perperContextMock.Object);
            executor.Register()
          
            var (chain, inputMessages, expectedOutputMessages) = await ExecutorFakes.GetTestAgentScenario(hashResolver, "-", null, 1);
            var chainId = await hashResolver.StoreAsync(chain);

            var agentStates = await chain.GenesisState.AgentStates.EnumerateItems(hashResolver).ToDictionaryAsync(x => x.Nonce, x => x);

            var outputMessages = new List<AgentMessage>();

            foreach (var inputMessage in inputMessages)
            {
                var inputState = agentStates[inputMessage.Target.AgentNonce];
                var (resultState, resultMessages) = await executor.CallFunctionAsync<(AgentState, Message[])>("Execute", (chainId, inputState, inputMessage));

                Assert.Equal(inputState, resultState, SerializedComparer.Instance);

                outputMessages.AddRange(resultMessages);
            }

            Assert.Equal(outputMessages.ToArray(), expectedOutputMessages, SerializedComparer.Instance);

        }
    }
}