using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apocryph.Agents.Koth;
using Apocryph.Ipfs;
using Apocryph.Ipfs.Fake;
using Apocryph.Ipfs.MerkleTree;
using Apocryph.Model;
using NUnit.Framework;
using Perper.Application;
using Perper.Extensions;
using Perper.Model;

namespace Apocryph.Tests.Koth
{
    [TestFixture]
    public class Koth_Mining
    {
        private static readonly ManualResetEvent ManualResetEvent = new(false);

        public static IEnumerable<Hash<Chain>> GetChainsScenario(Hash<object>? consensusParameters, int slotsCount, int numberOfChains)
        {
            var hashResolver = new FakeHashResolver();
            var consensusType = "Dummy";
            var messageFilter = new [] { typeof(int).FullName! };

            for (int i = 0; i < numberOfChains; i++)
            {
                var fakeChainId = Hash.From(i).Cast<Chain>();

                var agentStates = new[] {
                    new AgentState(0, AgentReferenceData.From(new AgentReference(fakeChainId, 0, messageFilter)), Hash.From("AgentInc")),
                    new AgentState(1, AgentReferenceData.From(new AgentReference(fakeChainId, 1, messageFilter)), Hash.From("AgentDec"))
                };

                var agentStatesTree = MerkleTreeBuilder.CreateRootFromValues(hashResolver, agentStates, 2).GetAwaiter().GetResult();
                var chain = new Chain(new ChainState(agentStatesTree, agentStates.Length), consensusType, consensusParameters, slotsCount);
                yield return Hash.From(chain);
            }
        }

        static async Task Init()
        {
            var hashResolver = new FakeHashResolver();
            var peerConnector = (new FakePeerConnectorProvider()).GetPeerConnector();
            var cancellationTokenSource = new CancellationTokenSource();


            Hash<Chain>[] chainHashes = GetChainsScenario(null, 16, 3).ToArray();

            var kothProcessor = await PerperContext.StartAgentAsync(nameof(KothAgent), hashResolver, peerConnector, cancellationTokenSource.Token);
            var stream = await kothProcessor.CallAsync<PerperStream>("GetKoTHProcessor");

            await PerperContext.StartAgentAsync(nameof(KothMinerAgent), hashResolver, peerConnector, cancellationTokenSource.Token, stream, chainHashes);

            await Task.Delay(10000);

            ManualResetEvent.Set();
        }

        [Test]
        public void Should_Pass()
        {
            ManualResetEvent.WaitOne(TimeSpan.FromMinutes(1));
        }

        [OneTimeSetUp]
        public Task OneTimeSetup()
        {
            new PerperStartup()
                .AddClassHandlers<KothAgent>()
                .AddClassHandlers<KothMinerAgent>()
                .AddInitHandler("", Init)
                .RunAsync(default)
                .ConfigureAwait(false);

            return Task.CompletedTask;
        }
    }
}



