using System.Threading.Tasks;
using Apocryph.Shared;
using Perper.Extensions;

// ReSharper disable once CheckNamespace
namespace Apocryph.Consensus.Dummy.Agent.Tests.AgentLauncher.Calls;

public static class Init
{
    public static async Task RunAsync()
    {
        await PerperContext.StartAgentAsync(PerperContext.Agent.Agent);

        var agentMessageProducer = await PerperContext.Stream("AgentMessageProducer")
            .Packed()
            .Persistent()
            .StartAsync()
            .ConfigureAwait(false);

        var processor = await PerperContext.Stream("AgentMessageProcessor")
            .StartAsync(agentMessageProducer.Replay())
            .ConfigureAwait(false);

        var _ = await PerperContext.Stream("AgentMessageConsumer")
            .Action()
            .StartAsync(processor)
            .ConfigureAwait(false);

    }
}

public static class Startup
{
    public static void Run() => ExecutionContexts.Add(PerperContext.Agent);
}

