using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessageStore.Query;

namespace MessageStore.Database
{
    interface ITable
    {
        string Name
        {
            get;
        }

        List<IColumn> Columns
        {
            get;
        }

        IColumn Identity
        {
            get;
        }

        IColumn BlobValueColumn
        {
            get;
        }

        IColumn BlobNameColumn
        {
            get;
        }
        
        IColumn BlobContainerColumn
        {
            get;
        }

        ITableRow InsertRow(ITableRow row);

        bool DeleteRow(ITableRow row);

        List<ITableRow> QueryRows(QueryFilter query);

        int GetRowCount(QueryFilter query);
    }
}
