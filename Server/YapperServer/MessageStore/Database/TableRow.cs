using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageStore.Database
{
    class TableRow : ITableRow
    {
        public Dictionary<IColumn, object> ColumnValues
        {
            get;
            set;
        }

        public virtual string BlobContainer
        {
            get;
            set;
        }
    }
}
