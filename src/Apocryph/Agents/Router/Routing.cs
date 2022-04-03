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
    private readonly IRoutingAdapter _routingAdapter;
    private readonly IHashResolver _hashResolver;
 
    public Routing(IRoutingAdapter routingAdapter, IHashResolver hashResolver)
    {
        _routingAdapter = routingAdapter;
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
        await _routingAdapter.StartCollectorStream();
        await _routingAdapter.SetInput(kothStates, executor);
    }

    /// <summary>
    /// Appends message into collector stream
    /// </summary>
    /// <param name="message">Agent message</param>
    public async Task PostMessage(AgentMessage message)
    {
        await _routingAdapter.CollectorStream.WriteItemAsync(message);
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
        var (currentCallsStreamName, currentRoutedOutput) = await _routingAdapter.GetPublicationStream(key);
        if (currentCallsStreamName != "" && currentRoutedOutput != null)
        {
            return (currentCallsStreamName, currentRoutedOutput);
        }

        var chain = await _hashResolver.RetrieveAsync(chainId);
        var callsStream = await  _routingAdapter.StartCallsStream(); // for IAsyncEnumerable<AgentMessage>
        var publicationsStream = await _routingAdapter.StartPublicationsStream(); // for IAsyncEnumerable<AgentMessage>
        var subscriptionsStream = await _routingAdapter.StartSubscriptionsStream(); // for IAsyncEnumerable<List<AgentReference>>

        var (kothStates, executor, _) = await _routingAdapter.GetInput();
        var routedInput = await _routingAdapter.RouterInput(callsStream, subscriptionsStream);
        var consensusOutput = await _routingAdapter.StartConsensusAgent(chain.ConsensusType, // name of consensus
            routedInput,
            subscriptionsStream.Stream,
            chain,
            kothStates,
            executor);

        var _ = _routingAdapter.RouterOutput(publicationsStream.Stream, consensusOutput, chainId);
        
        var resultValue = (callsStream.Stream, publicationsStream);
        await _routingAdapter.SetPublicationStream(key, callsStream.Stream, publicationsStream);
        return resultValue;
    }
}