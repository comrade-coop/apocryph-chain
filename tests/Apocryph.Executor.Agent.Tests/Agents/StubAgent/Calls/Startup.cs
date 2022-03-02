using System;
using System.Threading;
using System.Threading.Tasks;

namespace Apocryph.Executor.Agent.Tests.Agents.StubAgent.Calls
{
    public static class Startup
    {
        public static ExecutionContext ExecutionContext = null!;

        public static async Task RunAsync()
        {
            Console.WriteLine("ExecutorTests Startup");

            ExecutionContext = ExecutionContext.Capture()!;
        }
    }
}