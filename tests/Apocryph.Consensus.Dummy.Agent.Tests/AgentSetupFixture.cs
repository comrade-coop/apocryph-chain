using System;
using System.Threading.Tasks;
using Apocryph.Ipfs;
using Apocryph.Shared;
using Apocryph.Testing;
using NUnit.Framework;
using Perper.Extensions;

namespace Apocryph.Consensus.Dummy.Agent.Tests.Callback.Calls
{
    public static class Init
    {
        public static async Task RunAsync()
        {
        }
    }

    public static class Startup
    {
        public static Task RunAsync()
        {
            ExecutionContexts.Add(PerperContext.Agent);
            return Task.CompletedTask;
        }
    }

    public static class Callback
    {
        public static Task<(AgentState, AgentMessage[])> RunAsync(Hash<Chain> chainId, AgentState agentState, AgentMessage inputAgentMessage) => Task.FromResult<(AgentState, AgentMessage[])>(new(agentState, Array.Empty<AgentMessage>()));
    }
}


namespace Apocryph.Consensus.Dummy.Agent.Tests
{
    [SetUpFixture]
    public class AgentSetupFixture
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            TestHelper.SetupAgent("AgentLauncher", typeof(Apocryph.Consensus.Dummy.Agent.Tests.AgentLauncher.Calls.Init));
            TestHelper.SetupAgent("Callback", typeof(Apocryph.Consensus.Dummy.Agent.Tests.Callback.Calls.Init));
            TestHelper.SetupAgent("Executor", typeof(Apocryph.Executor.Agent.Calls.Startup));
            TestHelper.SetupAgent("DummyConsensus", typeof(Apocryph.Consensus.Dummy.Agent.Calls.Startup));
            TestHelper.WaitForSetup("AgentLauncher");

            TestHelper.RunInContextOf("AgentLauncher", async () =>
            {
                await PerperContext.StartAgentAsync("Executor");
                await PerperContext.StartAgentAsync("Callback");
                await PerperContext.StartAgentAsync("DummyConsensus");
            });

            TestHelper.WaitForSetup("Executor", "Callback","DummyConsensus");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            TestHelper.DestroyAgent("AgentLauncher");
            TestHelper.DestroyAgent("Executor");
            TestHelper.DestroyAgent("Callback");
            TestHelper.DestroyAgent("DummyConsensus");
        }
    }
}