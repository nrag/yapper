using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;

namespace MessageStore.Database
{
    class DatabaseInsertOperator : IDisposable
    {
        private IDatabaseConnection connection;

        private ITable table;

        private static string InsertQueryFormat = "INSERT into {0} ({1}) VALUES ({2}); {3}";

        private static string ScopeIdentity = "Select Scope_Identity()";

        public DatabaseInsertOperator(ITable table)
        {
            this.table = table;
            this.connection = DatabaseConnectionFactory.Instance.CreateDatabaseConnection();
        }

        internal object Execute(ITableRow row)
        {
            this.connection.StartTransaction(System.Data.IsolationLevel.ReadCommitted);

            // Save the blob first
            if (table.BlobValueColumn != null && table.BlobNameColumn != null)
            {
                object guidObject = null;
                Guid blobName = Guid.Empty;
                if (!row.ColumnValues.TryGetValue(table.BlobNameColumn, out guidObject))
                {
                    blobName = Guid.NewGuid();
                    row.ColumnValues.Add(table.BlobNameColumn, blobName);
                }
                else
                {
                    blobName = (Guid)guidObject;
                }

                object blobValue = null;
                if (!row.ColumnValues.TryGetValue(table.BlobValueColumn, out blobValue))
                {
                    row.ColumnValues[table.BlobNameColumn] = Guid.Empty;
                    blobName = Guid.Empty;
                }

                IBlobStore blobStore = BlobStoreFactory.Instance.GetBlobStore();
                blobStore.SaveBlob(row.BlobContainer, blobName.ToString(), (byte[])blobValue);
            }

            // Save the row in the table
            DbCommand command = this.BuildInsertCommand(row);

            if (table.Identity != null)
            {
                SqlParameter outParameter = new SqlParameter(string.Format("@{0}", table.Identity.Name), DatabaseInsertOperator.GetSqlType(table.Identity.Type));
                outParameter.Direction = ParameterDirection.Output;
                command.Parameters.Add(outParameter);
            }

            object outParam = command.ExecuteScalar();
            this.connection.CommitTransaction();

            return null;
        }

        private static SqlDbType GetSqlType(Type type)
        {
            if (type == typeof(int))
            {
                return SqlDbType.Int;
            }

            if (type == typeof(long))
            {
                return SqlDbType.BigInt;
            }

            if (type == typeof(short))
            {
                return SqlDbType.SmallInt;
            }

            if (type == typeof(byte))
            {
                return SqlDbType.TinyInt;
            }

            if (type == typeof(Decimal))
            {
                return SqlDbType.Decimal;
            }

            throw new ArgumentException("Invalid type passed as an identity column");
        }

        private DbCommand BuildInsertCommand(ITableRow row)
        {
            StringBuilder columns = new StringBuilder();
            StringBuilder columnParameters = new StringBuilder();

            foreach (IColumn column in row.ColumnValues.Keys)
            {
                if (column.ColumnLocation == ColumnLocation.Database)
                {
                    columns.AppendFormat("{0},", column.Name);
                    columnParameters.AppendFormat("@{0},", column.Name);
                }
            }

            columns.Remove(columns.Length - 1, 1);
            columnParameters.Remove(columnParameters.Length - 1, 1);

            string query = string.Format(
                DatabaseInsertOperator.InsertQueryFormat, 
                this.table.Name, 
                columns.ToString(), 
                columnParameters.ToString(),
                this.table.Identity == null ? string.Empty : DatabaseInsertOperator.ScopeIdentity);

            DbCommand command = this.connection.CreateCommand(query);

            // Add the parameters to the SQL command
            foreach (IColumn column in row.ColumnValues.Keys)
            {
                if (column.ColumnLocation == ColumnLocation.Database)
                {
                    command.Parameters.Add(this.connection.CreateParameter(string.Format("@{0}", column.Name), row.ColumnValues[column]));
                }
            }

            return command;
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
