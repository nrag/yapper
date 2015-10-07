using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MessageStore.Query
{
    internal abstract class QueryFilter
    {
        public abstract string QueryString
        {
            get;
        }
    }
}
