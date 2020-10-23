using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;
using Apocryph.Core.Consensus.Blocks;
using Apocryph.Core.Consensus.Blocks.Command;
using Apocryph.Core.Consensus.Blocks.Messages;

namespace Apocryph.Core.Consensus
{
    public class Validator
    {
        private Guid _chainId;
        private Block _lastBlock;
        private HashSet<Hash> _confirmedBlocks;
        private HashSet<ICommand> _pendingCommands;
        private HashSet<byte[]>? _pendingSetChainBlockMessages = new HashSet<byte[]>();
        private Executor _executor;
        private Func<Hash, Task<object>> _hashRegistryReader;
        private Func<object, Task> _hashRegistryWriter;

        public Validator(Executor executor, Func<Hash, Task<object>> hashRegistryReader, Func<object, Task> hashRegistryWriter, Guid chainId, Block lastBlock, HashSet<Hash> confirmedBlocks, HashSet<ICommand> pendingCommands)
        {
            _executor = executor;
            _hashRegistryReader = hashRegistryReader;
            _hashRegistryWriter = hashRegistryWriter;
            _chainId = chainId;
            _lastBlock = lastBlock;
            _confirmedBlocks = confirmedBlocks;
            _pendingCommands = pendingCommands;
        }

        public async Task<bool> Validate(Block block)
        {
            var _sawClaimRewardMessage = false;
            var inputCommands = (await MerkleTree.LoadAsync(block.InputCommands, _hashRegistryReader)).Values.Cast<ICommand>().ToArray();
            foreach (var inputCommand in inputCommands)
            {
                if (_chainId == Guid.Empty && inputCommand is Invoke invokation)
                {
                    if (invokation.Message.Item1 == typeof(ClaimRewardMessage).FullName)
                    {
                        if (_sawClaimRewardMessage)
                        {
                            return false;
                        }
                        _sawClaimRewardMessage = true;
                        continue;
                    }
                    else if (invokation.Message.Item1 == typeof(SetChainBlockMessage).FullName)
                    {
                        if (!_pendingSetChainBlockMessages!.Contains(invokation.Message.Item2))
                        {
                            return false;
                        }
                        continue;
                    }
                }

                if (!_pendingCommands!.Contains(inputCommand))
                {
                    return false;
                }
            }

            var (newState, newCommands, newCapabilities) = await _executor.Execute(
                _lastBlock!.States, inputCommands, _lastBlock!.Capabilities);

            var outputCommandsTree = await MerkleTree.CreateAsync(newCommands, _hashRegistryWriter);
            // Validate historical blocks as per protocol
            return block.Equals(new Block(Hash.From(_lastBlock), _chainId, block.Proposer, block.ProposerAccount, newState, block.InputCommands, outputCommandsTree.Root, newCapabilities));
        }


        public async Task AddConfirmedBlock(Block block)
        {
            var hash = Hash.From(block);
            if (!_confirmedBlocks.Add(hash)) return;

            var inputs = (await MerkleTree.LoadAsync(block.InputCommands, _hashRegistryReader)).Values.Cast<ICommand>().ToArray();
            var outputs = (await MerkleTree.LoadAsync(block.OutputCommands, _hashRegistryReader)).Values.Cast<ICommand>().ToArray();
            _pendingCommands!.UnionWith(inputs.Where(x => _executor.FilterCommand(x, _lastBlock!.Capabilities)));

            if (_chainId == Guid.Empty)
            {
                _pendingSetChainBlockMessages!.Add(JsonSerializer.SerializeToUtf8Bytes(new SetChainBlockMessage
                {
                    ChainId = block.ChainId,
                    BlockId = new byte[] { },
                    ProcessedCommands = new Dictionary<Guid, BigInteger>()
                    {
                        [block.ProposerAccount] = outputs.Length,
                    },
                    UsedTickets = new Dictionary<Guid, BigInteger>() { }, // TODO: Keep track of tickets
                    UnlockedTickets = new Dictionary<Guid, BigInteger>() { },
                }));
            }

            if (_chainId == block.ChainId)
            {
                _lastBlock = block;
                _pendingCommands.ExceptWith(inputs);
            }
        }
    }
}