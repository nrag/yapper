using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MessageStore
{
    interface IBlobStore
    {
        void SaveBlob(string containerName, string blobName, byte[] blobValue);

        byte[] GetBlob(string containerName, string blobName);
    }
}
