using System.Collections.Concurrent;
using Apocryph.Ipfs;
using Apocryph.Model;
using Perper.Extensions;
using Perper.Model;

namespace Apocryph.Agents.Koth;

/// <summary>
/// KoTH Simple Miner
/// </summary>
// ReSharper disable once InconsistentNaming
public class KoTHSimpleMiner
{
    private readonly IHashResolver _hashResolver;
    private readonly IPeerConnector _peerConnector;

    public KoTHSimpleMiner(IHashResolver hashResolver, IPeerConnector peerConnector)
    {
        _hashResolver = hashResolver;
        _peerConnector = peerConnector;
    }

    /// <summary>
    /// Call starting slot mining
    /// </summary>
    /// <param name="kothStates">stream of KothStates</param>
    /// <param name="initialChains">initial chains array</param>
    /// <param name="token">cancellation token</param>
    public async Task Mine(PerperStream kothStates, Hash<Chain>[] initialChains, CancellationToken token = default)
    {
        var self = await _peerConnector.Self;
        var chains = new ConcurrentDictionary<Hash<Chain>, KoTHState>();

        var generator = Task.Run(async () =>
        {
            var random = new Random();
            while (!token.IsCancellationRequested)
            {
                var attemptData = new byte[16];
                random.NextBytes(attemptData);
                var newSlot = new Slot(self, attemptData);

                foreach (var (chain, state) in chains)
                {
                    if (state.TryInsert(newSlot))
                    {
                        await _peerConnector.SendPubSub(KoTH.PubSubPath, (chain, newSlot), token);
                    }
                }

                // DEBUG: Try not to hog a full CPU core while testing
                await Task.Delay(5, token);
            }
        }, token);

        foreach (var chain in initialChains)
        {
            var chainValue = await _hashResolver.RetrieveAsync(chain, token);
            chains[chain] = new KoTHState(new Slot?[chainValue.SlotsCount]);
        }

        await foreach (var (chain, peers) in kothStates.EnumerateAsync<KothStates>(cancellationToken: token))
        {
            chains[chain] = new KoTHState(peers);
        }

        await generator;
    }
}