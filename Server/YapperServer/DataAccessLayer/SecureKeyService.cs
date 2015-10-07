using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccessLayer
{
    internal enum KeyType
    {
        AuthCookieV1,
    }

    internal class SecureSigningService
    {
        private const int KeySize = 2048;

        private Dictionary<KeyType, byte[]> publicKeys = new Dictionary<KeyType, byte[]>();

        private Dictionary<KeyType, byte[]> privateKeys = new Dictionary<KeyType, byte[]>();

        private const string KeyQueryString =
            "SELECT PublicKey, PrivateKey from dbo.SecureKeyTable WHERE KeyType = @type;";

        private const string InsertKeyCommandString = "INSERT into dbo.SecureKeyTable (PublicKey, PrivateKey, KeyType) VALUES (@publicKey, @privateKey, @keyType);";

        private ReaderWriterLockSlim RWlock = new ReaderWriterLockSlim();

        private static SecureSigningService _instance = new SecureSigningService();

        public static SecureSigningService Instance
        {
            get
            {
                return SecureSigningService._instance;
            }
        }

        public string SignAuthCookieV1(string cookie)
        {
            byte[] privateKey = this.GetPrivateKey(KeyType.AuthCookieV1);

            //// The array to store the signed message in bytes
            byte[] signedBytes;
            using (var rsa = new RSACryptoServiceProvider())
            {
                // Write the message to a byte array using UTF8 as the encoding.
                
                byte[] originalData = System.Text.Encoding.UTF8.GetBytes(cookie);

                try
                {
                    // Import the private key used for signing the message
                    rsa.ImportParameters(SecureSigningService.FromBinaryToRSAParameters(privateKey));

                    signedBytes = rsa.SignData(originalData, CryptoConfig.MapNameToOID("SHA512"));
                }
                catch (CryptographicException e)
                {
                    Console.WriteLine(e.Message);
                    return null;
                }
                finally
                {
                    //// Set the keycontainer to be cleared when rsa is garbage collected.
                    rsa.PersistKeyInCsp = false;
                }
            }

            // Convert the a base64 string before returning
            return Convert.ToBase64String(signedBytes);
        }

        public bool VerifyAuthCookieV1(string cookie, string sign)
        {
            byte[] publicKey = this.GetPublicKey(KeyType.AuthCookieV1);

            using (var rsa = new RSACryptoServiceProvider())
            {
                byte[] originalData = System.Text.Encoding.UTF8.GetBytes(cookie);
                byte[] signedBytes = Convert.FromBase64String(sign);

                try
                {
                    // Import the private key used for signing the message
                    rsa.ImportParameters(SecureSigningService.FromBinaryToRSAParameters(publicKey));

                    return rsa.VerifyData(originalData, CryptoConfig.MapNameToOID("SHA512"), signedBytes);
                }
                catch (CryptographicException e)
                {
                    return false;
                }
                finally
                {
                    //// Set the keycontainer to be cleared when rsa is garbage collected.
                    rsa.PersistKeyInCsp = false;
                }
            }
        }

        private byte[] GetPublicKey(KeyType type)
        {
            try
            {
                this.RWlock.EnterReadLock();
                if (this.publicKeys.ContainsKey(type))
                {
                    return this.publicKeys[type];
                }
            }
            finally
            {
                this.RWlock.ExitReadLock();
            }

            // Load or create the keys
            this.LoadKeys(type);

            try
            {
                this.RWlock.EnterReadLock();
                if (this.publicKeys.ContainsKey(type))
                {
                    return this.publicKeys[type];
                }
            }
            finally
            {
                this.RWlock.ExitReadLock();
            }

            return null;
        }

        private byte[] GetPrivateKey(KeyType type)
        {
            try
            {
                this.RWlock.EnterReadLock();
                if (this.privateKeys.ContainsKey(type))
                {
                    return this.privateKeys[type];
                }
            }
            finally
            {
                this.RWlock.ExitReadLock();
            }

            // Load or create the keys
            this.LoadKeys(type);

            try
            {
                this.RWlock.EnterReadLock();
                if (this.privateKeys.ContainsKey(type))
                {
                    return this.privateKeys[type];
                }
            }
            finally
            {
                this.RWlock.ExitReadLock();
            }

            return null;
        }

        private void CreateKeys(KeyType type, SqlConnection connection, SqlTransaction sqlTransaction, out byte[] publicKey, out byte[] privateKey)
        {
            byte[] publicKeyInternal = null;
            byte[] privateKeyInternal = null;
            publicKey = null;
            privateKey = null;
            SecureSigningService.GenerateKeys(out publicKeyInternal, out privateKeyInternal);

            // Create the Command and Parameter objects.
            using (SqlCommand command = new SqlCommand(SecureSigningService.InsertKeyCommandString, connection, sqlTransaction))
            {
                command.Parameters.AddWithValue("@publicKey", publicKeyInternal);
                command.Parameters.AddWithValue("@privateKey", privateKeyInternal);
                command.Parameters.AddWithValue("@keyType", type);

                command.ExecuteScalar();

                sqlTransaction.Commit();

                publicKey = publicKeyInternal;
                privateKey = privateKeyInternal;
            }
        }

        private void LoadKeys(KeyType type)
        {
            // Create and open the connection in a using block. This
            // ensures that all resources will be closed and disposed
            // when the code exits.
            using (SqlConnection connection = new SqlConnection(Globals.SqlConnectionString))
            {
                connection.Open();
                using (SqlTransaction sqlTransaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    bool lockHeld = false;

                    // Open the connection in a try/catch block.
                    // Create and execute the DataReader, writing the result
                    // set to the console window.
                    try
                    {
                        // Create the Command and Parameter objects.
                        using (SqlCommand command = new SqlCommand(SecureSigningService.KeyQueryString, connection, sqlTransaction))
                        {
                            command.Parameters.AddWithValue("@type", type);

                            var adapter = new SqlDataAdapter(command);
                            var dataset = new DataSet();

                            adapter.Fill(dataset);

                            byte[] publicKey = null;
                            byte[] privateKey = null;

                            if (dataset.Tables[0].Rows.Count == 1)
                            {
                                publicKey = (byte[])dataset.Tables[0].Rows[0][0];
                                privateKey = (byte[])dataset.Tables[0].Rows[0][1];
                            }

                            if (dataset.Tables[0].Rows.Count == 0)
                            {
                                this.CreateKeys(type, connection, sqlTransaction, out publicKey, out privateKey);
                            }

                            if (publicKey != null && privateKey != null)
                            {
                                this.RWlock.EnterWriteLock();
                                if (!this.publicKeys.ContainsKey(type))
                                {
                                    this.publicKeys.Add(type, publicKey);
                                    this.privateKeys.Add(type, privateKey);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        return;
                    }
                    finally
                    {
                        this.RWlock.ExitWriteLock();
                        connection.Close();
                    }
                }
            }
        }

        private static void GenerateKeys(out byte[] publicKey, out byte[] privateKey)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(KeySize);

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
