using Apocryph.Ipfs;
using Apocryph.Model;
using Apocryph.Shared;
using Microsoft.Extensions.Logging;
using Perper.Extensions;
using Perper.Model;

namespace Apocryph.Agents.Router;

/// <summary>
/// Routing Agent
/// </summary>
public partial class Routing : DependencyAgent
{
    //private readonly ILogger _logger;
    //private readonly IHashResolver _hashResolver;

    /*public Routing(ILogger logger, IHashResolver hashResolver)
    {
        _logger = logger;
        _hashResolver = hashResolver;
    }*/

    /// <summary>
    /// Starts up the Agent
    /// </summary>
    /// <param name="kothStates">stream of KothStates</param>
    /// <param name="executor"></param>
    /// <param name="collectorStream">stream of AgentMessages</param>
    public async Task Startup(PerperStream kothStates, PerperAgent executor, PerperStream collectorStream)
    {
        await PerperState.SetAsync("input", (kothStates, executor, collectorStream));
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
        var (currentCallsStreamName, currentRoutedOutput) = await PerperState.GetOrDefaultAsync<(string, PerperStream?)>(key);
        if (currentCallsStreamName != "" && currentRoutedOutput != null)
        {
            return (currentCallsStreamName, currentRoutedOutput);
        }

        var chain = await _hashResolver.RetrieveAsync(chainId);

        var callsStream = await PerperContext.BlankStream().StartAsync(); // for IAsyncEnumerable<AgentMessage>
        var publicationsStream = await PerperContext.BlankStream().StartAsync(); // for IAsyncEnumerable<AgentMessage>
        var subscriptionsStream = await PerperContext.BlankStream().StartAsync(); // for IAsyncEnumerable<List<AgentReference>>

        var (kothStates, executor, _) = await PerperState.GetOrDefaultAsync<(PerperStream, PerperAgent, PerperStream)>("input");

        var routedInput = await PerperContext.CallAsync<PerperStream>("RouterInput", callsStream, subscriptionsStream, subscriptionsStream);

        var (_, consensusOutput) = await PerperContext.StartAgentAsync<PerperStream>(
            chain.ConsensusType, // name of consensus
            routedInput,
            subscriptionsStream.Stream,
            chain,
            kothStates,
            executor
        );

        var task = PerperContext.CallAsync("RouterOutput", publicationsStream.Stream, consensusOutput, chainId)
            .ContinueWith(x => Console.WriteLine(x.Exception), TaskContinuationOptions.OnlyOnFaulted); // DEBUG: FakeStream does not log errors

        var resultValue = (callsStream.Stream, publicationsStream);
        await PerperState.SetAsync(key, resultValue);
        return resultValue;
    }
}