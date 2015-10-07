using System;
using System.Collections.Generic;

namespace MessageStore
{
    interface ITableRow
    {
        /// <summary>
        /// The values for the columns
        /// After saving, it'll contain the values for identity columns
        /// </summary>
        Dictionary<IColumn, object> ColumnValues
        {
            get;
        }

        string BlobContainer
        {
            get;
        }
    }
}
