using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessageStore.Database;

namespace MessageStore.MessageLayer
{
    class MessageTableRow :TableRow
    {
        public MessageTableRow(Dictionary<IColumn, object> columnValues)
        {
            this.ColumnValues = columnValues;
        }

        public override string BlobContainer
        {
            get 
            {
                IColumn conversationIdColumn = MessageTable.GetColumnFromName("ConversationId");
                object value;
                if (!this.ColumnValues.TryGetValue(conversationIdColumn, out value))
                {
                    return string.Empty;
                }

                return value.ToString();
            }
        }
    }
}
