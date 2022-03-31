using System.Threading.Channels;
using Apocryph.Ipfs;
using Apocryph.Model;
using Perper.Extensions;
using Perper.Model;
using Serilog;

namespace Apocryph.Agents.Koth;

/// <summary>
/// KoTH Agent
/// </summary>
public class KoTH : DependencyAgent
{
    //private readonly ILogger _logger;
    //private readonly IHashResolver _hashResolver;
    //private readonly IPeerConnector _peerConnector;
    
    public static string PubSubPath = "koth";

    /*public KoTH(ILogger logger, IHashResolver hashResolver, IPeerConnector peerConnector)
    {
        _logger = logger;
        _hashResolver = hashResolver;
        _peerConnector = peerConnector;
    }*/

    /// <summary>
    /// Starts up the Agent
    /// </summary>
    public async Task<PerperStream> Startup()
    {
        var stream = await PerperContext.Stream("KoTHProcessor").Persistent().StartAsync().ConfigureAwait(false);
        await PerperState.SetAsync("KoTHProcessor", stream.Replay()).ConfigureAwait(false);
        return stream;
    }

    /// <summary>
    /// Stream listening for incoming Slots per Chain
    /// </summary>
    /// <param name="token">cancellation token</param>
    /// <returns>Task of KothStates enumerable</returns>
    public async Task<IAsyncEnumerable<KothStates>> KoTHProcessor(CancellationToken token = default)
    {
        var output = Channel.CreateUnbounded<KothStates>();

        // NOTE: Should use Perper for locking instead
        var semaphore = new SemaphoreSlim(1, 1);

        await _peerConnector.ListenPubSub<(Hash<Chain> chain, Slot slot)>(PubSubPath, async (_, message) =>
        {
            await semaphore.WaitAsync(token);
            var chainState = await PerperState.GetOrDefaultAsync<KoTHState?>(message.chain.ToString());
            if (chainState == null)
            {
                var chainValue = await _hashResolver.RetrieveAsync(message.chain, token);
                chainState = new KoTHState(new Slot?[chainValue.SlotsCount]);
            }

            if (chainState.TryInsert(message.slot))
            {
                var self = await _peerConnector.Self;
                Log.Debug("{ChainId} {SlotMap}", message.chain.ToString().Substring(0, 16), string.Join("", chainState.Slots.Select(x => x == null ? '_' : x.Peer == self ? 'X' : '.')));
                await PerperState.SetAsync(message.chain.ToString(), chainState);

                // DEBUG: ToArray used due to in-place modifications
                await output.Writer.WriteAsync(new KothStates(message.chain, chainState.Slots.ToArray()), token);
            }

            semaphore.Release();
            return true;

        }, token);

        // DEBUG: Used for testing purposes mainly
        token.Register(() => output.Writer.Complete());

        return output.Reader.ReadAllAsync(token);
    }
}