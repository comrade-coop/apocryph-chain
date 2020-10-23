using System;
using System.Text.Json;
using System.Text.Encodings.Web;
using Apocryph.Core.Consensus.Blocks;

namespace Apocryph.Core.Consensus.Serialization
{
    public class ApocryphSerializationOptions
    {
        public static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters =
            {
                { new TypeDictionaryConverter() },
                { new NonStringKeyDictionaryConverter() },
                { new ObjectParameterConstructorConverter() {
                    AllowSubtypes = true
                } }
            }
        };

        public static JsonSerializerOptions JsonSerializerOptionsMerkleTree(Type type)
        {
            return new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters =
                {
                    { new TypeDictionaryConverter() },
                    { new NonStringKeyDictionaryConverter() },
                    { new ObjectParameterConstructorConverter() {
                        AllowSubtypes = true,
                        SubtypeFilter = (from, to) => from == typeof(object) ? (type.IsAssignableFrom(to) || typeof(MerkleTreeNode).IsAssignableFrom(to)) : true,
                    } }
                }
            };
        }
    }
}