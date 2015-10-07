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
    interface IDatabaseConnection
    {
        void StartTransaction(IsolationLevel isolationLevel);

        void CommitTransaction();

        void AbortTransaction();

        DbCommand CreateCommand(string query);

        DbDataAdapter CreateDataAdapter(DbCommand command);

        DbParameter CreateParameter(string parameter, object value);
    }
}
