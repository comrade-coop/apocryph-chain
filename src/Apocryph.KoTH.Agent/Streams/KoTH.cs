using System.Threading.Channels;
using Apocryph.Consensus;
using Apocryph.Ipfs;
using Microsoft.Extensions.Logging;
using Perper.Extensions;

namespace Apocryph.KoTH.Agent.Streams
{
    public class KoTHProcessor
    {
        private readonly IHashResolver _hashResolver;
        private readonly IPeerConnector _peerConnector;
        private readonly ILogger _logger;

        public KoTHProcessor(IHashResolver hashResolver, IPeerConnector peerConnector, ILogger logger)
        {
            _hashResolver = hashResolver;
            _peerConnector = peerConnector;
            _logger = logger;
        }

        public async Task<IAsyncEnumerable<(Hash<Chain>, Slot?[])>> RunAsync(object? input, CancellationToken cancellationToken)
        {
            var output = Channel.CreateUnbounded<(Hash<Chain>, Slot?[])>();
            var semaphore = new SemaphoreSlim(1, 1); // NOTE: Should use Perper for locking instead
            await _peerConnector.ListenPubSub<(Hash<Chain> chain, Slot slot)>(KoTHConstants.PubSubPath, async (_, message) =>
            {
                await semaphore.WaitAsync(cancellationToken);
                var chainState = await PerperState.GetOrDefaultAsync<KoTHState>(message.chain.ToString());

                if (chainState.TryInsert(message.slot))
                {
                    var self = await _peerConnector.Self;
                    _logger.LogDebug("{ChainId} {SlotMap}", message.chain.ToString().Substring(0, 16), string.Join("", chainState.Slots.Select(x => x == null ? '_' : x.Peer == self ? 'X' : '.')));
                    await PerperState.SetAsync(message.chain.ToString(), chainState);
                    await output.Writer.WriteAsync((message.chain, chainState.Slots.ToArray()), cancellationToken); // DEBUG: ToArray used due to in-place modifications
                }

                semaphore.Release();
                return true;

            }, cancellationToken);

            cancellationToken.Register(() => output.Writer.Complete()); // DEBUG: Used for testing purposes mainly

            return output.Reader.ReadAllAsync(cancellationToken);
        }
    }
}