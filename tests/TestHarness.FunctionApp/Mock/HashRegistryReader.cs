using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Apocryph.Core.Consensus.Blocks;
using Apocryph.Core.Consensus.Serialization;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace TestHarness.FunctionApp.Mock
{
    public class HashRegistryReader
    {
        [FunctionName(nameof(HashRegistryReader))]
        [return: Perper("$return")]
        public async Task<object?> Run([PerperWorkerTrigger] PerperWorkerContext context,
            [Perper("type")] string _type,
            [Perper("allowMerkleNode")] bool? allowMerkleNode,
            [Perper("hash")] Hash hash,
            CancellationToken cancellationToken)
        {
            var type = Type.GetType(_type)!;
//             Console.WriteLine("Read: {0}", hash);
            byte[]? serialized;

            // Simulate IPFS's behavior where trying to get a nonexistent object blocks until the object is available.
            while (!HashRegistryWriter._storedValues.TryGetValue(hash, out serialized))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(50, cancellationToken);
            }

            var value = (allowMerkleNode ?? false) ?
                JsonSerializer.Deserialize(serialized!, typeof(object), ApocryphSerializationOptions.JsonSerializerOptionsMerkleTree(type)) :
                JsonSerializer.Deserialize(serialized!, type, ApocryphSerializationOptions.JsonSerializerOptions);

            return value;
        }
    }
}