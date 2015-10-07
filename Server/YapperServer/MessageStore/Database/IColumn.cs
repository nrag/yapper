using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageStore.Database;

namespace MessageStore
{
    interface IColumn
    {
        string Name
        {
            get;
        }

        Type Type
        {
            get;
        }

        int Length
        {
            get;
        }

        ColumnLocation ColumnLocation
        {
            get;
        }

        bool IsBlobName
        {
            get;
        }

        bool IsRequired
        {
            get;
        }

        bool Identity
        {
            get;
        }
    }
}
