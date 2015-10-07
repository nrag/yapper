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
    class DatabaseQueryOperator : IDisposable
    {
        private IDatabaseConnection connection;

        private ITable table;

        private QueryFilter query;

        public DatabaseQueryOperator(
            ITable table,
            QueryFilter query)
        {
            this.table = table;
            this.query = query;
            if (!(query is SelectFilter))
            {
                throw new Exception("Invalid query filter. It should be a select filter");
            }

            this.connection = DatabaseConnectionFactory.Instance.CreateDatabaseConnection();
        }

        internal List<ITableRow> Execute()
        {
            this.connection.StartTransaction(System.Data.IsolationLevel.ReadCommitted);

            DbCommand command = this.connection.CreateCommand(this.query.QueryString);

            var adapter = this.connection.CreateDataAdapter(command);
            var dataset = new DataSet();

            adapter.Fill(dataset);
            List<ITableRow> rows = new List<ITableRow>();
            for (int i = 0; i < dataset.Tables[0].Rows.Count; i++)
            {
                TableRow row = new TableRow();
                Dictionary<IColumn, object> columnValues = new Dictionary<IColumn, object>();
                row.ColumnValues = columnValues;

                object[] values = dataset.Tables[0].Rows[i].ItemArray;
                for (int j = 0; j < values.Count(); j++)
                {
                    object value = values[j] == DBNull.Value ? null : values[j];

                    if (((SelectFilter)query).Columns[j].Type.Equals(typeof(DateTime)))
                    {
                        value = DateTime.SpecifyKind((DateTime)values[j], DateTimeKind.Utc);
                    }

                    if (((SelectFilter)query).Columns[j].Type.Equals(typeof(DateTime?)) &&
                        value != null)
                    {
                        value = DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc);
                    }

                    columnValues.Add(((SelectFilter)query).Columns[j], value);
                }

                if (this.table.BlobNameColumn != null)
                {
                    object blobName = columnValues[this.table.BlobNameColumn];
                    object blobContainer = columnValues[this.table.BlobContainerColumn];
                    IBlobStore blobStore = BlobStoreFactory.Instance.GetBlobStore();
                    byte[] blobValue = blobStore.GetBlob(blobContainer.ToString(), blobName.ToString());
                    columnValues.Add(this.table.BlobValueColumn, blobValue);
                }

                rows.Add(row);
            }

            return rows;
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
