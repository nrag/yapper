using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MessageStore;
using MessageStore.MessageLayer;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.Common;

namespace MessageStoreTest
{
    [TestClass]
    public class MessageStoreTests
    {
        [TestInitialize]
        public void Initialize()
        {
             MessageStore.Database.DatabaseConnectionFactory.SetTestHook(new TestDatabaseConnectionFactory());
        }

        [TestCleanup]
        public void Cleanup()
        {
            MessageStore.Database.IDatabaseConnection connection = MessageStore.Database.DatabaseConnectionFactory.Instance.CreateDatabaseConnection();
            connection.StartTransaction(System.Data.IsolationLevel.ReadCommitted);
            DbCommand command = connection.CreateCommand("DELETE FROM MESSAGETable");
            command.ExecuteNonQuery();
            connection.CommitTransaction();
        }

        [TestMethod]
        public void TestMessageSave()
        {
            Message message = new Message();
            message.SenderId = 1;
            message.Sender = new DataAccessLayer.User() { Id = message.SenderId, Name = "TestSender" + DateTime.UtcNow.Ticks.ToString(), PhoneNumber = "+1 425-111-2222" };
            message.RecipientId = 7;
            message.Recipient = new DataAccessLayer.User() { Id = message.RecipientId, Name = "TestRecipient" + DateTime.UtcNow.Ticks.ToString(), PhoneNumber = "+1 425-111-2222" };
            message.ConversationId = GetGuid(1, 7);
            message.MessageId = Guid.NewGuid();
            message.LastUpdateTimeUtcTicks = DateTime.UtcNow.Ticks;
            message.PostDateTimeUtcTicks = DateTime.UtcNow.Ticks;
            message.TextMessage = "This is a test Msg" + DateTime.UtcNow.Ticks.ToString();
            message.MessageBlobName = Guid.NewGuid();
            message.MessageFlags = 0;

            MessageStore.MessageStore.Instance.SaveMessage(message.Sender, message);
        }

        [TestMethod]
        public void TestQueryRows()
        {

            Message message1 = new Message();
            message1.SenderId = 1;
            message1.Sender = new DataAccessLayer.User() { Id = message1.SenderId, Name = "TestSender" + DateTime.UtcNow.Ticks.ToString(), PhoneNumber = "+1 425-111-2222" }; 
            message1.RecipientId = 7;
            message1.Recipient = new DataAccessLayer.User() { Id = message1.RecipientId, Name = "TestRecipient" + DateTime.UtcNow.Ticks.ToString(), PhoneNumber = "+1 425-111-2222" }; message1.ConversationId = GetGuid(1, 7);
            message1.MessageId = Guid.NewGuid();
            message1.LastUpdateTimeUtcTicks = DateTime.UtcNow.Ticks;
            message1.PostDateTimeUtcTicks = DateTime.UtcNow.Ticks;
            message1.TextMessage = "This is a test Msg" + DateTime.UtcNow.Ticks.ToString();
            message1.MessageBlobName = Guid.NewGuid();
            message1.MessageFlags = 0;

            Message message2 = new Message();
            message2.SenderId = 1;
            message2.RecipientId = 8;
            message2.Sender = new DataAccessLayer.User() { Id = message2.SenderId, Name = "TestSender" + DateTime.UtcNow.Ticks.ToString(), PhoneNumber = "+1 425-111-2222" };
            message2.Recipient = new DataAccessLayer.User() { Id = message2.RecipientId, Name = "TestRecipient" + DateTime.UtcNow.Ticks.ToString(), PhoneNumber = "+1 425-111-2222" }; message1.ConversationId = GetGuid(1, 7); 
            message2.ConversationId = GetGuid(1, 7);
            message2.MessageId = Guid.NewGuid();
            message2.LastUpdateTimeUtcTicks = DateTime.UtcNow.Ticks;
            message2.PostDateTimeUtcTicks = DateTime.UtcNow.Ticks;
            message2.TextMessage = "This is a test Msg" + DateTime.UtcNow.Ticks.ToString();
            message2.MessageBlobName = Guid.NewGuid();
            message2.MessageFlags = 0;

            Message message3 = new Message();
            message3.SenderId = 1;
            message3.RecipientId = 8;
            message3.Sender = new DataAccessLayer.User() { Id = message3.SenderId, Name = "TestSender" + DateTime.UtcNow.Ticks.ToString(), PhoneNumber = "+1 425-111-2222" };
            message3.Recipient = new DataAccessLayer.User() { Id = message3.RecipientId, Name = "TestRecipient" + DateTime.UtcNow.Ticks.ToString(), PhoneNumber = "+1 425-111-2222" }; message1.ConversationId = GetGuid(1, 7);
            message3.ConversationId = GetGuid(1, 7);
            message3.MessageId = Guid.NewGuid();
            message3.LastUpdateTimeUtcTicks = DateTime.UtcNow.Ticks;
            message3.PostDateTimeUtcTicks = DateTime.UtcNow.Ticks;
            message3.TextMessage = "This is a test Msg" + DateTime.UtcNow.Ticks.ToString();
            message3.MessageBlobName = Guid.NewGuid();
            message3.MessageFlags = 0;

            MessageStore.MessageStore.Instance.SaveMessage(message1.Sender, message1);
            MessageStore.MessageStore.Instance.SaveMessage(message2.Sender, message2);
            MessageStore.MessageStore.Instance.SaveMessage(message3.Sender, message3);

            List<Message> queriedMessages = MessageStore.MessageStore.Instance.GetAllMessagesForUser(new DataAccessLayer.User(1, "+1 425-111-9999", "Test User", null), null); 
        }

        [TestMethod]
        public void TestUnseenCount()
        {

            Message message1 = new Message();
            message1.SenderId = 1;
            message1.RecipientId = 7;
            message1.ConversationId = GetGuid(1, 7);
            message1.MessageId = Guid.NewGuid();
            message1.LastUpdateTimeUtcTicks = DateTime.UtcNow.Ticks;
            message1.PostDateTimeUtcTicks = DateTime.UtcNow.Ticks;
            message1.TextMessage = "This is a test Msg" + DateTime.UtcNow.Ticks.ToString();
            message1.MessageBlobName = Guid.NewGuid();
            message1.MessageFlags = 0;

            Message message2 = new Message();
            message2.SenderId = 1;
            message2.RecipientId = 8;
            message2.ConversationId = GetGuid(1, 7);
            message2.MessageId = Guid.NewGuid();
            message2.LastUpdateTimeUtcTicks = DateTime.UtcNow.Ticks;
            message2.PostDateTimeUtcTicks = DateTime.UtcNow.Ticks;
            message2.TextMessage = "This is a test Msg" + DateTime.UtcNow.Ticks.ToString();
            message2.MessageBlobName = Guid.NewGuid();
            message2.MessageFlags = 0;

            Message message3 = new Message();
            message3.SenderId = 1;
            message3.RecipientId = 8;
            message3.ConversationId = GetGuid(1, 7);
            message3.MessageId = Guid.NewGuid();
            message3.LastUpdateTimeUtcTicks = DateTime.UtcNow.Ticks;
            message3.PostDateTimeUtcTicks = DateTime.UtcNow.Ticks;
            message3.TextMessage = "This is a test Msg" + DateTime.UtcNow.Ticks.ToString();
            message3.MessageBlobName = Guid.NewGuid();
            message3.MessageFlags = 0;

            MessageStore.MessageStore.Instance.SaveMessage(message1.Sender, message1);
            MessageStore.MessageStore.Instance.SaveMessage(message2.Sender, message2);
            MessageStore.MessageStore.Instance.SaveMessage(message3.Sender, message3);

            int count = MessageStore.MessageStore.Instance.GetUnseenMessageCount(new DataAccessLayer.User(1, "+1 425-111-9999", "Test User", null), DateTime.UtcNow - new TimeSpan(7, 0, 0, 0));
        }

        private static Guid GetGuid(long n1, long n2)
        {
            Int16 firstPart = (Int16)((n1 & 0xffff00000000L) >> 32);
            Int16 secondPart = (Int16)((n1 & 0x0000ffff0000L) >> 16);
            byte thirdPart = (byte)((n1 & 0xff00L) >> 16);
            byte fourthPart = (byte)(n1 & 0xffL);;
            byte byte1 = (byte)((n2 & 0xff0000000000L) >> 40);
            byte byte2 = (byte)((n2 & 0x00ff00000000L) >> 32);
            byte byte3 = (byte)((n2 & 0x0000ff000000L) >> 24);
            byte byte4 = (byte)((n2 & 0x000000ff0000L) >> 16);
            byte byte5 = (byte)((n2 & 0x00000000ff00L) >> 8);
            byte byte6 = (byte)((n2 & 0x0000000000ffL));

            return new Guid(0,firstPart, secondPart, thirdPart, fourthPart, byte1, byte2, byte3, byte4, byte5, byte6);
        }

        private void ClearTestDatabase()
        {
            throw new NotImplementedException();
        }
    }
}
