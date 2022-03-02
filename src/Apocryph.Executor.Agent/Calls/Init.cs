using Perper.Extensions;

namespace Apocryph.Executor.Agent.Calls;

public static class Init
{
    public static async Task RunAsync()
    {
        await PerperContext.StartAgentAsync("Executor");
        Console.WriteLine("Executor started");
    }
}