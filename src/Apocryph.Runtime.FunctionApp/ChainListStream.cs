using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;
using Apocryph.Core.Consensus.Blocks;
using Apocryph.Core.Consensus.VirtualNodes;

namespace Apocryph.Runtime.FunctionApp
{
    public class ChainListStream
    {
        [FunctionName(nameof(ChainListStream))]
        public async Task Run([PerperStreamTrigger] PerperStreamContext context,
            [Perper("self")] Peer self,
            [Perper("outsideGossipsStream")] string outsideGossipsStream,
            [Perper("outsideQueriesStream")] string outsideQueriesStream,
            [Perper("hashRegistryWriter")] string hashRegistryWriter,
            [Perper("hashRegistryReader")] string hashRegistryReader,
            [Perper("slotGossipsStream")] string slotGossipsStream,
            [Perper("chains")] IDictionary<Guid, Chain> chains,
            [Perper("output")] IAsyncCollector<int> output,
            CancellationToken cancellationToken)
        {
            await using var peering = context.DeclareStream("Peering", typeof(PeeringStream));
            var gossips = peering;
            var queries = peering;
            var reports = peering;
            var hashes = peering;
            await using var salts = context.DeclareStream("Salts", typeof(SaltsStream));

            await using var slotGossips = context.DeclareStream("Salts", slotGossipsStream);

            await using var chain = await context.StreamFunctionAsync("Chain", typeof(ChainStream), new
            {
                self,
                chains,
                gossips,
                queries,
                hashRegistryWriter,
                hashRegistryReader,
                salts = salts.Subscribe(),
                slotGossips = slotGossips.Subscribe()
            });

            await context.StreamFunctionAsync(slotGossips, new
            {
                self,
                claims = chain.Subscribe()
            });

            // HACK: Create an empty stream for the global IBC
            await using var validator = await context.StreamFunctionAsync("DummyStream", new { });

            await using var ibc = await context.StreamFunctionAsync("IBC-global", typeof(IBCStream), new
            {
                chain = chain.Subscribe(),
                validator = validator.Subscribe(),
                gossips = gossips.Subscribe(),
                nodes = new Dictionary<Guid, Node?[]>(),
                node = default(Node?)
            });
            await using var filter = await context.StreamFunctionAsync("Filter-global", typeof(FilterStream), new
            {
                ibc = ibc.Subscribe(),
                gossips = gossips.Subscribe(),
                hashRegistryWriter,
                hashRegistryReader,
                chains
            });

            await context.StreamFunctionAsync(salts, new
            {
                filter = filter.Subscribe(),
                hashRegistryReader,
                chains
            });

            await using var outsideGossips = await context.StreamFunctionAsync(outsideQueriesStream, new
            {
                self,
                chain = chain.Subscribe(),
                gossips = peering.Subscribe()
            });
            await using var outsideQueries = await context.StreamFunctionAsync(outsideQueriesStream, new
            {
                self,
                chain = chain.Subscribe(),
                queries = peering.Subscribe()
            });

            await context.StreamFunctionAsync(peering, new
            {
                factory = chain.Subscribe(),
                initial = new List<IPerperStream>() { filter, outsideGossips, outsideQueries },
            });

            await using var reportsStream = await context.StreamActionAsync("Reports", typeof(ReportsStream), new
            {
                chain = chain.Subscribe(),
                filter = filter.Subscribe(),
                reports = reports.Subscribe(),
                hashRegistryReader,
                nodes = new Dictionary<Guid, Node?[]>()
            });

            await context.BindOutput(cancellationToken);
        }
    }
}