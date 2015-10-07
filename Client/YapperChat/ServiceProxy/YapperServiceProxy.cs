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
using YapperChat.Models;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Collections.Generic;
using YapperChat.Common;
using Microsoft.Phone.Info;
using SharpGIS;
using System.Threading;
using System.ServiceModel;
using System.Diagnostics;

namespace YapperChat.ServiceProxy
{
    public enum SubscriptionType
    {
        WindowsPhoneToast,

        WindowsPhoneTile
    }

    /// <summary>
    /// This class encapsulates all the webservice calls made to the yapper service
    /// </summary>
    public class YapperServiceProxy : IServiceProxy
    {
        /// <summary>
        /// Prefix to the service
        /// </summary>

        //private static string ServicePrefix = "http://cryptk.cloudapp.net/yapperMessage.svc/api/";
        //private static string ServicePrefix = "http://127.0.0.1:80/yapperMessage.svc/api/";
        private static string ServicePrefix = "http://yapper.cloudapp.net/yapperMessage.svc/api/";
        //private static string ServicePrefix = "http://169.254.80.80/YapperWebRole/YapperMessage.svc/api/";

        /// <summary>
        /// Relative URL to retrieve all conversations
        /// </summary>
        /// private static string AllConversationsRelativeUrl = "{0}conversation?random={1}";

        /// <summary>
        /// Relative URL to retrieve all conversations
        /// </summary>
        private static string AllMessagesRelativeUrl = "{0}message";

        /// <summary>
        /// Relative URL to retrieve all conversations
        /// </summary>
        private static string GetAllMessagesSinceLastSyncRelativeUrl = "{0}message?lastsync={1}";

        private static string SetLastSyncDataTimeRelativeUrl = "{0}user/lastsyncdatetime?lastsync={1}";

        private static string UpdateUserPublicKeyRelativeUrl = "{0}user/userpk";

        /// <summary>
        /// Relative URL to retrieve message from messageid
        /// </summary>
        private static string GetFullMessageFromMessageIdRelativeUrl = "{0}messages?conversationId={1}&messageid={2}";

        /// <summary>
        /// Url to get the list of conversation messages
        /// The random part is to avoid hitting the cache.
        /// </summary>
        /// private static string ConversationsMessagesRelativeUrl = "{0}message?convid={1}&random={2}";

        /// <summary>
        /// Relative URL to send new message
        /// </summary>
        private static string SendNewMessageRelativeUrl = "{0}message";

        /// <summary>
        /// Relative URL to register a new user
        /// </summary>
        private static string NewUserRegistrationRelativeUrl = "{0}user?phoneNumber={1}&name={2}&deviceId={3}";

        /// <summary>
        /// Url to validate the confirmation code
        /// </summary>
        private static string ValidateConfirmationCodeRelativeUrl = "{0}user/validate?phoneNumber={1}&otp={2}&deviceId={3}&random={4}";

        /// <summary>
        /// Relative URL to retrieve registered contacts
        /// </summary>
        private static string RegisteredContactsRelativeUrl = "{0}contacts";

        /// <summary>
        /// Relative URL to subscribe to push notifications
        /// </summary>
        private static string SubscribeToPushRelativeUrl = "{0}notification/subscribe?deviceid={1}&subscriptionType={2}&subscriptionUrl={3}";

        /// <summary>
        /// Relative URL to subscribe to push notifications
        /// </summary>
        private static string UnSubscribeToPushRelativeUrl = "{0}notification/unsubscribe?deviceid={1}&subscriptionType={2}";

        /// <summary>
        /// Relative URL to subscribe to push notifications
        /// </summary>
        private static string UploadExceptionRelativeUrl = "{0}exception";

        /// <summary>
        /// get groups
        /// </summary>
        private static string GetGroupsRelativeUrl = "{0}group";

        /// <summary>
        /// get groups
        /// </summary>
        private static string CreateGroupRelativeUrl = "{0}group";

        /// <summary>
        /// get groups
        /// </summary>
        private static string AddGroupMemberRelativeUrl = "{0}group/user/add?groupId={1}&user={2}";

        /// <summary>
        /// get groups
        /// </summary>
        private static string RemoveGroupMemberRelativeUrl = "{0}group/user/remove?groupId={1}&user={2}";

        /// <summary>
        /// Create a poll
        /// </summary>
        private static string CreatePollRelativeUrl = "{0}poll";

        /// <summary>
        /// Post poll response
        /// </summary>
        private static string PostPollResponseRelativeUrl = "{0}poll/response?messageId={1}&response={2}";

        /// <summary>
        /// Static instance
        /// </summary>
        private static IServiceProxy instance = new YapperServiceProxy();

        /// <summary>
        /// Returns an instance of IServiceProxy
        /// </summary>
        public static IServiceProxy Instance
        {
            get
            {
                return YapperServiceProxy.instance;
            }
        }
        
        /// <summary>
        /// GetAllMessages
        /// </summary>
        public void GetAllMessages(Action<List<MessageModel>> callback)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "YapperServiceProxy::GetAllMessagesSinceLastSync ", "start"));
            (Application.Current as App).PerfTrackerStopWatch.Restart();

            Uri uri = new Uri(string.Format(
                YapperServiceProxy.AllMessagesRelativeUrl,
                YapperServiceProxy.ServicePrefix));
            GZipWebClient webClient = new GZipWebClient();
            webClient.Headers["AuthTokenCookie"] = this.GetAuthTokenCookie();
            webClient.Headers["Accept-Encoding"] = "gzip, deflate";

            webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(AllMessagesDownloaded);
            webClient.DownloadStringAsync(uri, callback);
        }

        /// <summary>
        /// GetAllMessagesSinceLastSync
        /// </summary>
        public void GetAllMessagesSinceLastSync(Action<List<MessageModel>> callback, DateTime LastSyncDateTime)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "YapperServiceProxy::GetAllMessagesSinceLastSync ", "start"));
            (Application.Current as App).PerfTrackerStopWatch.Restart();

            DateTime lastSyncUtcTime = LastSyncDateTime.ToUniversalTime();
            Uri uri = new Uri(string.Format(
                YapperServiceProxy.GetAllMessagesSinceLastSyncRelativeUrl,
                YapperServiceProxy.ServicePrefix, lastSyncUtcTime));
            GZipWebClient webClient = new GZipWebClient();
            webClient.Headers["AuthTokenCookie"] = this.GetAuthTokenCookie();
            webClient.Headers["Accept-Encoding"] = "gzip, deflate";

            webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(AllMessagesDownloaded);
            webClient.DownloadStringAsync(uri, callback);
        }

        /// <summary>
        /// GetFullMessageFromMessageId
        /// </summary>
        public void GetFullMessageFromMessageId(Action<MessageModel> callback, Guid conversationId, Guid messageId)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "YapperServiceProxy::GetFullMessageFromMessageId ", "start"));
            Uri uri = new Uri(string.Format(
                YapperServiceProxy.GetFullMessageFromMessageIdRelativeUrl,
                YapperServiceProxy.ServicePrefix, 
                conversationId,
                messageId));
            GZipWebClient webClient = new GZipWebClient();
            webClient.Headers["AuthTokenCookie"] = this.GetAuthTokenCookie();
            webClient.Headers["Accept-Encoding"] = "gzip, deflate";

            webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(SingleMessageDownloaded);
            webClient.DownloadStringAsync(uri, callback);
        }

        /// <summary>
        /// SendNewMessage
        /// </summary>
        /// <param name="conversationId"></param>
        /// <param name="message"></param>
        /// <param name="callback"></param>
        public void SendNewMessage(
            MessageModel messageModel,
            Action<MessageModel> callback)
        {
            Uri uri = new Uri(string.Format(
                    YapperServiceProxy.SendNewMessageRelativeUrl,
                    YapperServiceProxy.ServicePrefix));

            MemoryStream stream = new MemoryStream();
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(MessageModel));
            serializer.WriteObject(stream, messageModel);
            string jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray(), 0, stream.ToArray().Length);
            
            WebClient wc = new WebClient();
            wc.Headers["AuthTokenCookie"] = this.GetAuthTokenCookie();
            wc.Headers["Content-Type"] = "application/json; Charset=UTF-8";
            //wc.Headers["Accept-Encoding"] = "gzip, deflate";

            wc.UploadStringCompleted += new UploadStringCompletedEventHandler(NewMessageUploaded);

            Debug.WriteLine(string.Format("{0} {1} cid-{2} clientmessageid-{3} {4}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "YapperServiceProxy::SendNewMessage ", messageModel.ConversationId, messageModel.ClientMessageId, "start"));
            wc.UploadStringAsync(uri, null, jsonString, callback);

            (Application.Current as App).PerfTrackerString += "\nMsg sent:" + (Application.Current as App).PerfTrackerStopWatch.ElapsedMilliseconds.ToString();
        }

        public void GetRegisteredPhoneNumbers(List<string> userPhoneNumbers, Action<List<UserModel>> callback)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "YapperServiceProxy::GetRegisteredPhoneNumbers ", "start"));
            MemoryStream stream = new MemoryStream();
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<string>));
            serializer.WriteObject(stream, userPhoneNumbers);
            string jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray(), 0, stream.ToArray().Length);

            Uri uri = new Uri(string.Format(YapperServiceProxy.RegisteredContactsRelativeUrl, YapperServiceProxy.ServicePrefix));
            WebClient webClient = new WebClient();
            webClient.Headers["AuthTokenCookie"] = this.GetAuthTokenCookie();
            webClient.Headers["Content-Type"] = "application/json; Charset=UTF-8";

            webClient.UploadStringCompleted += new UploadStringCompletedEventHandler(RegisteredPhoneNumbersDownloaded);
            webClient.UploadStringAsync(uri, null, jsonString, callback);
        }

        public void RegisterNewUser(string phoneNumber, string name, Action<UserCookieModel> callback)
        {
            string uniqueId = string.Empty;
            byte[] deviceUniqueId;

            try
            {
                deviceUniqueId = (byte[])DeviceExtendedProperties.GetValue("DeviceUniqueId");
                uniqueId = BitConverter.ToString(deviceUniqueId);
            }
            catch (Exception)
            {
            }

            try
            {
                if (string.IsNullOrEmpty(uniqueId))
                {
                    uniqueId = Windows.Phone.System.Analytics.HostInformation.PublisherHostId;
                }
            }
            catch (Exception)
            {
                uniqueId = Guid.NewGuid().ToString();
            }

            UserSettingsModel.Instance.DeviceUniqueId = uniqueId;

            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "YapperServiceProxy::RegisterNewUser ", "start"));
            string encodedPhoneNumber = HttpUtility.UrlEncode(phoneNumber);
            string encodedName = HttpUtility.UrlEncode(name);
            Uri uri = new Uri(string.Format(YapperServiceProxy.NewUserRegistrationRelativeUrl, YapperServiceProxy.ServicePrefix, encodedPhoneNumber, encodedName, uniqueId));
            WebClient wc = new WebClient();
            wc.Headers["Content-Length"] = "0";

            wc.UploadStringCompleted += new UploadStringCompletedEventHandler(RegistrationCompleted);
            wc.UploadStringAsync(uri, null, string.Empty, callback);
        }

        public void ValidateConfirmationCode(string code, Action<UserCookieModel> callback)
        {
            Uri uri = new Uri(
                string.Format(
                    YapperServiceProxy.ValidateConfirmationCodeRelativeUrl, 
                    YapperServiceProxy.ServicePrefix, 
                    UserSettingsModel.Instance.MyPhoneNumber, 
                    code, 
                    UserSettingsModel.Instance.DeviceUniqueId,
                    DateTime.Now.Ticks));
            WebClient wc = new WebClient();
            wc.Headers["Content-Length"] = "0";

            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(ValidationCompleted);
            wc.DownloadStringAsync(uri, callback);
        }

        public void SubscribeToPush(string url, Action<bool> callback)
        {
            string uniqueId = string.Empty;

            uniqueId = UserSettingsModel.Instance.DeviceUniqueId;

            Uri uri = new Uri(
                string.Format(
                    YapperServiceProxy.SubscribeToPushRelativeUrl, 
                    YapperServiceProxy.ServicePrefix,
                    uniqueId,
                    SubscriptionType.WindowsPhoneToast, 
                    url));
            WebClient webClient = new WebClient();
            webClient.Headers["AuthTokenCookie"] = this.GetAuthTokenCookie();
            webClient.Headers["Content-Type"] = "application/json; Charset=UTF-8";

            webClient.UploadStringCompleted += new UploadStringCompletedEventHandler(SubscriptionCompleted);
            webClient.UploadStringAsync(uri, null, string.Empty, callback);
        }

        public void UnSubscribeToPush()
        {
            Uri uri = new Uri(
                string.Format(
                    YapperServiceProxy.UnSubscribeToPushRelativeUrl,
                    YapperServiceProxy.ServicePrefix,
                    UserSettingsModel.Instance.DeviceUniqueId,
                    SubscriptionType.WindowsPhoneToast));
            WebClient webClient = new WebClient();
            webClient.Headers["AuthTokenCookie"] = this.GetAuthTokenCookie();
            webClient.Headers["Content-Type"] = "application/json; Charset=UTF-8";

            webClient.UploadStringCompleted += new UploadStringCompletedEventHandler(UnSubscribeCompleted);
            webClient.UploadStringAsync(uri, null, string.Empty);
        }

        /// <summary>
        /// SendNewMessage
        /// </summary>
        /// <param name="conversationId"></param>
        /// <param name="message"></param>
        /// <param name="callback"></param>
        public void UploadExceptions()
        {
            if (UserSettingsModel.Instance.UserId == -1)
            {
                return;
            }

            List<ExceptionDetails> exceptions = UserSettingsModel.Instance.Exceptions;

            if (exceptions == null)
            {
                return;
            }

            UserSettingsModel.Instance.Exceptions = null;

            for (int i = 0; i < exceptions.Count; i++)
            {
                MemoryStream stream = new MemoryStream();
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ExceptionDetails));
                serializer.WriteObject(stream, exceptions[i]);
                string jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray(), 0, stream.ToArray().Length);

                Uri uri = new Uri(string.Format(
                    YapperServiceProxy.UploadExceptionRelativeUrl,
                    YapperServiceProxy.ServicePrefix));
                WebClient wc = new WebClient();
                wc.Headers["AuthTokenCookie"] = this.GetAuthTokenCookie();
                wc.Headers["Content-Type"] = "application/json; Charset=UTF-8";

                using (AutoResetEvent resetEvent = new AutoResetEvent(false))
                {
                    wc.UploadStringCompleted += new UploadStringCompletedEventHandler(ExceptionUploadCompleted);
                    wc.UploadStringAsync(uri, null, jsonString, resetEvent);

                    resetEvent.WaitOne();
                }
            }
        }

        public void GetGroups(Action<List<GroupModel>> callback)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "YapperServiceProxy::GetGroups ", "start"));
            Uri uri = new Uri(
                string.Format(
                    YapperServiceProxy.GetGroupsRelativeUrl,
                    YapperServiceProxy.ServicePrefix));
            WebClient webClient = new WebClient();
            webClient.Headers["AuthTokenCookie"] = this.GetAuthTokenCookie();

            webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(this.GetGroupsCompleted);
            webClient.DownloadStringAsync(uri, callback);
        }

        public void CreateGroup(string name, List<UserModel> members, Action<GroupModel> callback)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "YapperServiceProxy::CreateGroup ", "start"));
            if (!members.Contains(UserSettingsModel.Instance.Me))
            {
                members.Add(UserSettingsModel.Instance.Me);
            }

            GroupModel newGroup = new GroupModel() { Members = members, Name = name };
            MemoryStream stream = new MemoryStream();
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(GroupModel));
            serializer.WriteObject(stream, newGroup);
            string jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray(), 0, stream.ToArray().Length);

            Uri uri = new Uri(
                string.Format(
                    YapperServiceProxy.CreateGroupRelativeUrl,
                    YapperServiceProxy.ServicePrefix));
            WebClient webClient = new WebClient();
            webClient.Headers["AuthTokenCookie"] = this.GetAuthTokenCookie();
            webClient.Headers["Content-Type"] = "application/json; Charset=UTF-8";

            webClient.UploadStringCompleted += new UploadStringCompletedEventHandler(this.CreateGroupCompleted);
            webClient.UploadStringAsync(uri, null, jsonString, callback);
        }

        public void AddGroupMember(GroupModel group, UserModel user, Action<GroupModel, UserModel> callback)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "YapperServiceProxy::AddGroupMember ", "start"));

            Uri uri = new Uri(
                string.Format(
                    YapperServiceProxy.AddGroupMemberRelativeUrl,
                    YapperServiceProxy.ServicePrefix,
                    group.Id,
                    HttpUtility.UrlEncode(user.PhoneNumber)));

            WebClient webClient = new WebClient();
            webClient.Headers["AuthTokenCookie"] = this.GetAuthTokenCookie();
            webClient.Headers["Content-Type"] = "application/json; Charset=UTF-8";

            webClient.UploadStringCompleted += new UploadStringCompletedEventHandler(this.AddGroupMemberCompleted);
            webClient.UploadStringAsync(
                uri, 
                null, 
                string.Empty, 
                new YapperChat.Common.Tuple<Action<GroupModel, UserModel>, YapperChat.Common.Tuple<GroupModel, UserModel>>
                    (
                        callback, 
                        new YapperChat.Common.Tuple<GroupModel, UserModel>(group, user)
                    ));
        }

        public void RemoveGroupMember(GroupModel group, UserModel user, Action<GroupModel, UserModel> callback)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "YapperServiceProxy::RemoveGroupMember ", "start"));
            Uri uri = new Uri(
                string.Format(
                    YapperServiceProxy.RemoveGroupMemberRelativeUrl,
                    YapperServiceProxy.ServicePrefix,
                    group.Id,
                    HttpUtility.UrlEncode(user.PhoneNumber)));

            WebClient webClient = new WebClient();
            webClient.Headers["AuthTokenCookie"] = this.GetAuthTokenCookie();
            webClient.Headers["Content-Type"] = "application/json; Charset=UTF-8";

            webClient.UploadStringCompleted += new UploadStringCompletedEventHandler(this.RemoveGroupMemberCompleted);
            webClient.UploadStringAsync(
                uri,
                null,
                string.Empty,
                new YapperChat.Common.Tuple<Action<GroupModel, UserModel>, YapperChat.Common.Tuple<GroupModel, UserModel>>
                    (
                        callback,
                        new YapperChat.Common.Tuple<GroupModel, UserModel>(group, user)
                    ));
        }

        public void PostPollResponse(Guid messageId, string pollResponse, Action<MessageModel> callback)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "YapperServiceProxy::PostPollResponse ", "start"));
            Uri uri = new Uri(
                string.Format(
                    YapperServiceProxy.PostPollResponseRelativeUrl,
                    YapperServiceProxy.ServicePrefix,
                    messageId,
                    HttpUtility.UrlEncode(pollResponse)));

            WebClient webClient = new WebClient();
            webClient.Headers["AuthTokenCookie"] = this.GetAuthTokenCookie();
            webClient.Headers["Content-Type"] = "application/json; Charset=UTF-8";

            webClient.UploadStringCompleted += new UploadStringCompletedEventHandler(this.PostPollResponseCompleted);
            webClient.UploadStringAsync(
                uri,
                null,
                string.Empty,
                callback);
        }

        public void SetLastSyncDateTime(DateTime LastSyncDateTime)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "YapperServiceProxy::SetLastSyncDateTime ", "start"));
            long clientticks = LastSyncDateTime.ToUniversalTime().Ticks;
            Uri uri = new Uri(string.Format(
                YapperServiceProxy.SetLastSyncDataTimeRelativeUrl,
                YapperServiceProxy.ServicePrefix, Convert.ToString(clientticks)));
            WebClient webClient = new WebClient();
            webClient.Headers["AuthTokenCookie"] = this.GetAuthTokenCookie();
            webClient.Headers["Content-Type"] = "application/json; Charset=UTF-8";

            webClient.UploadStringCompleted += new UploadStringCompletedEventHandler(this.SetLastSyncDateTimeCompleted);
            webClient.UploadStringAsync(uri, null, string.Empty);
        }

        public void UpdateUserPublicKey(UserModel userWithPK, Action<bool> callback)
        {
            WebClient webClient = new WebClient();
            webClient.Headers["AuthTokenCookie"] = this.GetAuthTokenCookie();
            webClient.Headers["Content-Type"] = "application/json; Charset=UTF-8";

            MemoryStream stream = new MemoryStream();
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(UserModel));
            serializer.WriteObject(stream, userWithPK);
            string jsonString = System.Text.Encoding.UTF8.GetString(stream.ToArray(), 0, stream.ToArray().Length);

            Uri uri = new Uri(string.Format(
                YapperServiceProxy.UpdateUserPublicKeyRelativeUrl,
                YapperServiceProxy.ServicePrefix));

            webClient.UploadStringCompleted += new UploadStringCompletedEventHandler(this.UpdateUserPublicKeyCompleted);
            webClient.UploadStringAsync(uri, null, jsonString, callback);
        }

        private void RegistrationCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "YapperServiceProxy::RegisterNewUser ", "end"));
            UserCookieModel userCookie = null;
            if (e.Error == null)
            {
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(UserCookieModel));
                userCookie = (UserCookieModel)jsonSerializer.ReadObject(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(e.Result)));
            }
            else
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (UserSettingsModel.Instance.DebugEnabled == true)
                    {
                        MessageBox.Show(e.Error.Message);
                    }
                });
            }

            Action<UserCookieModel> callback = e.UserState as Action<UserCookieModel>;
            callback(userCookie);
        }

        private void UpdateUserPublicKeyCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            Action<bool> callback = e.UserState as Action<bool>;
            callback(e.Error == null);
        }

        private void ValidationCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            UserCookieModel cookie = null;

            if (e.Error == null)
            {
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(UserCookieModel));
                cookie = (UserCookieModel)jsonSerializer.ReadObject(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(e.Result)));
            }
            else
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (UserSettingsModel.Instance.DebugEnabled == true)
                    {
                        MessageBox.Show(e.Error.Message);
                    }
                });
            }

            Action<UserCookieModel> callback = e.UserState as Action<UserCookieModel>;
            callback(cookie);
        }

        /// <summary>
        /// Callback invoked when the API call ends.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AllConversationsDownloaded(object sender, DownloadStringCompletedEventArgs e)
        {
            List<ConversationModel> result = null;

            if (e.Error == null)
            {
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(List<ConversationModel>));
                result = (List<ConversationModel>)jsonSerializer.ReadObject(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(e.Result)));
            }
            else
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (UserSettingsModel.Instance.DebugEnabled == true)
                        {
                            MessageBox.Show(e.Error.Message);
                        }
                    });
            }

            Action<List<ConversationModel>> callback = e.UserState as Action<List<ConversationModel>>;
            callback(result);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConversationMessagesDownloaded(object sender, DownloadStringCompletedEventArgs e)
        {
            List<MessageModel> result = null;
            if (e.Error == null)
            {
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(List<MessageModel>));
                result = (List<MessageModel>)jsonSerializer.ReadObject(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(e.Result)));
            }
            else
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (UserSettingsModel.Instance.DebugEnabled == true)
                        {
                            MessageBox.Show(e.Error.Message);
                        }
                    });
            }

            Action<List<MessageModel>> callback = e.UserState as Action<List<MessageModel>>;
            callback(result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AllMessagesDownloaded(object sender, DownloadStringCompletedEventArgs e)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "YapperServiceProxy::GetAllMessages/GetAllMessagesSinceLastSync ", "end"));
            (Application.Current as App).PerfTrackerString += "\nMessages Downloaded:" + (Application.Current as App).PerfTrackerStopWatch.ElapsedMilliseconds.ToString();
            List<MessageModel> result = null;
            if (e.Error == null)
            {
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(List<MessageModel>));
                result = (List<MessageModel>)jsonSerializer.ReadObject(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(e.Result)));
            }

            Action<List<MessageModel>> callback = e.UserState as Action<List<MessageModel>>;
            callback(result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SingleMessageDownloaded(object sender, DownloadStringCompletedEventArgs e)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "YapperServiceProxy::GetFullMessageFromMessageId ", "end"));
            List<MessageModel> result = null;
            if (e.Error == null)
            {
                MessageModel returnMessage = null;
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(MessageModel));
                returnMessage = (MessageModel)jsonSerializer.ReadObject(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(e.Result)));
                result = new List<MessageModel>();
                result.Add(returnMessage);
            }

            Action<MessageModel> callback = e.UserState as Action<MessageModel>;

            if (result != null && result.Count != 0)
            {
                callback(result[0]);
            }
        }

        private void NewConversationCreated(object sender, UploadStringCompletedEventArgs e)
        {
            MessageModel message = null;
            
            if (e.Error == null)
            {
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(MessageModel));
                message = (MessageModel)jsonSerializer.ReadObject(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(e.Result)));
            }
            else
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (UserSettingsModel.Instance.DebugEnabled == true)
                    {
                        MessageBox.Show(e.Error.Message);
                    }
                });
            }

            Action<MessageModel> callback = e.UserState as Action<MessageModel>;
            callback(message);
        }

        private void NewMessageUploaded(object sender, UploadStringCompletedEventArgs e)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "YapperServiceProxy::SendNewMessage ", "end"));
            MessageModel message = null;
            if (e.Error == null)
            {
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(MessageModel));
                message = (MessageModel)jsonSerializer.ReadObject(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(e.Result)));
            }
            else
            {
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "YapperServiceProxy::SendNewMessage error", e.Error));
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (UserSettingsModel.Instance.DebugEnabled == true)
                    {
                        MessageBox.Show(e.Error.Message);
                    }
                });
            }

            (Application.Current as App).PerfTrackerString += "\nMsg callback:" + (Application.Current as App).PerfTrackerStopWatch.ElapsedMilliseconds.ToString();
            Action<MessageModel> callback = e.UserState as Action<MessageModel>;
            callback(message);
        }

        /// <summary>
        /// Callback invoked when the API call ends.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RegisteredPhoneNumbersDownloaded(object sender, UploadStringCompletedEventArgs e)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "YapperServiceProxy::GetRegisteredPhoneNumbers ", "end"));
            List<UserModel> result = null;
            if (e.Error == null)
            {
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(List<UserModel>));
                result = (List<UserModel>)jsonSerializer.ReadObject(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(e.Result)));
            }

            Action<List<UserModel>> callback = e.UserState as Action<List<UserModel>>;
            callback(result);
        }

        private void SubscriptionCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            bool success = false;
            if (e.Error == null)
            {
                success = true;
            }

            Action<bool> callback = e.UserState as Action<bool>;
            callback(success);
        }

        private void UnSubscribeCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            bool success = false;
            if (e.Error == null)
            {
                success = true;
            }
        }

        private void ExceptionUploadCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            AutoResetEvent resetEvent = e.UserState as AutoResetEvent;
            resetEvent.Set();
        }

        private void GetGroupsCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "YapperServiceProxy::GetGroups ", "end"));
            List<GroupModel> groups = null;
            if (e.Error == null)
            {
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(List<GroupModel>));
                groups = (List<GroupModel>)jsonSerializer.ReadObject(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(e.Result)));
            }

            Action<List<GroupModel>> callback = e.UserState as Action<List<GroupModel>>;
            callback(groups);
        }

        private void CreateGroupCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "YapperServiceProxy::CreateGroup ", "end"));
            GroupModel newGroup = null;
            if (e.Error == null)
            {
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(GroupModel));
                newGroup = (GroupModel)jsonSerializer.ReadObject(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(e.Result)));
            }

            Action<GroupModel> callback = e.UserState as Action<GroupModel>;
            callback(newGroup);
        }


        private void AddGroupMemberCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "YapperServiceProxy::AddGroupMember ", "end"));
            GroupModel group = ((YapperChat.Common.Tuple<Action<GroupModel, UserModel>, YapperChat.Common.Tuple<GroupModel, UserModel>>)e.UserState).m_Item2.m_Item1;
            UserModel user = null;
            Action<GroupModel, UserModel> callback = ((YapperChat.Common.Tuple<Action<GroupModel, UserModel>, YapperChat.Common.Tuple<GroupModel, UserModel>>)e.UserState).m_Item1;

            if (e.Error == null)
            {
                user = ((YapperChat.Common.Tuple<Action<GroupModel, UserModel>, YapperChat.Common.Tuple<GroupModel, UserModel>>)e.UserState).m_Item2.m_Item2;
            }

            callback(group, user);
        }

        private void RemoveGroupMemberCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "YapperServiceProxy::RemoveGroupMember ", "end"));
            GroupModel group = ((YapperChat.Common.Tuple<Action<GroupModel, UserModel>, YapperChat.Common.Tuple<GroupModel, UserModel>>)e.UserState).m_Item2.m_Item1;
            UserModel user = null;
            Action<GroupModel, UserModel> callback = ((YapperChat.Common.Tuple<Action<GroupModel, UserModel>, YapperChat.Common.Tuple<GroupModel, UserModel>>)e.UserState).m_Item1;

            if (e.Error == null)
            {
                user = ((YapperChat.Common.Tuple<Action<GroupModel, UserModel>, YapperChat.Common.Tuple<GroupModel, UserModel>>)e.UserState).m_Item2.m_Item2;
            }

            callback(group, user);
        }

        private void PostPollResponseCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "YapperServiceProxy::PostPollResponse ", "end"));
            MessageModel pollResponseMessage = null;

            if (e.Error == null)
            {
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(MessageModel));
                pollResponseMessage = (MessageModel)jsonSerializer.ReadObject(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(e.Result)));
            }

            Action<MessageModel> callback = (Action<MessageModel>)e.UserState;
            callback(pollResponseMessage);
        }

        private void SetLastSyncDateTimeCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "YapperServiceProxy::SetLastSyncDateTime ", "end"));
        }

#if FALSE
        private string GetAuthTokenCookie()
        {
            if (!string.IsNullOrEmpty(UserSettingsModel.Instance.MyPhoneNumber))
            {
                return UserSettingsModel.Instance.MyPhoneNumber;
            }
            else
            {
                return "0000000000";
            }
        }
#else
        private string GetAuthTokenCookie()
        {
            if (!string.IsNullOrEmpty(UserSettingsModel.Instance.Cookie))
            {
                return UserSettingsModel.Instance.Cookie;
            }
            else
            {
                return "0000000000";
            }
        }
        /*private string GetAuthTokenCookie()
        {
            return UserSettingsModel.Instance.Cookie;
        }*/
#endif
    }
}
