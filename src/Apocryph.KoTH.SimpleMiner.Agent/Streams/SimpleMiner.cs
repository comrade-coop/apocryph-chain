using System.Collections.Concurrent;
using Apocryph.Consensus;
using Apocryph.Ipfs;

namespace Apocryph.KoTH.SimpleMiner.Agent.Streams
{
    public class SimpleMiner
    {
        private readonly IHashResolver _hashResolver;
        private readonly IPeerConnector _peerConnector;

        public SimpleMiner( IHashResolver hashResolver, IPeerConnector peerConnector)
        {
            _hashResolver = hashResolver;
            _peerConnector = peerConnector;
        }

        public async Task RunAsync((IAsyncEnumerable<(Hash<Chain>, Slot?[])> kothStates, Hash<Chain>[] initialChains) input, CancellationToken cancellationToken)
        {
            var self = await _peerConnector.Self;
            var chains = new ConcurrentDictionary<Hash<Chain>, KoTHState>();

            var generator = Task.Run(async () =>
            {
                var random = new Random();
                while (!cancellationToken.IsCancellationRequested)
                {
                    var attemptData = new byte[16];
                    random.NextBytes(attemptData);
                    var newSlot = new Slot(self, attemptData);

                    foreach (var (chain, state) in chains)
                    {
                        if (state.TryInsert(newSlot))
                        {
                            await _peerConnector.SendPubSub(KoTHConstants.PubSubPath, (chain, newSlot), cancellationToken);
                        }
                    }

                    await Task.Delay(1, cancellationToken); // DEBUG: Try not to hog a full CPU core while testing
                }
            }, cancellationToken);

            foreach (var chain in input.initialChains)
            {
                var chainValue = await _hashResolver.RetrieveAsync(chain, cancellationToken);
                chains[chain] = new KoTHState(new Slot?[chainValue.SlotsCount]);
            }

            await foreach (var (chain, peers) in input.kothStates.WithCancellation(cancellationToken))
            {
                chains[chain] = new KoTHState(peers);
            }

            await generator;
        }
    }
}