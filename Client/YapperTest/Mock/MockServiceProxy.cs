using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using YapperChat.Models;
using YapperChat.ServiceProxy;
using System.Runtime.Serialization.Json;
using System.IO;

namespace YapperTest.Mock
{
    class MockServiceProxy : IServiceProxy
    {
        /// <summary>
        /// JSON string containing conversations to be returned on GetAllConversations call
        /// </summary>
        private string jsonConversations;

        public MockServiceProxy(string jsonConversations)
        {
            this.jsonConversations = jsonConversations;
        }

        public void GetAllConversations(Action<List<YapperChat.Models.ConversationModel>> callback)
        {
            List<ConversationModel> result = null;

            if (!string.IsNullOrEmpty(this.jsonConversations))
            {
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(List<ConversationModel>));
                result = (List<ConversationModel>)jsonSerializer.ReadObject(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(this.jsonConversations)));
            }

            System.Threading.Tasks.Task.Factory.StartNew(() =>
                {
                    callback(result);
                });
        }

        public void GetMessagesForConversation(string conversationId, Action<List<YapperChat.Models.MessageModel>> callback)
        {
            throw new NotImplementedException();
        }

        public void SendNewMessage(string conversationId, string message, Action<YapperChat.Models.MessageModel> callback)
        {
            throw new NotImplementedException();
        }

        public void SendNewConversation(string recipients, string message, Action<YapperChat.Common.Tuple<YapperChat.Models.ConversationModel, YapperChat.Models.MessageModel>> callback)
        {
            throw new NotImplementedException();
        }

        public void GetRegisteredPhoneNumbers(List<string> userPhoneNumbers, Action<List<YapperChat.Models.ContactGroup<YapperChat.Models.UserModel>>> callback)
        {
            throw new NotImplementedException();
        }

        public void RegisterNewUser(string phoneNumber, string name, Action<YapperChat.Models.UserModel> callback)
        {
            throw new NotImplementedException();
        }

        public void SubscribeToPush(string url, Action<bool> callback)
        {
            throw new NotImplementedException();
        }
    }
}
