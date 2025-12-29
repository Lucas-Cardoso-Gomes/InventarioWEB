using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Web.Services
{
    public static class EncryptionHelper
    {
        public static string Encrypt(string plainText, string keyString)
        {
            byte[] key = GetKey(keyString);
            byte[] iv = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                    byte[] encryptedContent = ms.ToArray();
                    byte[] result = new byte[iv.Length + encryptedContent.Length];
                    Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                    Buffer.BlockCopy(encryptedContent, 0, result, iv.Length, encryptedContent.Length);
                    return Convert.ToBase64String(result);
                }
            }
        }

        public static string Decrypt(string cipherText, string keyString)
        {
            byte[] fullCipher = Convert.FromBase64String(cipherText);
            byte[] key = GetKey(keyString);
            byte[] iv = new byte[16];
            byte[] cipher = new byte[fullCipher.Length - 16];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, 16, cipher, 0, cipher.Length);

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream(cipher))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        private static byte[] GetKey(string secret)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(secret));
            }
        }
    }
}
