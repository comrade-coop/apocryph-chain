using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Apocryph.Core.Consensus.Serialization;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;
using Ipfs.Http;

namespace Apocryph.Runtime.FunctionApp
{
    public class HashRegistryWriter
    {
        private static IpfsClient? _ipfsClient;
        [FunctionName(nameof(HashRegistryWriter))]
        [return: Perper("$return")]
        public async Task<bool> Run([PerperWorkerTrigger] PerperWorkerContext context,
            [Perper("value")] object value,
            CancellationToken cancellationToken)
        {
            if (_ipfsClient == null)
            {
                _ipfsClient = new IpfsClient();
            }

            var serialized = JsonSerializer.SerializeToUtf8Bytes(new RootLevelTypeWrapper(value), ApocryphSerializationOptions.JsonSerializerOptions);

            // FIXME: Using "raw" here instead of "json", since Ipfs.Http.Client doesn't seem to consider "json" a valid MultiCodec
            await _ipfsClient.Block.PutAsync(serialized, "raw", "sha2-256", "base58btc", false, cancellationToken);

            return true;
        }
    }
}