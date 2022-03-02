// ReSharper disable MemberCanBePrivate.Global

namespace Apocryph.Executor.Agent.Calls;

public static class Startup
{
    public static ExecutionContext ExecutionContext = null!;

    public static async Task RunAsync()
    {
        Console.WriteLine("Startup");

        ExecutionContext = ExecutionContext.Capture()!;
    }
}