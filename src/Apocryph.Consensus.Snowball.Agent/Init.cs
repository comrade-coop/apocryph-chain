using Apocryph.Ipfs;
using Apocryph.KoTH;
using Perper.Extensions;
using Perper.Model;

namespace Apocryph.Consensus.Snowball.Agent;

public class Init
{
    private readonly IHashResolver _hashResolver;

    public Init(IHashResolver hashResolver)
    {
        _hashResolver = hashResolver;
    }

    public async Task<PerperStream> RunAsync(Chain chain, IAsyncEnumerable<(Hash<Chain>, Slot?[])> kothStates, IAsyncEnumerable<Message> messages)
    {
        var self = await _hashResolver.StoreAsync(chain);

        await PerperContext.Stream("MessagePool").StartAsync(messages, self);
        await PerperContext.Stream("KothProcessor").StartAsync(self, kothStates);

        return await PerperContext.Stream("SnowballStream").StartAsync(self);
    }

}