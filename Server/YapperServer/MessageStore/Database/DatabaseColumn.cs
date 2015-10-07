using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageStore.Database
{
    enum ColumnLocation
    {
        Database,

        BlobStore
    }

    class DatabaseColumn : IColumn
    {
        public string Name
        {
            get;
            set;
        }

        public bool Identity
        {
            get;
            set;
        }

        public Type Type
        {
            get;
            set;
        }

        public ColumnLocation ColumnLocation
        {
            get;
            set;
        }

        public int Length
        {
            get;
            set;
        }

        public bool IsBlobName
        {
            get;
            set;
        }

        public bool IsRequired
        {
            get;
            set;
        }

        public override bool Equals(object obj)
        {
            DatabaseColumn otherColumn = obj as DatabaseColumn;

            if (obj == null)
            {
                return false;
            }

            return (0 == string.Compare(this.Name, otherColumn.Name, StringComparison.InvariantCulture));
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }
    }
}
