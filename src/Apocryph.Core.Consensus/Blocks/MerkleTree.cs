using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Apocryph.Core.Consensus.Blocks
{
    public struct MerkleTree
    {
        public object[] Values { get; set; }
        public Hash[] Hashes { get; set; }
        public Hash? Root { get => Hashes.Length > 0 ? Hashes[0] : (Hash?)null; }

        public MerkleTree(object[] values, Hash[] hashes)
        {
            Values = values;
            Hashes = hashes;
        }

        public MerkleTreeProof GenerateProof(int index)
        {
            var hashes = new List<(bool, Hash)>();

            var hashIndex = index + Values.Length - 1;
            while (hashIndex != 0)
            {
                var (parentIndex, isLeft) = ParentHashIndex(hashIndex);
                var siblingIndex = isLeft ? RightChildHashIndex(parentIndex) : LeftChildHashIndex(parentIndex);
                hashes.Add((isLeft, Hashes[siblingIndex]));
                hashIndex = parentIndex;
            }

            return new MerkleTreeProof(Values[index], hashes, Root!.Value);
        }

        public static async Task<MerkleTree> CreateAsync(object[] values, Func<object, Task> saveObject)
        {
            if (values.Length == 0) return new MerkleTree(new object[0], new Hash[0]);
            // Algorithm: We want to construct a complete binary tree of hashes and store it into an array.
            // The array is ordered with the root hash at index 0, and its children at 1 and 2, etc. (refs https://en.wikipedia.org/wiki/Binary_tree#Arrays)
            var hashes = new Hash[2 * values.Length - 1];
            for (var i = 0; i < values.Length; i++)
            {
                await saveObject.Invoke(values[i]);
                hashes[i + values.Length - 1] = Hash.From(values[i]);
            }

            for (var i = values.Length - 2; i >= 0; i--)
            {
                var node = new MerkleTreeNode(hashes[LeftChildHashIndex(i)], hashes[RightChildHashIndex(i)]);
                await saveObject.Invoke(node);
                hashes[i] = Hash.From(node);
            }

            return new MerkleTree(values, hashes);
        }

        public static async Task<MerkleTree> LoadAsync(Hash? root, Func<Hash, Task<object>> loadObject)
        {
            if (root == null) return new MerkleTree(new object[0], new Hash[0]);
            var hashes = new List<Hash>();
            var values = new List<object>();
            var valuesStarted = false;
            hashes.Add(root.Value);

            for (var i = 0; i < hashes.Count; i++)
            {
                var value = await loadObject.Invoke(hashes[i]);
                if (value is MerkleTreeNode node)
                {
                    if (valuesStarted)
                    {
                        throw new Exception($"Unexpected incomplete binary tree while loading merkle tree with root {root}");
                    }
                    hashes.Add(node.Left);
                    hashes.Add(node.Right);
                }
                else
                {
                    valuesStarted = true;
                    values.Add(value);
                }
            }

            return new MerkleTree(values.ToArray(), hashes.ToArray());
        }

        private static int LeftChildHashIndex(int nodeIndex)
        {
            return nodeIndex * 2 + 1;
        }

        private static int RightChildHashIndex(int nodeIndex)
        {
            return nodeIndex * 2 + 2;
        }

        private static (int, bool isLeft) ParentHashIndex(int nodeIndex)
        {
            return ((nodeIndex - 1) / 2, nodeIndex % 2 == 1);
        }
    }
}