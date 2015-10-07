using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageStore.Query
{
    internal class OrFilter : QueryFilter
    {
        private List<QueryFilter> subFilters = new List<QueryFilter>();

        public OrFilter(QueryFilter filter1, QueryFilter filter2)
        {
            if (filter1 == null || filter2 == null)
            {
                throw new ArgumentNullException("One of the filters is null");
            }

            this.subFilters.Add(filter1);
            this.subFilters.Add(filter2);
        }

        public OrFilter(List<QueryFilter> subFilters)
        {
            if (subFilters == null | subFilters.Count == 0)
            {
                throw new ArgumentException("One of the filters is null");
            }

            this.subFilters.AddRange(subFilters);
        }

        public override string QueryString
        {
            get
            {
                return this.BuildQueryString();
            }
        }

        private string BuildQueryString()
        {
            StringBuilder andFilter = new StringBuilder();
            for (int i = 0; i < this.subFilters.Count; i++)
            {
                if (i != 0)
                {
                    andFilter.Append(" OR ");
                }

                andFilter.AppendFormat("({0})", this.subFilters[i].QueryString);
            }

            return andFilter.ToString();
        }
    }
}
