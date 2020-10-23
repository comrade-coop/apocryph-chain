using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Apocryph.Core.Consensus.Blocks;
using Apocryph.Core.Consensus.Blocks.Command;
using Apocryph.Core.Consensus.VirtualNodes;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;
using TestHarness.FunctionApp.Mock;


namespace TestHarness.FunctionApp
{
    public static class Launcher
    {
        [FunctionName("Launcher")]
        public static async Task RunAsync([PerperModuleTrigger(RunOnStartup = true)]
            PerperModuleContext context,
            CancellationToken cancellationToken)
        {
            var mode = Environment.GetEnvironmentVariable("ApocryphEnvironment");
            var self = new Peer(new byte[0]);
            var slotGossipsStream = "DummyStream";
            var hashRegistryWriter = typeof(HashRegistryWriter).FullName! + ".Run";
            var hashRegistryReader = typeof(HashRegistryReader).FullName! + ".Run";
            var outsideGossipsStream = "DummyStream";
            var outsideQueriesStream = "DummyStream";

            if (mode == "ipfs")
            {
                self = await context.CallWorkerAsync<Peer>("Apocryph.Runtime.FunctionApp.SelfPeerWorker.Run", new { }, default);
                slotGossipsStream = "Apocryph.Runtime.FunctionApp.IpfsSlotGossipStream.Run";
                hashRegistryWriter = "Apocryph.Runtime.FunctionApp.HashRegistryWriter.Run";
                hashRegistryReader = "Apocryph.Runtime.FunctionApp.HashRegistryReader.Run";
                outsideGossipsStream = "Apocryph.Runtime.FunctionApp.IpfsGossipStream.Run";
                outsideQueriesStream = "Apocryph.Runtime.FunctionApp.IpfsQueryStream.Run";
            }

            var slotCount = 10; // 30

            var pingChainId = Guid.NewGuid();
            var pongChainId = Guid.NewGuid();

            // For test harness we create seed blocks with valid references in the states
            var pingReference = Guid.NewGuid();
            var pongReference = Guid.NewGuid();

            Func<object, Task> merkleTreeSaver = value => context.CallWorkerAsync<bool>(hashRegistryWriter, new { value }, cancellationToken);
            var chains = new Dictionary<Guid, Chain>
            {
                {pingChainId, new Chain(slotCount, new Block(
                    new Hash(new byte[] {}),
                    pingChainId,
                    null,
                    Guid.NewGuid(),
                    new Dictionary<string, byte[]>
                    {
                        {
                            typeof(ChainAgentPing).FullName! + ".Run",
                            JsonSerializer.SerializeToUtf8Bytes(new ChainAgentState {OtherReference = pongReference})
                        },
                        {
                            typeof(ChainAgentPong).FullName! + ".Run",
                            JsonSerializer.SerializeToUtf8Bytes(new ChainAgentState {OtherReference = pingReference})
                        }
                    },
                    (await MerkleTree.CreateAsync(new ICommand[] { }, merkleTreeSaver)).Root,
                    (await MerkleTree.CreateAsync(new ICommand[]
                    {
                        new Invoke(pingReference, (typeof(string).FullName!, JsonSerializer.SerializeToUtf8Bytes("Init")))
                    }, merkleTreeSaver)).Root,
                    new Dictionary<Guid, (string, string[])>
                    {
                        {pongReference, (typeof(ChainAgentPong).FullName! + ".Run", new[] {typeof(string).FullName!})},
                        {pingReference, (typeof(ChainAgentPing).FullName! + ".Run", new[] {typeof(string).FullName!})}
                    }))}
            };

            await context.StreamActionAsync("Apocryph.Runtime.FunctionApp.ChainListStream.Run", new
            {
                self,
                hashRegistryWriter,
                hashRegistryReader,
                outsideGossipsStream,
                outsideQueriesStream,
                slotGossipsStream,
                chains
            });

            await context.BindOutput(cancellationToken);
        }
    }
}