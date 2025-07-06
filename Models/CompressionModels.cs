using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Compression_Vault.Models
{
    /// <summary>
    /// معلومات الرأس المشتركة
    /// </summary>
    public class HeaderInfo
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public List<CompressedItemInfo> Items { get; set; } = new List<CompressedItemInfo>();
    }

    /// <summary>
    /// معلومات العنصر المضغوط المشتركة
    /// </summary>
    public class CompressedItemInfo
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public int FileCount { get; set; }
        public bool IsFolder { get; set; }
    }

    /// <summary>
    /// بيانات العنصر المفكوك المشتركة
    /// </summary>
    public class DecompressedItemData
    {
        public string ItemName { get; set; }
        public string OutputPath { get; set; }
        public long OriginalSize { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// فئة مساعدة لحساب hash كلمة المرور
    /// </summary>
    public static class PasswordHelper
    {
        /// <summary>
        /// حساب hash كلمة المرور باستخدام SHA256
        /// </summary>
        public static byte[] ComputePasswordHash(string password)
        {
            if (string.IsNullOrEmpty(password))
                return new byte[0];

            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        /// <summary>
        /// التحقق من صحة كلمة المرور
        /// </summary>
        public static bool VerifyPassword(string password, byte[] storedHash)
        {
            if (string.IsNullOrEmpty(password) || storedHash == null || storedHash.Length == 0)
                return false;

            var computedHash = ComputePasswordHash(password);
            
            if (computedHash.Length != storedHash.Length)
                return false;

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != storedHash[i])
                    return false;
            }

            return true;
        }
    }
} 