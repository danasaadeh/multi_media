using System;
using System.Collections.Generic;
using System.Linq;

namespace Compression_Vault.Algorithms
{
    /// <summary>
    /// مسؤول عن بناء شجرة Shannon-Fano
    /// </summary>
    public static class ShannonFanoTreeBuilder
    {
        /// <summary>
        /// بناء شجرة Shannon-Fano من جدول التكرار
        /// </summary>
        public static ShannonFanoNode BuildShannonFanoTree(Dictionary<byte, int> frequencies)
        {
            // Convert to list and sort by frequency (descending)
            var nodes = new List<ShannonFanoNode>();
            foreach (var kvp in frequencies)
            {
                nodes.Add(new ShannonFanoNode(kvp.Key, kvp.Value));
            }
            nodes.Sort();

            // Build Shannon-Fano tree using recursive division
            return BuildShannonFanoTreeRecursive(nodes);
        }

        /// <summary>
        /// بناء شجرة Shannon-Fano بشكل متكرر
        /// </summary>
        private static ShannonFanoNode BuildShannonFanoTreeRecursive(List<ShannonFanoNode> nodes)
        {
            if (nodes.Count == 1)
                return nodes[0];

            if (nodes.Count == 2)
                return new ShannonFanoNode(nodes[0], nodes[1]);

            // Calculate total frequency
            int totalFrequency = nodes.Sum(n => n.Frequency);

            // Find the best split point
            int currentSum = 0;
            int bestSplitIndex = 0;
            int bestDifference = int.MaxValue;

            for (int i = 0; i < nodes.Count - 1; i++)
            {
                currentSum += nodes[i].Frequency;
                int difference = Math.Abs(currentSum - (totalFrequency - currentSum));
                
                if (difference < bestDifference)
                {
                    bestDifference = difference;
                    bestSplitIndex = i;
                }
            }

            // Split the list
            var leftNodes = nodes.Take(bestSplitIndex + 1).ToList();
            var rightNodes = nodes.Skip(bestSplitIndex + 1).ToList();

            // Recursively build subtrees
            var leftChild = BuildShannonFanoTreeRecursive(leftNodes);
            var rightChild = BuildShannonFanoTreeRecursive(rightNodes);

            return new ShannonFanoNode(leftChild, rightChild);
        }

        /// <summary>
        /// توليد رموز Shannon-Fano من الشجرة
        /// </summary>
        public static Dictionary<byte, string> GenerateShannonFanoCodes(ShannonFanoNode root)
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
        private static void GenerateCodesRecursive(ShannonFanoNode node, string code, Dictionary<byte, string> codes)
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