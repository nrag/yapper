using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MessageStore.MessageLayer
{
    class YapperTableAttribute : Attribute
    {
        public string Name
        {
            get;
            set;
        }
    }
}
