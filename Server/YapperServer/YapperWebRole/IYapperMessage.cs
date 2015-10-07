using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using MessageStore.MessageLayer;
using DAL = DataAccessLayer;

namespace YapperWebRole
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IEncryptedMessage" in both code and config file together.
    [ServiceContract]
    public interface IYapperMessage
    {
        /// <summary>
        /// Validate the user based on the key sent through the text message.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [OperationContract]
        [WebInvoke(
            Method = "GET",
            UriTemplate = "user/validate?phoneNumber={phoneNumber}&otp={oneTimePassword}&deviceId={deviceId}&random={random}",
            ResponseFormat = WebMessageFormat.Json)]
        DAL.UserCookie ValidateUser(string phoneNumber, int oneTimePassword, string deviceId, string random);

        /// <summary>
        /// Add a new user or update the user's private key.
        /// </summary>
        /// <param name="publicKey"></param>
        /// <returns></returns>
        [OperationContract]
        [WebInvoke(
            Method = "POST",
            UriTemplate = "user?phoneNumber={phoneNumber}&name={name}&deviceId={deviceId}",
            ResponseFormat=WebMessageFormat.Json)]
        DAL.UserCookie AddUser(string phoneNumber, string name, string deviceId);

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            UriTemplate = "group",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json)]
        DAL.Group CreateGroup(DAL.Group newGroup);

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            UriTemplate = "group/user/add?groupId={groupId}&user={user}",
            ResponseFormat = WebMessageFormat.Json)]
        bool AddUserToGroup(int groupId, string user);

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            UriTemplate = "group/user/remove?groupId={groupId}&user={user}",
            ResponseFormat = WebMessageFormat.Json)]
        bool RemoveUserFromGroup(int groupId, string user);

        [OperationContract]
        [WebInvoke(
            Method = "GET",
            UriTemplate = "group",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        List<DAL.Group> GetGroups();

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            UriTemplate = "contacts",
            ResponseFormat = WebMessageFormat.Json,
            RequestFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        DAL.User[] GetUsers(string[] phoneNumbers);

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            UriTemplate = "message",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json)]
        Message SendMessage(Message message);

        [OperationContract]
        [WebInvoke(
            Method = "GET",
            UriTemplate = "messages?conversationid={conversationId}&messageid={messageid}",
            ResponseFormat = WebMessageFormat.Json)]
        Message GetFullMessageForMessageId(Guid conversationId, Guid messageId);

        [OperationContract]
        [WebInvoke(
            Method = "GET",
            UriTemplate = "message?lastsync={lastsyncdatetime}",
            ResponseFormat = WebMessageFormat.Json)]
        Message[] GetAllMessagesSincLastSync(DateTime LastSyncDateTime);

        [OperationContract]
        [WebInvoke(
            Method = "GET",
            UriTemplate = "message",
            ResponseFormat = WebMessageFormat.Json)]
        Message[] GetAllMessages();

        //[OperationContract]
        //[WebInvoke(
        //    Method = "GET",
        //    UriTemplate = "imapmessage",
        //    ResponseFormat = WebMessageFormat.Json)]
        //Message[] GetImapMessages();

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            UriTemplate = "notification/subscribe?deviceid={deviceid}&subscriptionType={subscriptionType}&subscriptionUrl={url}",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json)]
        void SubscribeToNotification(string deviceid, DAL.SubscriptionType subscriptionType, string url);

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            UriTemplate = "notification/unsubscribe?deviceid={deviceid}&subscriptionType={subscriptionType}",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json)]
        void UnSubscribeToNotification(string deviceid, DAL.SubscriptionType subscriptionType);

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            UriTemplate = "exception",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json)]
        void UploadException(DAL.ExceptionDetails exception);

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            UriTemplate = "user/lastsyncdatetime?lastsync={lastsyncdatetime}",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json)]
        void SetLastSyncDateTime(long LastSyncDateTime);

        [OperationContract]
        [WebInvoke(
            Method = "POST",
            UriTemplate = "user/userpk",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json)]
        void UpdateUserPublicKey(DAL.User user);
    }
}
