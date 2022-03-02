using System;
using System.Threading.Tasks;
using Perper.Extensions;

namespace Apocryph.Executor.Agent.Tests.Agents.StubAgent.Calls
{
    public static class Init
    {
        public static async Task RunAsync()
        {
            await PerperContext.StartAgentAsync("ExecutorTestsStub");

        }
    }
}