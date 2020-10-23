using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Apocryph.Core.Consensus.Blocks
{
    public class MerkleTreeProof : IEquatable<MerkleTreeProof>
    {
        public object Value { get; set; }
        public List<(bool, Hash)> Hashes { get; set; } // Leaves first
        public Hash Root { get; set; }

        public MerkleTreeProof(object value, List<(bool, Hash)> hashes, Hash root)
        {
            Value = value;
            Hashes = hashes;
            Root = root;
        }

        public bool Equals(MerkleTreeProof? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Value.Equals(other.Value) && Hashes.SequenceEqual(other.Hashes) && Root.Equals(other.Root);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MerkleTreeProof)obj);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            Hashes.ForEach(hash.Add);
            return hash.ToHashCode();
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            var endBuilder = new StringBuilder();
            builder.Append("MerkleTreeProof(");
            builder.Append(Root);
            builder.Append(" = ");
            foreach (var (isLeft, other) in Enumerable.Reverse(Hashes))
            {
                if (isLeft)
                {
                    builder.Append("(");
                    endBuilder.Insert(0, $", {other})");
                }
                else
                {
                    builder.Append($"({other}, ");
                    endBuilder.Insert(0, ")");
                }
            }
            builder.Append("(= ");
            builder.Append(Value);
            builder.Append(")");
            builder.Append(endBuilder);
            return builder.ToString();
        }

        public bool Validate()
        {
            var hash = Hash.From(Value);
            foreach (var (isLeft, other) in Hashes)
            {
                var node = isLeft ? new MerkleTreeNode(hash, other) : new MerkleTreeNode(other, hash);
                hash = Hash.From(node);
            }
            return hash.Equals(Root);
        }
    }
}