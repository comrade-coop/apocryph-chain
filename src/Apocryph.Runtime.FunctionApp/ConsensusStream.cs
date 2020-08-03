using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Apocryph.Core.Consensus;
using Apocryph.Core.Consensus.Blocks;
using Apocryph.Core.Consensus.Communication;
using Apocryph.Core.Consensus.VirtualNodes;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace Apocryph.Runtime.FunctionApp
{
    public class ConsensusStream
    {

        private static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            Converters =
            {
                { new NonStringKeyDictionaryConverter() }
            }
        };

        private readonly Channel<Query<Block>> _channel;

        private Node? _node;
        private Node?[]? _nodes;
        private Node?[]? _proposers;
        private Snowball<Block>? _snowball;
        private Proposer? _proposer;
        private IAsyncCollector<object>? _output;

        public ConsensusStream()
        {
            _channel = Channel.CreateUnbounded<Query<Block>>();
        }

        [FunctionName(nameof(ConsensusStream))]
        public async Task Run([PerperStreamTrigger] PerperStreamContext context,
            [Perper("proposerAccount")] Guid proposerAccount,
            [Perper("node")] Node node,
            [Perper("nodes")] List<Node> nodes,
            [Perper("chainData")] Chain chainData,
            [Perper("filter")] IAsyncEnumerable<Block> filter,
            [Perper("validator")] IAsyncEnumerable<Message<Block>> validator,
            [Perper("chain")] IAsyncEnumerable<Message<(Guid, Node?[])>> chain,
            [Perper("queries")] IAsyncEnumerable<Query<Block>> queries,
            [Perper("output")] IAsyncCollector<object> output,
            CancellationToken cancellationToken)
        {
            _output = output;
            _node = node;
            _nodes = nodes.ToArray();

            var executor = new Executor(_node!.ChainId,
                async (worker, input) => await context.CallWorkerAsync<(byte[]?, (string, object[])[], Dictionary<Guid, string[]>, Dictionary<Guid, string>)>(worker, new { input }, default));
            _proposer = new Proposer(executor, _node!.ChainId, chainData.GenesisBlock, new HashSet<Block>(), new HashSet<object>(), _node, proposerAccount);

            await TaskHelper.WhenAllOrFail(
                RunSnowball(context, cancellationToken),
                HandleChain(chain, cancellationToken),
                HandleValidator(validator, cancellationToken),
                HandleFilter(filter, cancellationToken),
                HandleQueries(queries, cancellationToken));
        }

        private Task<Block> Propose(PerperStreamContext context)
        {
            return _proposer!.Propose();
        }

        private async Task HandleChain(IAsyncEnumerable<Message<(Guid, Node?[])>> chain, CancellationToken cancellationToken)
        {
            await foreach (var message in chain.WithCancellation(cancellationToken))
            {
                var (chainId, nodes) = message.Value;

                if (_node!.ChainId == chainId)
                {
                    _nodes = nodes;
                }
            }
        }

        private async Task HandleValidator(IAsyncEnumerable<Message<Block>> validator, CancellationToken cancellationToken)
        {
            await foreach (var message in validator.WithCancellation(cancellationToken))
            {
                if (message.Type == MessageType.Valid)
                {
                    _proposer!.AddConfirmedBlock(message.Value);
                }
            }
        }

        private async Task HandleFilter(IAsyncEnumerable<Block> filter, CancellationToken cancellationToken)
        {
            await foreach (var block in filter.WithCancellation(cancellationToken))
            {
                _proposer!.AddConfirmedBlock(block);
            }
        }

        private async Task HandleQueries(IAsyncEnumerable<Query<Block>> queries, CancellationToken cancellationToken)
        {
            await foreach (var query in queries.WithCancellation(cancellationToken))
            {
                if (_snowball is null || !query.Receiver.Equals(_node)) continue;

                if (query.Verb == QueryVerb.Response)
                {
//                     Console.WriteLine("{0} received result from {1}", _node, query.Sender);
                    await _channel.Writer.WriteAsync(query, cancellationToken);
//                     Console.WriteLine("{0} processed result from {1}", _node, query.Sender);
                }
                else if (query.Verb == QueryVerb.Request)
                {
//                     Console.WriteLine("{0} received query from {1}", _node, query.Sender);
                    var q = _snowball!.Query(query);
                    await _output!.AddAsync(q, cancellationToken);
//                     Console.WriteLine("{0} sent result to {1}", _node, q.Receiver);
                }
            }
        }

        private async Task RunSnowball(PerperStreamContext context, CancellationToken cancellationToken)
        {
            await Task.Delay(2000);
            while (true)
            {
                await Task.Delay(1000);
                var proposerCount = _nodes!.Length / 10 + 1; // TODO: Move constant to parameter
                var serializedBlock = JsonSerializer.SerializeToUtf8Bytes(_proposer!.GetLastBlock(), jsonSerializerOptions);
                _proposers = RandomWalk.Run(serializedBlock).Select(selected => _nodes[(int)(selected.Item1 % _nodes.Length)]).Take(proposerCount).ToArray();

                var opinion = default(Block?);
                if (Array.IndexOf(_proposers, _node) != -1)
                {
                    opinion = await Propose(context);
                }

                var k = _nodes!.Length / 3 + 1; // TODO: Move constants to parameters
                var beta = _nodes!.Length * 2; // TODO: Move constants to parameters

//                 Console.WriteLine("{0} starts new snowball!", _node);
                _snowball = new Snowball<Block>(_node!, k, 0.6, beta,
                                                SnowballSend, SnowballRespond, opinion);

                var committedProposal = await _snowball!.Run(_nodes, cancellationToken);
//                 Console.WriteLine("{0} finished snowball!", _node);

                await _output!.AddAsync(new Message<Block>(committedProposal, MessageType.Proposed), cancellationToken);
            }
        }

        private async IAsyncEnumerable<Query<Block>> SnowballSend(Query<Block>[] queries, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Delay(100);
//             Console.WriteLine("{0} sent queries to {1}", _node, string.Join(":", queries.Select(q => q.Receiver)));
            await Task.WhenAll(queries.Select(q => _output!.AddAsync(q, cancellationToken)));
            await foreach (var answer in _channel.Reader.ReadAllAsync(cancellationToken).Take(queries.Length).WithCancellation(cancellationToken))
            {
                yield return answer;
            }
        }

        private Query<Block> SnowballRespond(Query<Block> query, Block? value, Block? opinion)
        {
            var result = opinion ??
                         (value is null || IsNewBlockBetter(value, query.Value) ? query.Value : value);
            return new Query<Block>(result, query.Receiver, query.Sender, QueryVerb.Response);
        }

        private bool IsNewBlockBetter(Block current, Block suggested)
        {
            var currentProposerOrder = Array.IndexOf(_proposers!, current.Proposer);
            var suggestedProposerOrder = Array.IndexOf(_proposers!, suggested.Proposer);

            return suggestedProposerOrder != -1 && suggestedProposerOrder < currentProposerOrder;
        }
    }
}