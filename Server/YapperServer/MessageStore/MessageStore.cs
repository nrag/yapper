using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessageStore.MessageLayer;
using MessageStore.Query;

namespace MessageStore
{
    public class MessageStore : IMessageStore
    {
        private static IMessageStore _instance = new MessageStore();

        public static IMessageStore Instance
        {
            get
            {
                return MessageStore._instance;
            }
        }

        /// <summary>
        /// Save a message in the database
        /// </summary>
        /// <param name="message"></param>
        public void SaveMessage(DataAccessLayer.User sender, Message message)
        {
            message.Validate(sender);
            ITableRow tableRow = message.ConvertToTableRow();
            MessageTable.Instance.InsertRow(tableRow);
        }

        public Message GetMessage(Guid conversationId, Guid messageId)
        {
            // Create a filter to say Select * from MessageTable where ConversationId = @conversationId AND MessageId = @messageId
            QueryFilter filter = new SelectFilter(
                MessageTable.Instance,
                new AndFilter(
                    new ComparisonFilter(MessageTable.ConversationIdColumn, conversationId, ComparisonOperator.Equal),
                    new ComparisonFilter(MessageTable.MessageIdColumn, messageId, ComparisonOperator.Equal)),
                null);
            
            List<ITableRow> rows = MessageTable.Instance.QueryRows(filter);
            if (rows.Count != 1)
            {
                return null;
            }

            return DataContractToTableRowConverter.ConvertToMessage(MessageTable.Instance, rows[0]);
        }

        public List<Message> GetAllMessagesForUser(DataAccessLayer.User user, DateTime? syncTime)
        {
            DateTime now = DateTime.UtcNow;
            DateTime nonNullSyncTime = syncTime ?? DateTime.UtcNow - new TimeSpan(7, 0, 0, 0);
            List<SortCriteria> sorts = new List<SortCriteria>();
            sorts.Add(new SortCriteria() { Column= MessageTable.LastUpdateTimeUtcTicksColumn, SortOrder = SortOrder.Descending });

            // Create a filter to say Select * from MessageTable where (Sender = @userId OR Recipient = @userId OR Recipient = @groupThatUserIsAMemberOf) AND LastUpdateTime > @syncTime
            QueryFilter filter = new SelectFilter(
                MessageTable.Instance,
                new AndFilter(
                    MessageStore.CreateUserFilter(user),
                    new ComparisonFilter(MessageTable.LastUpdateTimeUtcTicksColumn, nonNullSyncTime.Ticks, ComparisonOperator.Greater)),
                sorts);

            List<ITableRow> rows = MessageTable.Instance.QueryRows(filter);
            List<Message> messages = new List<Message>();
            if (rows != null)
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    messages.Add(DataContractToTableRowConverter.ConvertToMessage(MessageTable.Instance, rows[i]));
                }
            }

            DataAccessLayer.UserService.UpdateUserLastSyncTime(user.Id, now);

            return messages;
        }

        public List<Message> GetConversationMessages(Guid conversationId)
        {
            // Create a filter to say Select * from MessageTable where ConversationId = @conversationId AND MessageId = @messageId
            QueryFilter filter = new SelectFilter(
                MessageTable.Instance,
                new ComparisonFilter(MessageTable.ConversationIdColumn, conversationId, ComparisonOperator.Equal),
                null);

            List<ITableRow> rows = MessageTable.Instance.QueryRows(filter);
            List<Message> messages = new List<Message>();
            if (rows != null)
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    messages.Add(DataContractToTableRowConverter.ConvertToMessage(MessageTable.Instance, rows[i]));
                }
            }

            return messages;
        }

        public int GetUnseenMessageCount(DataAccessLayer.User user, DateTime lastSeenTime)
        {
            // Create a filter to say Select * from MessageTable where (Sender = @userId OR Recipient = @userId OR Recipient = @groupThatUserIsAMemberOf) AND LastUpdateTime > @syncTime
            QueryFilter filter = new SelectFilter(
                MessageTable.Instance,
                new AndFilter(
                    MessageStore.CreateUserFilterForCount(user),
                    new ComparisonFilter(MessageTable.LastUpdateTimeUtcTicksColumn, lastSeenTime.Ticks, ComparisonOperator.Greater)),
                null,
                true/*count*/);

            return MessageTable.Instance.GetRowCount(filter);
        }

        internal static QueryFilter CreateUserFilter(DataAccessLayer.User user)
        {
            QueryFilter senderFilter = new ComparisonFilter(MessageTable.SenderIdColumn, user.Id, ComparisonOperator.Equal);
            QueryFilter recipientFilter = new ComparisonFilter(MessageTable.RecipientIdColumn, user.Id, ComparisonOperator.Equal);
            if (user.GroupIds == null || user.GroupIds.Count == 0)
            {
                return new OrFilter(senderFilter, recipientFilter);
            }

            List<QueryFilter> filters = new List<QueryFilter>();
            filters.Add(senderFilter);
            filters.Add(recipientFilter);

            for (int i = 0; i < user.GroupIds.Count; i++)
            {
                filters.Add(new ComparisonFilter(MessageTable.RecipientIdColumn, user.GroupIds[i], ComparisonOperator.Equal));
            }

            return new OrFilter(filters);
        }

        internal static QueryFilter CreateUserFilterForCount(DataAccessLayer.User user)
        {
            QueryFilter recipientFilter = new ComparisonFilter(MessageTable.RecipientIdColumn, user.Id, ComparisonOperator.Equal);
            if (user.GroupIds == null || user.GroupIds.Count == 0)
            {
                return recipientFilter;
            }

            List<QueryFilter> filters = new List<QueryFilter>();
            filters.Add(recipientFilter);

            for (int i = 0; i < user.GroupIds.Count; i++)
            {
                filters.Add(new ComparisonFilter(MessageTable.RecipientIdColumn, user.GroupIds[i], ComparisonOperator.Equal));
            }

            return new OrFilter(filters);
        }
    }
}