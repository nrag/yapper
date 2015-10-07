using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MessageStore.Query
{
    internal enum SortOrder
    {
        Ascending,

        Descending
    }

    internal struct SortCriteria
    {
        public IColumn Column
        {
            get;
            set;
        }

        public SortOrder SortOrder
        {
            get;
            set;
        }
    }
}
