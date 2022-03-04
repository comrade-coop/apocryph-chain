using System;
using System.Linq;
using System.Text.Json;
using Apocryph.Ipfs.Serialization;

namespace Apocryph.Consensus
{
    public class AgentReferenceData : IEquatable<AgentReferenceData>
    {
        public string Type { get; private set; }
        public byte[] Data { get; private set; }
        public AgentReference[] References { get; private set; }

        public AgentReferenceData(string type, byte[] data, AgentReference[] references)
        {
            Type = type;
            Data = data;
            References = references;
        }

        public static AgentReferenceData From(object? value, AgentReference[]? references = null)
        {
            return new AgentReferenceData(
                value?.GetType()?.FullName ?? "",
                JsonSerializer.SerializeToUtf8Bytes(value, ApocryphSerializationOptions.JsonSerializerOptions),
                references ?? new AgentReference[] { });
        }

        public T Deserialize<T>()
        {
            return JsonSerializer.Deserialize<T>(Data, ApocryphSerializationOptions.JsonSerializerOptions);
        }

        public override bool Equals(object? other)
        {
            return other is AgentReferenceData otherReferenceData && Equals(otherReferenceData);
        }

        public bool Equals(AgentReferenceData? other)
        {
            return other != null && Type.Equals(other.Type) && Data.SequenceEqual(other.Data) && References.SequenceEqual(other.References);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Type);
            Array.ForEach(Data, hashCode.Add);
            Array.ForEach(References, hashCode.Add);
            return hashCode.ToHashCode();
        }
    }
}