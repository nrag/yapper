using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageStore
{
    class BlobStoreFactory : IBlobStoreFactory
    {
        private static IBlobStoreFactory _instance = new BlobStoreFactory();

        public static IBlobStoreFactory Instance
        {
            get
            {
                return BlobStoreFactory._instance;
            }
        }

        public IBlobStore GetBlobStore()
        {
            return new AzureBlobStore();
        }
    }
}
