using Apocryph.Ipfs;
using Apocryph.Model;
using Perper.Extensions;
using Perper.Model;

namespace Apocryph.Agents.Router;

public partial class Routing
{
    public async Task RouterOutput(string _, PerperStream outbox, Hash<Chain> self)
    {
        await foreach (var message in _routingAdapter.EnumerateOutbox(outbox))
        {
            if (message.Target.Chain == self && message.Target.AgentNonce < 0)
            {
                await _routingAdapter.CollectorStream.WriteItemAsync(message);
            }
            else
            {
                await _routingAdapter.GetChainInstance(message.Target.Chain);
                await _routingAdapter.CollectorStream.WriteItemAsync(message);
            }
        }
    }
}