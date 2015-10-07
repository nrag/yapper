using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace MessageStore
{
    class AzureBlobStore : IBlobStore
    {
        private static string AzureAccount = "DefaultEndpointsProtocol=https;AccountName=messagebloblocation;AccountKey=HhbctKNiUJI1sr9iKinlVTwfQAa6kPw1xWiFWy2eUNOtMoNZWBh5w3CmK43/uu8R8S3fXEDDfFV0kLzCfVtI/g==";

        public void SaveBlob(string containerName, string blobName, byte[] blobValue)
        {
            try
            {
                MemoryStream memStream = new MemoryStream(blobValue);
                memStream.Seek(0, SeekOrigin.Begin);
                CloudStorageAccount csa = CloudStorageAccount.Parse(AzureBlobStore.AzureAccount);
                CloudBlobClient cloudBlobClient = csa.CreateCloudBlobClient();
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);
                cloudBlobContainer.CreateIfNotExists();
                ICloudBlob blobReference = cloudBlobContainer.GetBlockBlobReference(blobName.ToString());

                blobReference.Metadata.Add(new KeyValuePair<string, string>("blobName", blobName.ToString()));
                blobReference.UploadFromStream(memStream);
                memStream.Close();
            }
            catch (Exception)
            {
                return;
            }
        }

        public byte[] GetBlob(string containerName, string blobName)
        {
            try
            {
                using (MemoryStream memStream = new MemoryStream())
                {
                    CloudStorageAccount csa = CloudStorageAccount.Parse(AzureBlobStore.AzureAccount);
                    CloudBlobClient cloudBlobClient = csa.CreateCloudBlobClient();
                    CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName.ToString());
                    ICloudBlob blobreference = cloudBlobContainer.GetBlockBlobReference(blobName.ToString());
                    blobreference.DownloadToStream(memStream);
                    memStream.Seek(0, SeekOrigin.Begin);
                    return memStream.ToArray();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
