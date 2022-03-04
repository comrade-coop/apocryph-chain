using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Apocryph.Consensus;
using NUnit.Framework;
using Perper.Application;
using Perper.Extensions;
using Perper.Protocol;

namespace Apocryph.Executor.Agent.Tests
{
    [SetUpFixture]
    public class AgentSetupFixture
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var stubAssembly =  typeof(AgentSetupFixture).Assembly;
            PerperBinaryConfigurations.TypeConfigurationsExtensions.Add(new BinaryTypeConfiguration(typeof(AgentMessage)));
            PerperBinaryConfigurations.TypeConfigurationsExtensions.Add(new BinaryTypeConfiguration(typeof(AgentReference)));

            new PerperStartup("ExecutorTestsStub")
                .DiscoverHandlersFromAssembly(stubAssembly, typeof(Apocryph.Executor.Agent.Tests.Agents.StubAgent.Calls.Startup).Namespace!)
                .RunAsync();

#pragma warning disable CS4014
            var executorAssembly = typeof(Apocryph.Executor.Agent.Calls.Startup).Assembly;
            new PerperStartup("Executor")
                .DiscoverHandlersFromAssembly(executorAssembly, typeof(Apocryph.Executor.Agent.Calls.Startup).Namespace!)
                .RunAsync();
#pragma warning restore CS4014

            while(Apocryph.Executor.Agent.Calls.Startup.ExecutionContext == null)
                Task.Delay(1000).GetAwaiter().GetResult();

            while(Agents.StubAgent.Calls.Startup.ExecutionContext == null)
                Task.Delay(1000).GetAwaiter().GetResult();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ExecutionContext.Run(Apocryph.Executor.Agent.Calls.Startup.ExecutionContext, _ =>
            {
                PerperContext.Agent.DestroyAsync()
                    .GetAwaiter()
                    .GetResult();

            }, null);
        }
    }
}