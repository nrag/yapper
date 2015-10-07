using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageStore.Database
{
    class DatabaseConnectionFactory : IDatabaseConnectionFactory
    {
        private static IDatabaseConnectionFactory _instance = new DatabaseConnectionFactory();

        public static IDatabaseConnectionFactory Instance
        {
            get
            {
                return DatabaseConnectionFactory._instance;
            }
        }

        public static void SetTestHook(IDatabaseConnectionFactory connectionFactory)
        {
            DatabaseConnectionFactory._instance = connectionFactory;
        }

        public IDatabaseConnection CreateDatabaseConnection()
        {
            return new DatabaseConnection();
        }
    }
}
