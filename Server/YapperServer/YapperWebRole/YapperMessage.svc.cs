using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;
using System.Web;
//using NeuroSpeech.Imap;

using MessageStore;
using MessageLayer = MessageStore.MessageLayer;
using DAL = DataAccessLayer;
//using ConsoleApplication1;
using System.Net.Mail;

namespace YapperWebRole
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "EncryptedMessage" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select EncryptedMessage.svc or EncryptedMessage.svc.cs at the Solution Explorer and start debugging.
    public class YapperMessage : IYapperMessage
    {
        /// <summary>
        /// Validates the one-time password for the user
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <param name="cookie"></param>
        /// <returns></returns>
        public DAL.UserCookie ValidateUser(string phoneNumber, int oneTimePassword, string deviceId, string random)
        {
            string normalizedPhone = DAL.PhoneNumberUtils.ValidatePhoneNumber(phoneNumber);

            DAL.User existingUser = DAL.UserService.Instance.GetUserFromPhone(normalizedPhone);
            if (existingUser == null)
            {
                throw new Exception("User not registered");
            }

            Authenticator.TOTP oneTimePasswordValidator = new Authenticator.TOTP(existingUser.Secret, 30, 6);
            if (!oneTimePasswordValidator.Verify(oneTimePassword))
            {
                throw new Exception("Invalid one-time password");
            }

            DAL.UserService.Instance.UpdateDeviceId(existingUser, deviceId);

            DAL.UserCookie cookie = new DAL.UserCookie(existingUser, deviceId);

            return cookie;
        }

        /// <summary>
        /// Adds a new user.
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public DAL.UserCookie AddUser(string phoneNumber, string name, string deviceId)
        {
            string normalizedPhone = DAL.PhoneNumberUtils.ValidatePhoneNumber(phoneNumber);

            if (string.IsNullOrEmpty(normalizedPhone))
            {
                return null;
            }

            DAL.User user = DAL.UserService.Instance.GetUserFromPhone(normalizedPhone);
            if (user == null)
            {
                user = DAL.UserService.Register(normalizedPhone, name);
            }
            else
            {
                DAL.UserService.Instance.UpdateName(user, name);
            }

            DateTime syncTime = DateTime.Now - new TimeSpan(7, 0, 0, 0);
            DataAccessLayer.UserService.UpdateUserLastSyncTime((int)user.Id, syncTime);

            if (DAL.PhoneNumberUtils.IsDebugPhoneNumber(normalizedPhone))
            {
                DAL.UserService.Instance.UpdateDeviceId(user, deviceId);

                DAL.UserCookie cookie = new DAL.UserCookie(user, deviceId);

                return cookie;
            }

            this.SendSmsWithConfirmationCode(user);

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="phone"></param>
        /// <returns></returns>
        public DAL.User[] GetUsers(string[] phoneNumbers)
        {
            DAL.User sender = this.GetAuthenticatedUser();

            if (sender == null)
            {
                return null;
            }

            List<string> normalizedPhoneNumbers = new List<string>();
            for (int i = 0; i < phoneNumbers.Length; i++)
            {
                string normalizedPhone = DAL.PhoneNumberUtils.ValidatePhoneNumber(phoneNumbers[i]);
                if (!string.IsNullOrEmpty(normalizedPhone))
                {
                    normalizedPhoneNumbers.Add(normalizedPhone);
                }
            }

            if (normalizedPhoneNumbers.Count == 0)
            {
                return new List<DAL.User>().ToArray();
            }

            List<DAL.User> users = DAL.UserService.Instance.GetUsersFromPhones(normalizedPhoneNumbers);
            return users.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="phone"></param>
        /// <returns></returns>
        public DAL.User[] GetUsersWithoutAuth(string deviceid, string[] phoneNumbers)
        {
            List<string> normalizedPhoneNumbers = new List<string>();
            for (int i = 0; i < phoneNumbers.Length; i++)
            {
                string normalizedPhone = DAL.PhoneNumberUtils.ValidatePhoneNumber(phoneNumbers[i]);
                if (!string.IsNullOrEmpty(normalizedPhone))
                {
                    normalizedPhoneNumbers.Add(normalizedPhone);
                }
            }

            if (normalizedPhoneNumbers.Count == 0)
            {
                return new List<DAL.User>().ToArray();
            }

            List<DAL.User> users = DAL.UserService.Instance.GetUsersFromPhones(normalizedPhoneNumbers);
            return users.ToArray();
        }

        public DAL.Group CreateGroup(DAL.Group newGroup)
        {
            DAL.User sender = this.GetAuthenticatedUser();

            if (newGroup.Members == null)
            {
                throw new Exception("Invalid member list");
            }

            Dictionary<int, DAL.User> uniqueUsers = new Dictionary<int, DAL.User>();

            for (int i = 0; i < newGroup.Members.Count; i++)
            {
                if (string.Compare(DAL.UserService.Instance.GetUserFromId(newGroup.Members[i].Id).PhoneNumber, newGroup.Members[i].PhoneNumber, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    throw new Exception("Invalid user specified");
                }

                uniqueUsers.Add(newGroup.Members[i].Id, newGroup.Members[i]);
            }

            if (!uniqueUsers.ContainsKey(sender.Id))
            {
                uniqueUsers.Add(sender.Id, sender);
            }

            DAL.Group createdGroup = DAL.Group.CreateGroup(sender, newGroup.Name, uniqueUsers.Values.ToList());

            MessageLayer.Message newGroupMessage = new MessageLayer.Message() { SenderId = sender.Id, RecipientId = createdGroup.Id, MessageFlags = MessageLayer.MessageFlags.GroupCreatedMessage, TextMessage = sender.Name + " has created the group" };

            MessageStore.MessageStore.Instance.SaveMessage(sender, newGroupMessage);

            if (newGroupMessage.RecipientId != sender.Id)
            {
                this.SendPushNotifications(
                    DAL.Subscription.GetSubscriptions(newGroupMessage.RecipientId, sender),
                    newGroupMessage.MessageId,
                    newGroupMessage.ConversationId,
                    sender,
                    newGroupMessage.PostDateTimeUtcTicks,
                    newGroupMessage.TextMessage,
                    createdGroup,
                    newGroupMessage.IsEncrypted);
            }
            return createdGroup;
        }

        public bool AddUserToGroup(int groupId, string userPhone)
        {
            DAL.User sender = this.GetAuthenticatedUser();

            DAL.Group group = (DAL.Group)DAL.UserService.Instance.GetUserFromId(groupId);

            DAL.User member = DAL.UserService.Instance.GetUserFromPhone(userPhone);

            bool success = DAL.Group.AddGroupMember(sender, group, member);

            if (!success)
            {
                return false;
            }

            // Read the group again
            group = (DAL.Group)DAL.UserService.Instance.GetUserFromId(groupId);

            MessageLayer.Message newGroupMessage = new MessageLayer.Message() { SenderId = sender.Id, RecipientId = group.Id, MessageFlags = MessageLayer.MessageFlags.GroupJoinMessage, TextMessage = sender.Name + " has added " + member.Name + " to the group" };

            MessageStore.MessageStore.Instance.SaveMessage(sender, newGroupMessage);

            if (newGroupMessage.RecipientId != sender.Id)
            {
                this.SendPushNotifications(
                    DAL.Subscription.GetSubscriptions(newGroupMessage.RecipientId, sender),
                    newGroupMessage.MessageId,
                    newGroupMessage.ConversationId,
                    sender,
                    newGroupMessage.PostDateTimeUtcTicks,
                    newGroupMessage.TextMessage,
                    group,
                    newGroupMessage.IsEncrypted);
            }

            return success;
        }

        public bool RemoveUserFromGroup(int groupId, string userPhone)
        {
            DAL.User sender = this.GetAuthenticatedUser();

            DAL.Group group = (DAL.Group)DAL.UserService.Instance.GetUserFromId(groupId);
            DAL.User member = DAL.UserService.Instance.GetUserFromPhone(userPhone);

            bool success = DAL.Group.RemoveGroupMember(sender, group, member);

            if (!success)
            {
                return false;
            }

            MessageLayer.Message newGroupMessage = new MessageLayer.Message() { Sender = sender, Recipient = group, MessageFlags = MessageLayer.MessageFlags.GroupLeaveMessage, TextMessage = sender.Name + " has removed " + member.Name + " from the group" };

            MessageStore.MessageStore.Instance.SaveMessage(sender, newGroupMessage);

            if (newGroupMessage.RecipientId != sender.Id)
            {
                this.SendPushNotifications(
                    DAL.Subscription.GetSubscriptions(newGroupMessage.RecipientId, sender),
                    newGroupMessage.MessageId,
                    newGroupMessage.ConversationId,
                    sender,
                    newGroupMessage.PostDateTimeUtcTicks,
                    newGroupMessage.TextMessage,
                    group,
                    newGroupMessage.IsEncrypted);
            }

            return success;
        }

        public List<DAL.Group> GetGroups()
        {
            DAL.User sender = this.GetAuthenticatedUser();

            if (sender == null)
            {
                return null;
            }

            return DAL.Group.GetGroupsForUser(sender.Id);
        }


        /// <summary>
        /// API to send a new message
        /// </summary>
        /// <param name="recipientPhoneNumber"></param>
        /// <param name="message"></param>
        /// <param name="UtcPostTime"></param>
        /// <returns></returns>
        public MessageLayer.Message SendMessage(MessageLayer.Message message)
        {
            Stopwatch watch = new Stopwatch();
            long authTime = 0;
            long messageCreationTime = 0;
            long pushNotification = 0;

            watch.Start();
            DAL.User sender = this.GetAuthenticatedUser();

            authTime = watch.ElapsedMilliseconds;

            if (sender == null)
            {
                throw new Exception("User not authenticated");
            }

            try
            {
                if (sender.Id != message.SenderId)
                {
                    throw new Exception("Invalid senderId");
                }

                if (sender.Id == message.RecipientId)
                {
                    throw new Exception("Invalid recipientId");
                }

                DAL.User recipient = DAL.UserService.Instance.GetUserFromId(message.RecipientId);
                if (recipient == null)
                {
                    throw new Exception("recipient not found");
                }

                if (!recipient.CanSendMessage(sender))
                {
                    throw new Exception("Cannot send message to recipient");
                }

                message.Recipient = recipient;

                MessageStore.MessageStore.Instance.SaveMessage(sender, message);
                messageCreationTime = watch.ElapsedMilliseconds - authTime;

                if (message != null)
                {
                    if (message.RecipientId != sender.Id)
                    {
                        this.SendPushNotifications(
                            DAL.Subscription.GetSubscriptions(message.RecipientId, sender),
                            message.MessageId,
                            message.ConversationId,
                            sender,
                            message.PostDateTimeUtcTicks,
                            message.TextMessage,
                            message.Recipient.UserType == DAL.UserType.Group ? (DAL.Group)message.Recipient : null,
                            message.IsEncrypted);

                        pushNotification = watch.ElapsedMilliseconds - messageCreationTime - authTime;
                    }
                }

                return message;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        //public MessageLayer.Message[] GetImapMessages()
        //{
        //    List<MessageLayer.Message> messages = new List<MessageLayer.Message>();
        //    ImapClient imapClient = new ImapClient();
        //    imapClient.Connect("imap.gmail.com", 993, true);
        //    imapClient.Login("yappchat@gmail.com", "t%nt0wn!");

        //    ImapResponseList imapFolderResponse = imapClient.RetriveFolders();
        //    ImapResponseSelect imapSelectionResponse = imapClient.SelectMailbox("INBOX");
        //    ImapRequestSearch imapSearchRequest = new ImapRequestSearch();
        //    imapSearchRequest.UID = false;
        //    imapSearchRequest.Since = new DateTime(2013, 10, 18);
        //    ImapResponseSearch rs = imapClient.Search(imapSearchRequest);

        //    foreach (ulong id in rs.IDs)
        //    {
        //        ImapRequestMessageHeader imapRequestMessageHeader = new ImapRequestMessageHeader();
        //        imapRequestMessageHeader.Id = id;
        //        ImapResponseMessageHeader imapResponseMessageHeader = imapClient.Request<ImapResponseMessageHeader>(imapRequestMessageHeader);

        //        ImapRequestMessageBody imapRequestMessageBody = new ImapRequestMessageBody();
        //        imapRequestMessageBody.Id = id;
        //        ImapResponseMessageBody imapReponseMessageBody = imapClient.Request<ImapResponseMessageBody>(imapRequestMessageBody);

        //        int index = imapReponseMessageBody.Response.IndexOf("Delivered");
        //        if (index == -1)
        //        {
        //            index = imapReponseMessageBody.Response.IndexOf("MIME-Version: 1.0");
        //        }

        //        string messageBodyText = imapReponseMessageBody.Response.Substring(index, imapReponseMessageBody.Response.Length - index);
        //        StringReader stringReaderMessageBodyText = new StringReader(messageBodyText);
        //        MailMessage mailMessage = MailMessageMimeParser.ParseMessage(stringReaderMessageBodyText);

        //        MessageLayer.Message message = new MessageLayer.Message();
        //        message.TextMessage = string.Empty;

        //        foreach (AttachmentBase alternativemessage in mailMessage.AlternateViews)
        //        {
        //            if (alternativemessage.ContentType.MediaType == "text/html" && alternativemessage.ContentStream.Length < 4000)
        //            {
        //                byte[] bodybuffer = new byte[alternativemessage.ContentStream.Length+1];
        //                alternativemessage.ContentStream.Read(bodybuffer, 0, (int)alternativemessage.ContentStream.Length);
        //                message.TextMessage = System.Text.Encoding.ASCII.GetString(bodybuffer);
        //                break;
        //            }
                    
        //            if (alternativemessage.ContentType.MediaType == "text/plain" && alternativemessage.ContentStream.Length < 4000)
        //            {
        //                byte[] bodybuffer = new byte[alternativemessage.ContentStream.Length + 1];
        //                alternativemessage.ContentStream.Read(bodybuffer, 0, (int)alternativemessage.ContentStream.Length);
        //                message.TextMessage = System.Text.Encoding.ASCII.GetString(bodybuffer);
        //            }
        //        }

        //        if (!string.IsNullOrEmpty(mailMessage.Body) && mailMessage.BodyEncoding != null && string.IsNullOrEmpty(message.TextMessage))
        //        {
        //            Byte[] bodytext = Encoding.Convert(mailMessage.BodyEncoding, Encoding.Unicode, Encoding.UTF8.GetBytes(mailMessage.Body));
        //            message.TextMessage = System.Text.Encoding.Unicode.GetString(bodytext);
        //        }
                
        //        message.Sender = new DAL.User(mailMessage.From.Address.GetHashCode(), "1234567", mailMessage.From.DisplayName, "foo");
        //        message.SenderId = mailMessage.From.Address.GetHashCode();
        //        message.Recipient = new DAL.User(mailMessage.To[0].Address.GetHashCode(), "1234567", mailMessage.To[0].DisplayName, "foo");
        //        message.RecipientId = mailMessage.To[0].Address.GetHashCode();
        //        message.MessageId = new Guid();
        //        message.PostDateTime = imapResponseMessageHeader.Messages[0].InternalDate;
        //        messages.Add(message);
        //    }

        //    return messages.ToArray();
        //}

        public MessageLayer.Message[] GetAllMessages()
        {
            DAL.User user = this.GetAuthenticatedUser();

            if (user == null)
            {
                throw new Exception("User not authenticated");
            }

            List<MessageLayer.Message> messages = MessageStore.MessageStore.Instance.GetAllMessagesForUser(user, null);

            if (messages == null)
            {
                return null;
            }
            else
            {
                return messages.ToArray();
            }
        }

        /// <summary>
        /// API to get the list of messages in a conversation
        /// </summary>
        /// <param name="LastSyncDateTime"></param>
        /// <returns></returns>
        public MessageLayer.Message[] GetAllMessagesSincLastSync(DateTime LastSyncDateTime)
        {
            DAL.User user = this.GetAuthenticatedUser();

            if (user == null)
            {
                throw new Exception("User not authenticated");
            }

            List<MessageLayer.Message> messages = MessageStore.MessageStore.Instance.GetAllMessagesForUser(user, LastSyncDateTime);

            if (messages == null)
            {
                return null;
            }
            else
            {
                return messages.ToArray();
            }
        }

        /// <summary>
        /// API to get the list of messages from ids
        /// </summary>
        /// <param name="Array of message ids"></param>
        /// <returns>Array of messages.</returns>
        public MessageLayer.Message GetFullMessageForMessageId(Guid conversationId, Guid messageId)
        {
            DAL.User user = this.GetAuthenticatedUser();

            if (user == null)
            {
                throw new Exception("User not authenticated");
            }

            MessageLayer.Message message = MessageStore.MessageStore.Instance.GetMessage(conversationId, messageId);
            
            return message;
        }

        /// <summary>
        /// API to subscribe to push notifications
        /// </summary>
        /// <param name="deviceid"></param>
        /// <param name="subscriptionType"></param>
        /// <param name="subscriptionUrl"></param>
        public void SubscribeToNotification(string deviceid, DAL.SubscriptionType subscriptionType, string subscriptionUrl)
        {
            DAL.User user = this.GetAuthenticatedUser();

            if (user == null)
            {
                throw new Exception("User not authenticated");
            }

            DAL.Subscription subscription = DAL.Subscription.UpdateOrInsertSubscription(deviceid, subscriptionType, subscriptionUrl, user.Id);
        }

        /// <summary>
        /// API to unsubscribe from notifications
        /// </summary>
        /// <param name="deviceid"></param>
        /// <param name="subscriptionType"></param>
        public void UnSubscribeToNotification(string deviceid, DAL.SubscriptionType subscriptionType)
        {
            DAL.User user = this.GetAuthenticatedUser();

            if (user == null)
            {
                throw new Exception("User not authenticated");
            }

            DAL.Subscription.Unsubscribe(deviceid, subscriptionType, user.Id);
        }

        public void UploadException(DAL.ExceptionDetails exception)
        {
            DAL.User user = this.GetAuthenticatedUser();

            if (user == null)
            {
                throw new Exception("User not authenticated");
            }

            exception.Save(user);
        }

        /// <summary>
        /// Reads the auth cookie and validates the user and returns the User object
        /// </summary>
        /// <returns></returns>
        private DAL.User GetAuthenticatedUser()
        {
            HttpRequestMessageProperty reqMsg = OperationContext.Current.IncomingMessageProperties["httpRequest"] as HttpRequestMessageProperty;
            DAL.UserCookie cookie = DAL.UserCookie.Parse((string)reqMsg.Headers[Globals.AuthTokenCookie]);

            if (cookie == null || !cookie.IsValid())
            {
                return null;
            }

            return cookie.User;
        }

        /// <summary>
        /// Method to post push notifications for windows phones
        /// </summary>
        /// <param name="urls"></param>
        /// <param name="messageId"></param>
        /// <param name="conversationId"></param>
        /// <param name="senderId"></param>
        /// <param name="postDate"></param>
        /// <param name="message"></param>
        /// <param name="sender"></param>
        private void SendPushNotifications(
            List<Tuple<DAL.User, string>> urls, 
            Guid messageId,
            Guid conversationId, 
            DAL.User sender,
            long postDate,
            string message, 
            DAL.Group group,
            bool isEncrypted)
        {
            PushNotificationQueue.Instance.SchedulePush(
                new PushNotification(
                    urls, 
                    messageId, 
                    conversationId, 
                    sender, 
                    postDate, 
                    isEncrypted ? "Message content is encrypted! Please open Yapper to view this message." : message, 
                    group));
        }

        /// <summary>
        /// Update last sync time from the client.
        /// </summary>
        /// <param name="LastSyncDateTime"></param>
        public void  SetLastSyncDateTime(long clientticks)
        {
            DAL.User user = this.GetAuthenticatedUser();

            DateTime LastSyncDateTime = new DateTime(clientticks);

            if (user == null)
            {
                throw new Exception("User not authenticated");
            }

            DAL.UserService.UpdateUserLastSyncTime(user.Id, LastSyncDateTime);
        }

        /// <summary>
        /// Update public key of the user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="publicKey"></param>
        public void UpdateUserPublicKey(DAL.User user)
        {
            DAL.User serverSideUser = DAL.UserService.Instance.GetUserFromId(user.Id);

            DAL.UserService.UpdateUserPublicKey(serverSideUser, user.PublicKey);
        }

        private void SendSmsWithConfirmationCode(DAL.User user)
        {
//            Task.Factory.StartNew(() =>
            {
                Authenticator.TOTP oneTimePasswordValidator = new Authenticator.TOTP(user.Secret, 30, 6);

                int code = oneTimePasswordValidator.Now();

                Authenticator.SmsSender sender = new Authenticator.SmsSender();

                sender.SendSMS(user.PhoneNumber, code);
            }//);
        }
    }
}
