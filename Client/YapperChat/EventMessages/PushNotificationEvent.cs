using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using GalaSoft.MvvmLight.Messaging;
using YapperChat.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Linq;

namespace YapperChat.EventMessages
{
    public class PushNotificationEvent : MessageBase
    {
        private string message;

        private Guid conversationId;

        private string senderName;

        private Guid messageId;

        private DateTime messagePostDate;

        private int senderId;

        public PushNotificationEvent(
            object sender, 
            Guid messageId,
            Guid conversationId,
            string message,
            long messagePostDate,
            string senderName,
            int senderId,
            int groupId,
            string groupName) : base(sender)
        {
            this.message = message;
            this.messageId = messageId;
            this.conversationId = conversationId;
            this.senderName = senderName;
            this.messagePostDate = new DateTime(messagePostDate, DateTimeKind.Utc);
            this.senderId = senderId;
            this.GroupId = groupId;
            this.GroupName = groupName;
            this.IsGroup = this.GroupId == -1;
        }

        public string Message
        {
            get
            {
                return this.message;
            }
        }

        public Guid MessageId
        {
            get
            {
                return this.messageId;
            }
        }

        public Guid ConversationId
        {
            get
            {
                return this.conversationId;
            }
        }

        public DateTime MessagePostDate
        {
            get
            {
                return this.messagePostDate;
            }
        }

        public string SenderName
        {
            get
            {
                return this.senderName;
            }
        }

        public int SenderId
        {
            get
            {
                return this.senderId;
            }
        }

        public bool IsGroup
        {
            get;
            set;
        }

        public int GroupId
        {
            get;
            set;
        }

        public string GroupName
        {
            get;
            set;
        }

        public ConversationModel CreateConversation(UserModel currentUser)
        {
            ConversationModel conversation = new ConversationModel();
            conversation.ConversationId = this.ConversationId;
            conversation.LastPostUtcTime = this.MessagePostDate;
            conversation.LastPostPreview = this.Message;
            conversation.ConversationParticipants = new EntitySet<UserModel>();
            conversation.ConversationParticipants.Add(this.GetSender());
            conversation.ConversationParticipants.Add(currentUser);

            return conversation;
        }

        private UserModel GetSender()
        {
            UserModel senderUser = new UserModel();

            senderUser.Name = this.senderName;
            senderUser.Id = this.senderId;
            senderUser.PhoneNumber = string.Empty;

            return senderUser;
        }
    }
}