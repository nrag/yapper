using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessageStore.Database;

namespace MessageStoreTest
{
    class TestDatabaseConnection : IDatabaseConnection, IDisposable
    {
        private static string SqlConnectionString = "Data Source=|DataDirectory|\\MessageStoreTestDatabase#1.sdf;Persist Security Info=False";

        private SqlCeConnection sqlConnection;

        private SqlCeTransaction transaction;

        public TestDatabaseConnection()
        {
            this.sqlConnection = new SqlCeConnection(TestDatabaseConnection.SqlConnectionString);
            this.sqlConnection.Open();
        }

        public void StartTransaction(IsolationLevel isolationLevel)
        {
            this.transaction = this.sqlConnection.BeginTransaction();
        }

        public void CommitTransaction()
        {
            if (this.transaction != null)
            {
                this.transaction.Commit();
            }
        }

        public void AbortTransaction()
        {
            if (this.transaction != null)
            {
                this.transaction.Rollback();
            }
        }

        public DbCommand CreateCommand(string query)
        {
            return new SqlCeCommand(query, this.sqlConnection, this.transaction);
        }

        public DbDataAdapter CreateDataAdapter(DbCommand command)
        {
            return new SqlCeDataAdapter((SqlCeCommand)command);
        }

        public DbParameter CreateParameter(string parameter, object value)
        {
            return new SqlCeParameter(parameter, value);
        }

        public void Dispose()
        {
            this.sqlConnection.Close();
        }
    }
}
