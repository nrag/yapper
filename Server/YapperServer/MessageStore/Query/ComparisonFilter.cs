using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageStore.Query
{
    internal enum ComparisonOperator
    {
        Equal,

        NotEqual,

        Greater,

        GreaterThanEqual,

        Lesser,

        LesserThanEqual,
    }

    class ComparisonFilter : QueryFilter
    {
        private static string ComparisonFormatString = "{0} {1} '{2}'";

        private IColumn column;

        private object value;

        private ComparisonOperator comparisonOperator;

        public ComparisonFilter(IColumn column, object value, ComparisonOperator comparisonOperator)
        {
            this.column = column;
            this.value = value;
            this.comparisonOperator = comparisonOperator;
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
            return string.Format(ComparisonFilter.ComparisonFormatString, column.Name, ComparisonFilter.ComparisonString(this.comparisonOperator), this.value.ToString());
        }

        private static string ComparisonString(ComparisonOperator comparisonOperator)
        {
            switch (comparisonOperator)
            {
                case ComparisonOperator.Equal:
                    return "=";
                case ComparisonOperator.Greater:
                    return ">";
                case ComparisonOperator.GreaterThanEqual:
                    return ">=";
                case ComparisonOperator.Lesser:
                    return "<";
                case ComparisonOperator.LesserThanEqual:
                    return "<=";
                case ComparisonOperator.NotEqual:
                    return "!=";
                default:
                    throw new Exception("Invalid comparison operator");
            }
        }
    }
}
