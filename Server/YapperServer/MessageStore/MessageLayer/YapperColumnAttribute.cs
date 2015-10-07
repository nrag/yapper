using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageStore.Database;

namespace MessageStore.MessageLayer
{
    class YapperColumnAttribute : Attribute
    {
        private DatabaseColumn column = new DatabaseColumn();

        public string Name
        {
            get
            {
                return this.column.Name;
            }

            set
            {
                this.column.Name = value;
            }
        }

        public bool Identity
        {
            get
            {
                return this.column.Identity;
            }

            set
            {
                this.column.Identity = value;
            }
        }

        public Type Type
        {
            get
            {
                return this.column.Type;
            }

            set
            {
                this.column.Type = value;
            }
        }

        public ColumnLocation ColumnLocation
        {
            get
            {
                return this.column.ColumnLocation;
            }

            set
            {
                this.column.ColumnLocation = value;
            }
        }

        public bool IsBlobName
        {
            get
            {
                return this.column.IsBlobName;
            }

            set
            {
                this.column.IsBlobName = value;
            }
        }

        public bool IsRequired
        {
            get
            {
                return this.column.IsRequired;
            }

            set
            {
                this.column.IsRequired = value;
            }
        }
    }
}
