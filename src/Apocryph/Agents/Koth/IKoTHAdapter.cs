using Apocryph.Ipfs;
using Apocryph.Model;
using Perper.Extensions;
using Perper.Model;
using PerperState = Perper.Extensions.PerperState;

namespace Apocryph.Agents.Koth;

// ReSharper disable once InconsistentNaming
public interface IKoTHAdapter
{
    // ReSharper disable once InconsistentNaming
    PerperStream KoTHProcessorStream
    {
        get =>
            PerperState
                .GetOrDefaultAsync<PerperStream>("KoTHProcessorStream")
                .GetAwaiter()
                .GetResult();
        
        set => PerperState.SetAsync("KoTHProcessorStream", value)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }
    
    async Task<PerperStream> StartLocalConsensusStream()
    {
        return await PerperContext.Stream("KoTHProcessor")
            .Persistent()
            .StartAsync()
            .ConfigureAwait(false);
    }

    Task<KoTHState?> GetChainState(Hash<Chain> chain)
    {
        return PerperState.GetOrDefaultAsync<KoTHState?>(chain.ToString());
    }
    
    Task SetChainState(Hash<Chain> chain, KoTHState chainState)
    {
        return PerperState.SetAsync(chain.ToString(), chainState);
    }
}