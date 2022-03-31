using System;
using System.Threading.Tasks;
using Apocryph.Shared;
using Perper.Application;
using Perper.Extensions;

namespace Apocryph.Testing;

public static class TestHelper
{
    public static void SetupAgent(string name, Type startup)
    {
        var launcherAssembly = startup.Assembly;
        //new PerperStartup(name)
            //.DiscoverHandlersFromAssembly(launcherAssembly, startup.Namespace!)
          //  .RunAsync();
    }

    public static void RunInContextOf(string name, Func<Task> action)
    {
        ExecutionContexts.RunInExecutionContext(name, action);
    }

    public static void DestroyAgent(string name)
    {
        ExecutionContexts.RunInExecutionContext(name, async () =>
        {
            await PerperContext.Agent.DestroyAsync();
        });
    }

    public static void WaitForSetup(params string[] names)
    {
        while(!ExecutionContexts.AllReady(names))
            Task.Delay(1000).GetAwaiter().GetResult();
    }
}