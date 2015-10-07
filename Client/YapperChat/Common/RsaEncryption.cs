using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace YapperChat.Common
{
    internal class RsaEncryption
    {
        private const int KeySize = 2048;

        public static byte[] EncryptMessage(byte[] message, byte[] publicKey)
        {
            try
            {
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(KeySize))
                {
                    //rsa.ImportCspBlob(publicKey);
                    rsa.ImportParameters(FromBinaryToRSAParameters(publicKey));

                    byte[] encryptedBytes = rsa.Encrypt(message, true);
                    return encryptedBytes;
                }
            }
            catch
            {
                return null;
            }
        }

        public static byte[] DecryptMessage(byte[] message, byte[] privateKey)
        {
            try
            {
                using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(KeySize))
                {
                    //rsa.ImportCspBlob(privateKey);
                    rsa.ImportParameters(FromBinaryToRSAParameters(privateKey));
                    byte[] encryptedBytes = message;

                    byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, true);
                    return decryptedBytes;
                }
            }
            catch
            {
                return null;
            }
        }

        public static void GenerateKeys(out byte[] publicKey, out byte[] privateKey)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(KeySize);

            /* windows phone 7 doesn't support ExportCspBlob
            privateKey = rsa.ExportCspBlob(true);
            publicKey = rsa.ExportCspBlob(false);
             */
            privateKey = FromRSAParametersToBinary(rsa.ExportParameters(true), true);
            publicKey = FromRSAParametersToBinary(rsa.ExportParameters(false), false);
        }

        private static byte[] FromRSAParametersToBinary(RSAParameters parameters, bool includePrivateKey)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                WriteByteArrayToStream(stream, parameters.Exponent);
                WriteByteArrayToStream(stream, parameters.Modulus);

                if (includePrivateKey)
                {
                    WriteByteArrayToStream(stream, parameters.P);
                    WriteByteArrayToStream(stream, parameters.Q);
                    WriteByteArrayToStream(stream, parameters.DP);
                    WriteByteArrayToStream(stream, parameters.DQ);
                    WriteByteArrayToStream(stream, parameters.InverseQ);
                    WriteByteArrayToStream(stream, parameters.D);
                }

                return stream.ToArray();
            }            
        }

        private static void WriteByteArrayToStream(Stream stream, byte[] data)
        {
            stream.Write(BitConverter.GetBytes(data.Length), 0, sizeof(int));
            stream.Write(data, 0, data.Length);
        }

        private static RSAParameters FromBinaryToRSAParameters(byte[] data)
        {
            RSAParameters parameters;
            using (MemoryStream stream = new MemoryStream(data))
            {
                parameters.Exponent = ReadByteArrayFromStream(stream);
                parameters.Modulus = ReadByteArrayFromStream(stream);
                parameters.P = ReadByteArrayFromStream(stream);
                parameters.Q = ReadByteArrayFromStream(stream);
                parameters.DP = ReadByteArrayFromStream(stream);
                parameters.DQ = ReadByteArrayFromStream(stream);
                parameters.InverseQ = ReadByteArrayFromStream(stream);
                parameters.D = ReadByteArrayFromStream(stream);

                return parameters;
            }
        }

        private static byte[] ReadByteArrayFromStream(Stream stream)
        {
            byte[] lengthBytes = new byte[sizeof(int)];
            if (stream.Read(lengthBytes, 0, sizeof(int)) == sizeof(int))
            {
                int length = BitConverter.ToInt32(lengthBytes, 0);

                if (length + stream.Position <= stream.Length)
                {
                    byte[] data = new byte[length];
                    stream.Read(data, 0, length);
                    return data;
                }
            }

            return null;
        }
    }
}
