using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Apocryph.Core.Consensus.Blocks;
using Apocryph.Core.Consensus.Serialization;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;
using Ipfs.Http;
using Ipfs;

namespace Apocryph.Runtime.FunctionApp
{
    public class HashRegistryReader
    {
        private static IpfsClient? _ipfsClient;

        [FunctionName(nameof(HashRegistryReader))]
        [return: Perper("$return")]
        public async Task<object> Run([PerperWorkerTrigger] PerperWorkerContext context,
            [Perper("type")] string _type,
            [Perper("allowMerkleNode")] bool? allowMerkleNode,
            [Perper("hash")] Hash hash,
            CancellationToken cancellationToken)
        {
            if (_ipfsClient == null)
            {
                _ipfsClient = new IpfsClient();
            }

            var type = Type.GetType(_type)!;

            var cid = new Cid { ContentType = "raw", Hash = new MultiHash("sha2-256", hash.Value) };

            // FIXME: The Ipfs.Http.Client library uses a GET request for Block.GetAsync, which doesn't work since go-ipfs v0.5.
            // See https://github.com/richardschneider/net-ipfs-http-client/issues/62 for more details.
            // var block = await _ipfsClient.Block.GetAsync(multihash);

            var stream = await _ipfsClient.PostDownloadAsync("block/get", cancellationToken, cid);


            var value = (allowMerkleNode ?? false) ?
                await JsonSerializer.DeserializeAsync(stream, typeof(object), ApocryphSerializationOptions.JsonSerializerOptionsMerkleTree(type), cancellationToken) :
                await JsonSerializer.DeserializeAsync(stream, type, ApocryphSerializationOptions.JsonSerializerOptions, cancellationToken);

            return value;
        }
    }
}