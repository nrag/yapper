using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessageStore.Query;

namespace MessageStore.Database
{
    class DatabaseCountOperator : IDisposable
    {
        private IDatabaseConnection connection;

        private ITable table;

        private QueryFilter query;

        public DatabaseCountOperator(
            ITable table,
            QueryFilter query)
        {
            this.table = table;
            this.query = query;
            if (!(query is SelectFilter) || !((SelectFilter)query).IsCount)
            {
                throw new Exception("Invalid query filter. It should be a select filter and should have IsCount set to true");
            }

            this.connection = DatabaseConnectionFactory.Instance.CreateDatabaseConnection();
        }

        internal int Execute()
        {
            this.connection.StartTransaction(System.Data.IsolationLevel.ReadCommitted);

            DbCommand command = this.connection.CreateCommand(this.query.QueryString);

            return (int)command.ExecuteScalar();
        }

        public void Dispose()
        {
            IDisposable disposableConnection = (IDisposable)this.connection;
            if (disposableConnection != null)
            {
                disposableConnection.Dispose();
            }
        }
    }
}
