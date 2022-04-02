using Apocryph.Ipfs;
using Apocryph.Model;
using Perper.Extensions;
using Perper.Model;

namespace Apocryph.Agents.Router;

public partial class Routing
{
    public async Task RouterOutput(string _, PerperStream outbox, Hash<Chain> self)
    {
        await foreach (var message in outbox.EnumerateAsync<AgentMessage>())
        {
            if (message.Target.Chain == self && message.Target.AgentNonce < 0)
            {
                await CollectorStream.WriteItemAsync(message);
            }
            else
            {
                await _context.CurrentAgent.CallAsync<(string, PerperStream)>("GetChainInstance", message.Target.Chain);
                await CollectorStream.WriteItemAsync(message);
            }
        }
    }
}