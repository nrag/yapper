using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using YapperChat.Models;
using YapperChat.ServiceProxy;
using System.Runtime.Serialization.Json;
using System.IO;
using System.ComponentModel;

namespace YapperUnitTest.Mock
{
    class MockServiceProxy : IServiceProxy
    {
        public List<ConversationModel> Conversations
        {
            get;
            set;
        }

        public List<MessageModel> Messages
        {
            get;
            set;
        }

        public bool SimulateSendMessageFailure
        {
            get;
            set;
        }

        public List<UserModel> Users
        {
            get;
            set;
        }

        public void GetAllConversations(Action<List<YapperChat.Models.ConversationModel>> callback)
        {
            callback(this.Conversations);
        }

        public void GetMessagesForConversation(long conversationId, Action<List<YapperChat.Models.MessageModel>> callback)
        {
            callback(this.Messages);
        }

        public void SendNewMessage(UserModel sender, UserModel recipient, long conversationId, string message, Action<YapperChat.Models.MessageModel> callback)
        {
            MessageModel messageModel = null;

            if (!this.SimulateSendMessageFailure)
            {
                messageModel = new MessageModel();

                messageModel.ConversationId = conversationId;
                messageModel.TextMessage = message;
                messageModel.Sender = sender;
                messageModel.Recipient = recipient;
                messageModel.MessageId = (new Random((int)DateTime.Now.Ticks).Next(1000, 2000));
                messageModel.PostDateTime = DateTime.UtcNow;
            }

            callback(messageModel);
        }

        public void SendNewConversation(UserModel sender, UserModel recipient, string message, Action<YapperChat.Common.Tuple<YapperChat.Models.ConversationModel, YapperChat.Models.MessageModel>> callback)
        {
            throw new NotImplementedException();
        }

        public void GetRegisteredPhoneNumbers(List<string> userPhoneNumbers, Action<List<YapperChat.Models.ContactGroup<YapperChat.Models.UserModel>>> callback)
        {
            List<ContactGroup<UserModel>> tempItems = null;
            if (this.Users != null)
            {
                tempItems = new List<ContactGroup<UserModel>>();
                var groups = new Dictionary<char, ContactGroup<UserModel>>();

                foreach (var user in this.Users)
                {
                    char firstLetter = char.ToLower(user.Name[0]);

                    // show # for numbers
                    if (firstLetter >= '0' && firstLetter <= '9')
                    {
                        firstLetter = '#';
                    }

                    // create group for letter if it doesn't exist
                    if (!groups.ContainsKey(firstLetter))
                    {
                        var group = new ContactGroup<UserModel>(firstLetter);
                        tempItems.Add(group);
                        groups[firstLetter] = group;
                    }

                    // create a contact for item and add it to the relevant 
                    groups[firstLetter].Add(user);
                }
            }

            callback(tempItems);
        }

        public void RegisterNewUser(string phoneNumber, string name, Action<YapperChat.Models.UserModel> callback)
        {
            throw new NotImplementedException();
        }

        public void SubscribeToPush(string url, Action<bool> callback)
        {
            throw new NotImplementedException();
        }


        public void UnSubscribeToPush()
        {
            throw new NotImplementedException();
        }


        public void ValidateConfirmationCode(string code, Action<UserCookieModel> callback)
        {
            throw new NotImplementedException();
        }


        public void GetAllMessages(Action<List<MessageModel>> callback)
        {
            throw new NotImplementedException();
        }
    }
}