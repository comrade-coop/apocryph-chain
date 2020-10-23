using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Text.Json;
using Apocryph.Core.Consensus.Blocks;
using Apocryph.Core.Consensus.Serialization;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace TestHarness.FunctionApp.Mock
{
    public class HashRegistryWriter
    {
        internal static readonly ConcurrentDictionary<Hash, byte[]> _storedValues = new ConcurrentDictionary<Hash, byte[]>();

        [FunctionName(nameof(HashRegistryWriter))]
        [return: Perper("$return")]
        public Task<bool> Run([PerperWorkerTrigger] PerperWorkerContext context,
            [Perper("value")] object value)
        {
            using var sha256Hash = SHA256.Create();
            var serialized = JsonSerializer.SerializeToUtf8Bytes(value, ApocryphSerializationOptions.JsonSerializerOptions);
            var hash = new Hash(sha256Hash.ComputeHash(serialized));

            _storedValues.TryAdd(hash, serialized);

            // Console.WriteLine("Store: {0}", hash);

            return Task.FromResult(true);
        }
    }
}