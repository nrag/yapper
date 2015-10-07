using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessageStore.MessageLayer;
using DAL = DataAccessLayer;

namespace MessageStore
{
    public interface IMessageStore
    {
        void SaveMessage(DAL.User sender, Message message);

        Message GetMessage(Guid conversationId, Guid messageId);

        List<Message> GetAllMessagesForUser(DAL.User user, DateTime? syncTime);

        List<Message> GetConversationMessages(Guid conversationId);

        int GetUnseenMessageCount(DAL.User user, DateTime lastSeenTime);
    }
}
