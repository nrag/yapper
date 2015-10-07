using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MessageStore.Query;

namespace MessageStore.Database
{
    abstract class DatabaseTable : ITable
    {
        public abstract string Name
        {
            get;
        }

        public abstract List<IColumn> Columns
        {
            get;
        }

        public abstract IColumn Identity
        {
            get;
        }

        public abstract IColumn BlobNameColumn
        {
            get;
        }

        public abstract IColumn BlobValueColumn
        {
            get;
        }

        public abstract IColumn BlobContainerColumn
        {
            get;
        }

        public ITableRow InsertRow(ITableRow row)
        {
            // Validate that we are attempting to insert only the columns that
            // exist in the table
            foreach (IColumn column in row.ColumnValues.Keys)
            {
                if (!this.Columns.Contains(column))
                {
                    throw new Exception("Attempting to insert a column that doesn't exist in the table");
                }
            }

            foreach (IColumn column in this.Columns)
            {
                if (column.IsRequired && !row.ColumnValues.ContainsKey(column))
                {
                    throw new Exception("Value for required column not present");
                }
            }

            if (this.Identity != null && row.ColumnValues.ContainsKey(this.Identity))
            {
                throw new Exception("Value should not be set for identity column");
            }

            using (DatabaseInsertOperator insertOp = new DatabaseInsertOperator(this))
            {
                object identity = insertOp.Execute(row);

                if (this.Identity != null)
                {
                    row.ColumnValues.Add(this.Identity, identity);
                }
            }

            return row;
        }

        public bool DeleteRow(ITableRow row)
        {
            return false;
        }

        public List<ITableRow> QueryRows(QueryFilter query)
        {
            using (DatabaseQueryOperator queryOperator = new DatabaseQueryOperator(this, query))
            {
                return queryOperator.Execute();
            }
        }

        public int GetRowCount(QueryFilter query)
        {
            using (DatabaseCountOperator queryOperator = new DatabaseCountOperator(this, query))
            {
                return queryOperator.Execute();
            }
        }
    }
}
