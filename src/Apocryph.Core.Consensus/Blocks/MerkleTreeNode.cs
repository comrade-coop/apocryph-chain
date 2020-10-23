using System;

namespace Apocryph.Core.Consensus.Blocks
{
    public struct MerkleTreeNode : IEquatable<MerkleTreeNode>
    {
        public Hash Left { get; set; }
        public Hash Right { get; set; }

        public MerkleTreeNode(Hash left, Hash right)
        {
            Left = left;
            Right = right;
        }

        public bool Equals(MerkleTreeNode other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Left.Equals(other.Left) && Right.Equals(other.Right);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MerkleTreeNode)obj);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Left);
            hash.Add(Right);
            return hash.ToHashCode();
        }

        public override string ToString()
        {
            return $"MerkleTreeNode({Left}, {Right})";
        }
    }
}