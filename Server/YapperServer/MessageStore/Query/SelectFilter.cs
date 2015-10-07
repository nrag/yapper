using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageStore.Database;

namespace MessageStore.Query
{
    internal class SelectFilter : QueryFilter
    {
        private static string SelectStringFormat = "SELECT {0} FROM {1} WHERE {2} {3}";

        private static string OrderByStringFormat = "ORDER BY {0}";

        private readonly ITable table;

        private readonly QueryFilter query;

        private readonly List<IColumn> columns;

        private readonly List<SortCriteria> sorts;

        private readonly bool isCount;

        private string selectString;

        public SelectFilter(
            ITable table, 
            QueryFilter query, 
            List<SortCriteria> sorts) 
            : this(table, query, sorts, false)
        {
        }

        public SelectFilter(
            ITable table,
            QueryFilter query,
            List<SortCriteria> sorts,
            bool isCount)
            : this(table, query, sorts, isCount, null)
        {
        }

        public SelectFilter(
            ITable table, 
            QueryFilter query, 
            List<SortCriteria> sorts,
            bool isCount,
            List<IColumn> columns)
        {
            this.table = table;
            this.query = query;
            this.sorts = sorts;
            this.isCount = isCount;
            this.columns = columns;
            
            if (this.columns == null)
            {
                this.columns = new List<IColumn>();
                foreach (IColumn col in this.table.Columns)
                {
                    if (col.ColumnLocation == ColumnLocation.Database)
                    {
                        this.columns.Add(col);
                    }
                }
            }
        }

        public override string QueryString
        {
            get 
            {
                if (string.IsNullOrEmpty(selectString))
                {
                    this.CreateSelectString();
                }

                return selectString;
            }
        }

        public List<IColumn> Columns
        {
            get
            {
                return this.columns;
            }
        }

        public bool IsCount
        {
            get
            {
                return this.isCount;
            }
        }

        private void CreateSelectString()
        {
            // If this is not a count operator, return something like SELECT * FROM
            // Otherwise return COUNT(*) from
            if (!this.IsCount)
            {
                this.selectString = string.Format(SelectFilter.SelectStringFormat, this.BuildColumnString(), table.Name, query.QueryString, this.BuildOrderbyString());
            }
            else
            {
                this.selectString = string.Format(SelectFilter.SelectStringFormat, "COUNT(*)", table.Name, query.QueryString, this.BuildOrderbyString());
            }
        }

        private string BuildOrderbyString()
        {
            if (this.sorts == null || this.sorts.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder sortString = new StringBuilder("ORDER BY ");
            for (int i = 0; i < this.sorts.Count; i++)
            {
                string sortOrderString = this.sorts[i].SortOrder == SortOrder.Ascending ? "ASC" : "DESC";
                sortString.AppendFormat("{0} {1},", this.sorts[i].Column.Name, sortOrderString);
            }

            sortString.Remove(sortString.Length - 1, 1);

            return sortString.ToString();
        }

        private string BuildColumnString()
        {
            StringBuilder columnString = new StringBuilder();
            for (int i = 0; i < this.columns.Count; i++)
            {
                columnString.Append(this.columns[i].Name);
                columnString.Append(',');
            }

            columnString.Remove(columnString.Length - 1, 1);

            return columnString.ToString();
        }
    }
}
