using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageStore.Database
{
    class DatabaseConnection : IDatabaseConnection,IDisposable
    {
        private static string SqlConnectionString = ""; // Use your connection string

        private SqlConnection sqlConnection;

        private SqlTransaction transaction;
        
        public DatabaseConnection()
        {
            this.sqlConnection = new SqlConnection(DatabaseConnection.SqlConnectionString);
            this.sqlConnection.Open();
        }

        public void StartTransaction(IsolationLevel isolationLevel)
        {
            this.transaction = this.sqlConnection.BeginTransaction(isolationLevel);
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
            return new SqlCommand(query, this.sqlConnection, this.transaction);
        }


        public DbDataAdapter CreateDataAdapter(DbCommand command)
        {
            return new SqlDataAdapter((SqlCommand)command);
        }

        public DbParameter CreateParameter(string parameter, object value)
        {
            return new SqlParameter(parameter, value);
        }

        public void Dispose()
        {
            this.sqlConnection.Close();
        }
    }
}
