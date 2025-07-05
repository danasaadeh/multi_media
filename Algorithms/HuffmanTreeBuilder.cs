using System.Collections.Generic;
using System.Linq;

namespace Compression_Vault.Algorithms
{
    /// <summary>
    /// مسؤول عن بناء شجرة Huffman
    /// </summary>
    public static class HuffmanTreeBuilder
    {
        /// <summary>
        /// بناء شجرة Huffman من جدول التكرار
        /// </summary>
        public static HuffmanNode BuildHuffmanTree(Dictionary<byte, int> frequencies)
        {
            var nodes = new List<HuffmanNode>();
            foreach (var kvp in frequencies)
            {
                nodes.Add(new HuffmanNode(kvp.Key, kvp.Value));
            }

            while (nodes.Count > 1)
            {
                nodes.Sort();
                var left = nodes[0];
                var right = nodes[1];
                nodes.RemoveRange(0, 2);
                nodes.Add(new HuffmanNode(left, right));
            }

            return nodes.FirstOrDefault();
        }

        /// <summary>
        /// توليد رموز Huffman من الشجرة
        /// </summary>
        public static Dictionary<byte, string> GenerateHuffmanCodes(HuffmanNode root)
        {
            var codes = new Dictionary<byte, string>();
            if (root != null)
            {
                GenerateCodesRecursive(root, "", codes);
            }
            return codes;
        }

        /// <summary>
        /// توليد الرموز بشكل متكرر
        /// </summary>
        private static void GenerateCodesRecursive(HuffmanNode node, string code, Dictionary<byte, string> codes)
        {
            if (node.IsLeaf)
            {
                codes[node.Symbol] = code;
                return;
            }

            GenerateCodesRecursive(node.Left, code + "0", codes);
            GenerateCodesRecursive(node.Right, code + "1", codes);
        }
    }
} 