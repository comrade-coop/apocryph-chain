using Perper.Extensions;
using Perper.Model;

namespace Apocryph.Consensus.Dummy.Agent.Calls;

public static class Init
{
    public static async Task<PerperStream> RunAsync( Chain chain, IAsyncEnumerable<Message> messages)
    {
        return await PerperContext.Stream("ExecutionStream").StartAsync(messages, chain);
    }
}