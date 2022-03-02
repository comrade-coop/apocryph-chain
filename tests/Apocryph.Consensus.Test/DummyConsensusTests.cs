using System.Linq;
using Apocryph.Consensus.Dummy.Agent.Streams;
using Apocryph.Executor.Test;
using Apocryph.Ipfs.Fake;
using Apocryph.Ipfs.Test;
using Xunit;
using Xunit.Abstractions;

namespace Apocryph.Consensus.Test
{
    public class ExecutionStreamTests
    {
        private readonly ITestOutputHelper _output;
        public ExecutionStreamTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async void ExecutionStream_Returns_ExpectedMessages()
        {
            var hashResolver = new FakeHashResolver();
            // var executor = await ExecutorFakes.GetExecutor(ExecutorFakes.TestAgents);
            var (chain, inputMessages, expectedOutputMessages) = await ExecutorFakes.GetTestAgentScenario(hashResolver, "Apocryph-DummyConsensus", null, 1);

            var executionStream = new ExecutionStream(chain, hashResolver);
            var outputMessages = await executionStream.RunAsync(inputMessages.ToAsyncEnumerable()).ToArrayAsync();

            Assert.Equal(outputMessages, expectedOutputMessages, SerializedComparer.Instance);
        }
    }
}