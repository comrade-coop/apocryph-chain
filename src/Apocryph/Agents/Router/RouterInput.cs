using System.Threading.Tasks.Dataflow;
using Apocryph.Model;
using Apocryph.Shared;
using Perper.Extensions;
using Perper.Model;

namespace Apocryph.Agents.Router;

public partial class Routing
{
    /// <summary>
    /// RouterInput stream
    /// </summary>
    /// <param name="calls">stream of AgentMessage items</param>
    /// <param name="subscriptions">stream of AgentReference items</param>
    /// <returns>enumerates over AgentMessage items</returns>
    public async Task<IAsyncEnumerable<AgentMessage>> RouterInput(PerperStream calls, PerperStream subscriptions)
    {
        var output = EmptyBlock<AgentMessage>();
        var lastSubscriptions = await PerperState.GetOrDefaultAsync("lastSubscriptions", new List<AgentReference>());
        var subscriptionsFromLastBlock = KeepLastBlock(lastSubscriptions);
        subscriptions.EnumerateAsync<List<AgentReference>>().ToDataflow().LinkTo(subscriptionsFromLastBlock);

        var subscriber = SubscriberBlock<AgentReference, AgentMessage>(async reference =>
        {
            var (_, targetOutput) = await PerperContext.CallAsync<(string, PerperStream)>("GetChainInstance", reference.Chain);
            return targetOutput.Replay().EnumerateAsync<AgentMessage>().ToDataflow(); // TODO: Make sure to replay only messages newer than the subscription
        });

        subscriptionsFromLastBlock.LinkTo(subscriber);
        subscriber.LinkTo(output);

        calls.EnumerateAsync<AgentMessage>().ToDataflow().LinkTo(output);

        return output.ToAsyncEnumerable();
    }

    private static IPropagatorBlock<T, T> EmptyBlock<T>() => new BufferBlock<T>(new DataflowBlockOptions { BoundedCapacity = 1 });

    private static TransformBlock<T, T> KeepLastBlock<T>(T stateEntry) where T : class
    {
        var output = new TransformBlock<T, T>(value => value);
        output.Post(stateEntry);
        return output;
    }

    private static IPropagatorBlock<IEnumerable<TKey>, TValue> SubscriberBlock<TKey, TValue>(Func<TKey, Task<ISourceBlock<TValue>>> resolver)
        where TKey : notnull
    {
        var links = new Dictionary<TKey, IDisposable>();
        var output = EmptyBlock<TValue>();
        var subscriber = new ActionBlock<IEnumerable<TKey>>(async (subscriptions) =>
        {
            var seenSubscriptions = new HashSet<TKey>();
            foreach (var subscription in subscriptions)
            {
                if (!links.ContainsKey(subscription))
                {
                    var source = await resolver(subscription);
                    links[subscription] = source.LinkTo(output);
                }
                seenSubscriptions.Add(subscription);
            }

            foreach (var (subscription, link) in links)
            {
                if (!seenSubscriptions.Contains(subscription))
                {
                    link.Dispose();
                }
            }
        });

        return DataflowBlock.Encapsulate(subscriber, output);
    }
}