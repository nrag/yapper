using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessageStore.Database;

namespace MessageStoreTest
{
    class TestDatabaseConnectionFactory : IDatabaseConnectionFactory
    {
        public IDatabaseConnection CreateDatabaseConnection()
        {
            return new TestDatabaseConnection();
        }
    }
}
