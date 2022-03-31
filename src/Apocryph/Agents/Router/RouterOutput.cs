using Apocryph.Ipfs;
using Apocryph.Model;
using Perper.Extensions;
using Perper.Model;

namespace Apocryph.Agents.Router;

public partial class Routing
{
    public async Task RouterOutput(string _, PerperStream outbox, Hash<Chain> self)
    {
        var (_, _, collectorStream) = await PerperState.GetOrDefaultAsync<(PerperStream, PerperAgent, PerperStream)>("input");
        
        await foreach (var message in outbox.EnumerateAsync<AgentMessage>())
        {
            if (message.Target.Chain == self && message.Target.AgentNonce < 0)
            {
                await PerperContext.WriteToBlankStreamAsync(collectorStream, message);
            }
            else
            {
                await PerperContext.CallAsync("PostMessage", message);
            }
        }
    }

    public async Task PostMessage(AgentMessage message)
    {
        var (_, _, collectorStream) = await PerperState.GetOrDefaultAsync<(PerperStream, PerperAgent, PerperStream)>("input");

        await PerperContext.WriteToBlankStreamAsync(collectorStream, message, true);
    }
}