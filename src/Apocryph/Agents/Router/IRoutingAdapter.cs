using Apocryph.Ipfs;
using Apocryph.Model;
using Perper.Extensions;
using Perper.Model;
using PerperState = Perper.Extensions.PerperState;

namespace Apocryph.Agents.Router;

public interface IRoutingAdapter
{
    PerperStream CollectorStream
    {
        get =>
            PerperState
                .GetOrDefaultAsync<PerperStream>("CollectorStream")
                .GetAwaiter()
                .GetResult();
        
        set => PerperState.SetAsync("CollectorStream", value)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    async Task StartCollectorStream()
    {
        await PerperContext.BlankStream().StartAsync().ConfigureAwait(false);
    }
    
    async Task<PerperStream> StartCallsStream()
    {
        return await PerperContext.BlankStream().StartAsync();
    }
    
    async Task<PerperStream> StartPublicationsStream()
    {
        return await PerperContext.BlankStream().StartAsync();
    }
    
    async Task<PerperStream> StartSubscriptionsStream()
    {
        return await PerperContext.BlankStream().StartAsync();
    }

    Task<List<AgentReference>> GetLastSubscriptions()
    {
        return PerperState.GetOrDefaultAsync("lastSubscriptions", new List<AgentReference>());
    }

    Task<(string, PerperStream)> GetChainInstance(Hash<Chain> chain)
    {
        return PerperContext.CallAsync<(string, PerperStream)>("GetChainInstance", chain);
    }

    Task<PerperStream> RouterInput(PerperStream callsStream, PerperStream subscriptionsStream)
    {
        return PerperContext.CallAsync<PerperStream>("RouterInput", callsStream, subscriptionsStream);
    }
    
    Task RouterOutput(string publicationsStreamName, PerperStream consensusOutput, Hash<Chain> chainId)
    {
        return PerperContext.CallAsync("RouterOutput", publicationsStreamName, consensusOutput, chainId)
            .ContinueWith(x => Console.WriteLine(x.Exception), TaskContinuationOptions.OnlyOnFaulted);
    }

    async Task<PerperStream> StartConsensusAgent(
        string consensusType,
        PerperStream routedInput,
        string subscriptionsStreamName,
        Chain chain,
        PerperStream kothStates,
        PerperAgent executor)
    {
        var (_, consensusOutput) = await PerperContext.StartAgentAsync<PerperStream>(
            chain.ConsensusType, // name of consensus
            routedInput,
            subscriptionsStreamName,
            chain,
            kothStates,
            executor
        );

        return consensusOutput;
    }

    Task SetInput(PerperStream kothStatesStream, PerperAgent executor)
    {
        return PerperState.SetAsync("input", (kothStatesStream, executor, CollectorStream));
    }  
    
    Task<(PerperStream, PerperAgent, PerperStream)> GetInput()
    {
        return PerperState.GetOrDefaultAsync<(PerperStream, PerperAgent, PerperStream)>("input");
    }

    Task SetPublicationStream(string key, string callsStreamName, PerperStream publicationsStream)
    {
        var resultValue = (callsStreamName, publicationsStream);
        return PerperState.SetAsync(key, resultValue);
    }
    
    Task<(string, PerperStream?)> GetPublicationStream(string key)
    {
        return PerperState.GetOrDefaultAsync<(string, PerperStream?)>(key);
    }
    
    IAsyncEnumerable<List<AgentReference>> EnumerateSubscriptions(PerperStream subscriptions)
    {
        return subscriptions.EnumerateAsync<List<AgentReference>>();
    }
    
    IAsyncEnumerable<AgentMessage> EnumerateTargetOutput(PerperStream subscriptions)
    {
        return subscriptions.EnumerateAsync<AgentMessage>();
    }
    
    IAsyncEnumerable<AgentMessage> EnumerateCalls(PerperStream calls)
    {
        return calls.EnumerateAsync<AgentMessage>();
    }
    
    IAsyncEnumerable<AgentMessage> EnumerateOutbox(PerperStream outbox)
    {
        return outbox.EnumerateAsync<AgentMessage>();
    }
    
    //  var (currentCallsStreamName, currentRoutedOutput) = await _context.CurrentState.GetOrDefaultAsync<(string, PerperStream?)>(key);
    
    //  var task = _context.CurrentAgent.CallAsync("RouterOutput", publicationsStream.Stream, consensusOutput, chainId)
    // .ContinueWith(x => Console.WriteLine(x.Exception), TaskContinuationOptions.OnlyOnFaulted); // DEBUG: FakeStream does not log errors
    
    //var resultValue = (callsStream.Stream, publicationsStream);
    //await _context.CurrentState.SetAsync(key, resultValue);

   
}