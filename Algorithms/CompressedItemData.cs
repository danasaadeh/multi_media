using System.Collections.Generic;

namespace Compression_Vault.Algorithms
{
    /// <summary>
    /// نموذج البيانات المضغوطة المشترك بين خوارزميات الضغط
    /// </summary>
    public class CompressedItemData
    {
        public string ItemName { get; set; }
        public long OriginalSize { get; set; }
        public long CompressedSize { get; set; }
        public Dictionary<byte, int> FrequencyTable { get; set; }
        public byte[] CompressedData { get; set; }
        public byte ValidBitsInLastByte { get; set; }
        public bool IsCompressed { get; set; }
        public List<CompressedItemData> FolderFiles { get; set; }
    }
} 