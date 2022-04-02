using Apocryph.Ipfs;
using Apocryph.Model;
using Perper.Extensions;
using Perper.Model;

namespace Apocryph.Agents.Router;

/// <summary>
/// Routing Agent
/// </summary>
public partial class Routing
{
    private readonly IPerperContext _context;
    private readonly IHashResolver _hashResolver;

    private PerperStream CollectorStream
    {
        get =>
            _context.CurrentState
                .GetOrDefaultAsync<PerperStream>("CollectorStream")
                .GetAwaiter()
                .GetResult();
        
        set => _context.CurrentState.SetAsync("CollectorStream", value)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }
    
    public Routing(IPerperContext context, IHashResolver hashResolver)
    {
        _context = context;
        _hashResolver = hashResolver;
    }

    /// <summary>
    /// Starts up the Agent
    /// </summary>
    /// <param name="kothStates">stream of KothStates</param>
    /// <param name="executor"></param>
    /// <param name="collectorStream">stream of AgentMessages</param>
    public async Task Startup(PerperStream kothStates, PerperAgent executor, PerperStream collectorStream)
    {
        var stream = CollectorStream =
            await PerperContext.BlankStream().StartAsync()
                .ConfigureAwait(false);
            
        await _context.CurrentState.SetAsync("input", (kothStates, executor, collectorStream));
    }

    /// <summary>
    /// Appends message into collector stream
    /// </summary>
    /// <param name="message">Agent message</param>
    public async Task PostMessage(AgentMessage message)
    {
        await CollectorStream.WriteItemAsync(message);
    }
    
    /// <summary>
    ///
    /// </summary>
    /// <param name="chainId"></param>
    /// <returns></returns>
    public async Task<(string, PerperStream)> GetChainInstance(Hash<Chain> chainId)
    {
        // NOTE: Can benefit from locking
        var key = $"{chainId}";
        var (currentCallsStreamName, currentRoutedOutput) = await _context.CurrentState.GetOrDefaultAsync<(string, PerperStream?)>(key);
        if (currentCallsStreamName != "" && currentRoutedOutput != null)
        {
            return (currentCallsStreamName, currentRoutedOutput);
        }

        var chain = await _hashResolver.RetrieveAsync(chainId);

        var callsStream = await PerperContext.BlankStream().StartAsync(); // for IAsyncEnumerable<AgentMessage>
        var publicationsStream = await PerperContext.BlankStream().StartAsync(); // for IAsyncEnumerable<AgentMessage>
        var subscriptionsStream = await PerperContext.BlankStream().StartAsync(); // for IAsyncEnumerable<List<AgentReference>>

        var (kothStates, executor, _) = await _context.CurrentState.GetOrDefaultAsync<(PerperStream, PerperAgent, PerperStream)>("input");

        var routedInput = await _context.CurrentAgent.CallAsync<PerperStream>("RouterInput", callsStream, subscriptionsStream, subscriptionsStream);

        var (_, consensusOutput) = await PerperContext.StartAgentAsync<PerperStream>(
            chain.ConsensusType, // name of consensus
            routedInput,
            subscriptionsStream.Stream,
            chain,
            kothStates,
            executor
        );

        var task = _context.CurrentAgent.CallAsync("RouterOutput", publicationsStream.Stream, consensusOutput, chainId)
            .ContinueWith(x => Console.WriteLine(x.Exception), TaskContinuationOptions.OnlyOnFaulted); // DEBUG: FakeStream does not log errors

        var resultValue = (callsStream.Stream, publicationsStream);
        await _context.CurrentState.SetAsync(key, resultValue);
        return resultValue;
    }
}