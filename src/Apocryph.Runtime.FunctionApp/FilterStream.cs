using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apocryph.Core.Consensus;
using Apocryph.Core.Consensus.Blocks;
using Apocryph.Core.Consensus.Blocks.Command;
using Apocryph.Core.Consensus.Communication;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace Apocryph.Runtime.FunctionApp
{
    public class FilterStream
    {
        private Dictionary<Guid, Validator> _validators = new Dictionary<Guid, Validator>();
        private Dictionary<Hash, Task<bool>> _validatedBlocks = new Dictionary<Hash, Task<bool>>();
        private Func<Hash, Task<Block>>? _hashRegistryReader;
        private IAsyncCollector<object>? _output;

        [FunctionName(nameof(FilterStream))]
        public async Task Run([PerperStreamTrigger] PerperStreamContext context,
            [Perper("chains")] Dictionary<Guid, Chain> chains,
            [Perper("ibc")] IAsyncEnumerable<Message<Hash>> ibc,
            [Perper("gossips")] IAsyncEnumerable<Gossip<Hash>> gossips,
            [Perper("hashRegistryWriter")] string hashRegistryWriter,
            [Perper("hashRegistryReader")] string hashRegistryReader,
            [Perper("output")] IAsyncCollector<object> output,
            CancellationToken cancellationToken)
        {
            _hashRegistryReader = hash => context.CallWorkerAsync<Block>(hashRegistryReader, new { hash, type = typeof(Block).AssemblyQualifiedName }, cancellationToken);
            Func<object, Task> _hashRegistryWriter = value => context.CallWorkerAsync<bool>(hashRegistryWriter, new { value }, cancellationToken);
            _output = output;

            foreach (var (chainId, chain) in chains)
            {
                var executor = new Executor(chainId,
                    async (worker, input) => await context.CallWorkerAsync<(byte[]?, (string, object[])[], Dictionary<Guid, string[]>, Dictionary<Guid, string>)>(worker, new { input }, cancellationToken));
                _validators[chainId] = new Validator(
                    executor,
                    hash => context.CallWorkerAsync<object>(hashRegistryReader, new { hash, type = typeof(ICommand).AssemblyQualifiedName, allowMerkleNode = true }, cancellationToken),
                    value => context.CallWorkerAsync<bool>(hashRegistryWriter, new { value }, cancellationToken),
                    chainId, chain.GenesisBlock, new HashSet<Hash>(), new HashSet<ICommand>());
            }

            await TaskHelper.WhenAllOrFail(
                Task.Run(async () =>
                {

                    // Second loop, as we want to distribute all genesis blocks to all chains
                    foreach (var (chainId, chain) in chains)
                    {
                        await Task.Delay(4000);
                        foreach (var (_chainId, validator) in _validators)
                        {
                            await validator.AddConfirmedBlock(chain.GenesisBlock);
                        }

                        await _hashRegistryWriter(chain.GenesisBlock);
                        await _output!.AddAsync(Hash.From(chain.GenesisBlock), cancellationToken);
                    }
                }),
                HandleIBC(context, ibc, cancellationToken),
                HandleGossips(context, gossips, cancellationToken));
        }

        private Task<bool> Validate(PerperStreamContext context, Block block)
        {
            return _validators[block.ChainId].Validate(block);
        }

        private async Task HandleIBC(PerperStreamContext context, IAsyncEnumerable<Message<Hash>> ibc, CancellationToken cancellationToken)
        {
            await foreach (var message in ibc.WithCancellation(cancellationToken))
            {
                if (message.Type != MessageType.Accepted) continue;

                var hash = message.Value;
                if (!_validatedBlocks.ContainsKey(hash))
                {
                    var block = await _hashRegistryReader!(hash);
                    _validatedBlocks[hash] = Validate(context, block!);
                }

                var valid = await _validatedBlocks[hash];

                if (valid)
                {
                    var block = await _hashRegistryReader!(hash);
                    foreach (var (chainId, validator) in _validators)
                    {
                        await validator.AddConfirmedBlock(block!);
                    }

                    await _output!.AddAsync(hash, cancellationToken);
                }
            }
        }

        private async Task HandleGossips(PerperStreamContext context, IAsyncEnumerable<Gossip<Hash>> gossips, CancellationToken cancellationToken)
        {
            // Validate blocks from gossips before they are fully confirmed, saving a tiny bit of time
            await foreach (var gossip in gossips.WithCancellation(cancellationToken))
            {
                var hash = gossip.Value;
                if (!_validatedBlocks.ContainsKey(hash))
                {
                    var block = await _hashRegistryReader!(hash);
                    _validatedBlocks[hash] = Validate(context, block!);
                }
            }
        }
    }
}