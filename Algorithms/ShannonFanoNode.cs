using System;

namespace Compression_Vault.Algorithms
{
    public class ShannonFanoNode : IComparable<ShannonFanoNode>
    {
        public byte Symbol { get; set; }
        public int Frequency { get; set; }
        public ShannonFanoNode Left { get; set; }
        public ShannonFanoNode Right { get; set; }
        public bool IsLeaf => Left == null && Right == null;

        public ShannonFanoNode(byte symbol, int frequency)
        {
            Symbol = symbol;
            Frequency = frequency;
        }

        public ShannonFanoNode(ShannonFanoNode left, ShannonFanoNode right)
        {
            Left = left;
            Right = right;
            Frequency = left.Frequency + right.Frequency;
        }

        public int CompareTo(ShannonFanoNode other)
        {
            // Sort by frequency in descending order (highest first)
            return other.Frequency.CompareTo(Frequency);
        }
    }
} 