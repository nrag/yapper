using System;
using System.Collections.Generic;
using YapperChat.Models;
using YapperChat.Common;

namespace YapperChat.ServiceProxy
{
    public interface IServiceProxy
    {
        /*
        void GetAllConversations(Action<List<ConversationModel>> callback);
        
        void GetMessagesForConversation(long conversationId, Action<List<MessageModel>> callback);
        */
        void GetAllMessages(Action<List<MessageModel>> callback);

        void GetAllMessagesSinceLastSync(Action<List<MessageModel>> callback, DateTime LastSyncDateTime);

        void GetFullMessageFromMessageId(Action<MessageModel> callback, Guid conversationId, Guid MessageId);

        void SendNewMessage(MessageModel message, Action<MessageModel> callback);

        void GetRegisteredPhoneNumbers(List<string> userPhoneNumbers, Action<List<UserModel>> callback);

        void RegisterNewUser(string phoneNumber, string name, Action<UserCookieModel> callback);

        void ValidateConfirmationCode(string code, Action<UserCookieModel> callback);

        void SubscribeToPush(string url, Action<bool> callback);

        void UnSubscribeToPush();

        void UploadExceptions();

        void GetGroups(Action<List<GroupModel>> callback);

        void CreateGroup(string name, List<UserModel> members, Action<GroupModel> callback);

        void AddGroupMember(GroupModel group, UserModel user, Action<GroupModel, UserModel> callback);

        void RemoveGroupMember(GroupModel group, UserModel user, Action<GroupModel, UserModel> callback);

        void PostPollResponse(Guid messageId, string pollResponse, Action<MessageModel> callback);

        void SetLastSyncDateTime(DateTime LastSyncDateTime);

        void UpdateUserPublicKey(UserModel userWithPK, Action<bool> callback);
    }
}
