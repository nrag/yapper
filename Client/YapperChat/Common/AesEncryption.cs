using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace YapperChat.Common
{
    class AesEncryption
    {
        private const int KeySize = 256;
        private const int BlockSize = 128;

        public static byte[] EncryptMessage(byte[] message, out byte[] key)
        {
            byte[] ret = null;
            using (AesManaged aes = new AesManaged())
            {
                aes.KeySize = AesEncryption.KeySize;
                aes.BlockSize = AesEncryption.BlockSize;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(message, 0, message.Length);
                    }
                    ret = msEncrypt.ToArray();
                }

                key = aes.Key.Concat(aes.IV).ToArray();
            }

            return ret;
        }

        public static byte[] DecryptMessage(byte[] encryptedMessage, byte[] key)
        {
            using (AesManaged aes = new AesManaged())
            {
                aes.Key = key.Take(AesEncryption.KeySize / 8).ToArray();
                aes.IV = key.Skip(AesEncryption.KeySize / 8).ToArray();

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream msDecrypt = new MemoryStream())
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
                    {

                        csDecrypt.Write(encryptedMessage, 0, encryptedMessage.Length);
                    }
                    return msDecrypt.ToArray();
                }
            }
        }
    }
}
