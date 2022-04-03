using System.Threading.Channels;
using Apocryph.Ipfs;
using Apocryph.Model;
using Perper.Model;
using Serilog;

namespace Apocryph.Agents.Koth;

// ReSharper disable once InconsistentNaming
/// <summary>
/// KoTH Agent
/// </summary>
// ReSharper disable once InconsistentNaming
public class KoTH
{
    public static string PubSubPath = "koth";
    
    private readonly IKoTHAdapter _koThAdapter;
    private readonly IHashResolver _hashResolver;
    private readonly IPeerConnector _peerConnector;

    
    public KoTH(IKoTHAdapter koThAdapter, IHashResolver hashResolver, IPeerConnector peerConnector)
    {
        _koThAdapter = koThAdapter;
        _hashResolver = hashResolver;
        _peerConnector = peerConnector;
    }
    
    /// <summary>
    /// Starts up the Agent
    /// </summary>
    public async Task<PerperStream> Startup()
    {
        var stream = _koThAdapter.KoTHProcessorStream = await _koThAdapter.StartLocalConsensusStream();
        return stream;
    }

    /// <summary>
    /// Stream listening for incoming Slots per Chain
    /// </summary>
    /// <param name="token">cancellation token</param>
    /// <returns>Task of KothStates enumerable</returns>
    // ReSharper disable once InconsistentNaming
    public async Task<IAsyncEnumerable<KothStates>> KoTHProcessor(CancellationToken token = default)
    {
        var output = Channel.CreateUnbounded<KothStates>();

        // NOTE: Should use Perper for locking instead
        var semaphore = new SemaphoreSlim(1, 1);

        await _peerConnector.ListenPubSub<(Hash<Chain> chain, Slot slot)>(PubSubPath, async (_, message) =>
        {
            await semaphore.WaitAsync(token);

            var chainState = await _koThAdapter.GetChainState(message.chain);
            if (chainState == null)
            {
                var chainValue = await _hashResolver.RetrieveAsync(message.chain, token);
                chainState = new KoTHState(new Slot?[chainValue.SlotsCount]);
            }

            if (chainState.TryInsert(message.slot))
            {
                var self = await _peerConnector.Self;
                Log.Debug("{ChainId} {SlotMap}", message.chain.ToString().Substring(0, 16), string.Join("", chainState.Slots.Select(x => x == null ? '_' : x.Peer == self ? 'X' : '.')));
                await _koThAdapter.SetChainState(message.chain, chainState);

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