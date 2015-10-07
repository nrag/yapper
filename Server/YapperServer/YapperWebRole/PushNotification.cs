using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using DataAccessLayer;
using DAL = DataAccessLayer;

namespace YapperWebRole
{
    public class PushNotification
    {
        public PushNotification(
            List<Tuple<User, string>> urls,
            Guid messageId,
            Guid conversationId,
            User sender,
            long postDate,
            string message,
            Group group)
        {
            this.Urls = urls;
            this.MessageId = messageId;
            this.ConversationId = conversationId;
            this.Sender = sender;
            this.PostDate = postDate;
            this.MessageId = messageId;
            this.Message = message;
            this.Group = group;
        }

        public List<Tuple<User, string>> Urls
        {
            get;
            set;
        }

        public Guid MessageId
        {
            get;
            set;
        }

        public Guid ConversationId
        {
            get;
            set;
        }

        public User Sender
        {
            get;
            set;
        }

        public long PostDate
        {
            get;
            set;
        }

        public string Message
        {
            get;
            set;
        }

        public string SenderName
        {
            get
            {
                if (this.Sender != null)
                {
                    return this.Sender.Name;
                }

                return string.Empty;
            }
        }

        public User Group
        {
            get;
            set;
        }
    }

    public class PushNotificationQueue
    {
        private List<PushNotification> pushNotifications = new List<PushNotification>();

        private bool isScheduled = false;

        public static PushNotificationQueue Instance = new PushNotificationQueue();

        protected PushNotificationQueue()
        {
        }

        public void SchedulePush(PushNotification push)
        {
            lock (this.pushNotifications)
            {
                this.pushNotifications.Add(push);

                if (!this.isScheduled)
                {
                    ThreadPool.QueueUserWorkItem(this.PumpQueue);
                    this.isScheduled = true;
        }
            }
        }

        public void PumpQueue(object state)
        {
            PushNotification notification;

            while ((notification = this.GetNextNotification()) != null)
            {
                if (notification.Urls == null)
                {
                    return;
                }

                for (int i = 0; i < notification.Urls.Count; i++)
                {
                    if (string.IsNullOrEmpty(notification.Urls[i].Item2))
                    {
                        continue;
                    }

                    // The optional custom header X-MessageID uniquely identifies a notification message. 
                    // If it is present, the // same value is returned in the notification response. It must be a string that contains a UUID.
                    // sendNotificationRequest.Headers.Add("X-MessageID", uuid);

                    // Create the toast message.

                    string param = null;

                    if (notification.Group == null)
                    {
                        param = string.Format(
                         "/Views/ConversationMessagesView.xaml?conversationId={0}&amp;messageId={1}&amp;date={2}&amp;recipientId={3}&amp;recipientName={4}",
                         notification.ConversationId,
                         notification.MessageId,
                         notification.PostDate,
                         notification.Sender.Id,
                         notification.SenderName);
                    }
                    else
                    {
                        param = string.Format(
                          "/Views/ConversationMessagesView.xaml?conversationId={0}&amp;messageId={1}&amp;date={2}&amp;recipientId={3}&amp;recipientName={4}&amp;groupId={5}&amp;groupName={6}",
                          notification.ConversationId,
                          notification.MessageId,
                          notification.PostDate,
                          notification.Sender.Id,
                          notification.SenderName,
                          notification.Group.Id,
                          notification.Group.Name);
                    }

                    int endIndex = 1;
                    int count = MessageStore.MessageStore.Instance.GetUnseenMessageCount(notification.Urls[i].Item1, notification.Urls[i].Item1.LastSyncTime);

                    string tileMessage = string.Empty;

                    if (count > 0)
                    {
                        endIndex = 2;
                        tileMessage = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                                    "<wp:Notification xmlns:wp=\"WPNotification\">" +
                                    "<wp:Tile><wp:Count>" + count + "</wp:Count></wp:Tile>" +
                                    "</wp:Notification>";
                    }

                    string toastMessage = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                    "<wp:Notification xmlns:wp=\"WPNotification\">" +
                        "<wp:Toast>" +
                    "<wp:Text1>" + notification.SenderName + "</wp:Text1>" +
                            "<wp:Text2>" + notification.Message + "</wp:Text2>" +
                            "<wp:Param>" + param + "</wp:Param>" +
                        "</wp:Toast> " +
                    "</wp:Notification>";

                    for (int index = 0; index < endIndex; index++)
                    {
                        HttpWebRequest sendNotificationRequest = (HttpWebRequest)WebRequest.Create(notification.Urls[i].Item2);
                        sendNotificationRequest.Method = "POST";

                        string messageContent = string.Empty;

                        if (index == 1)
                        {
                            messageContent = tileMessage;
                            sendNotificationRequest.Headers.Add("X-WindowsPhone-Target", "token");
                            sendNotificationRequest.Headers.Add("X-NotificationClass", "1");
                        }

                        if (index == 0)
                        {
                            messageContent = toastMessage;
                            sendNotificationRequest.Headers.Add("X-WindowsPhone-Target", "toast");
                            sendNotificationRequest.Headers.Add("X-NotificationClass", "2");
                        }

                        // Sets the notification payload to send.
                        byte[] notificationMessage = Encoding.Default.GetBytes(messageContent);

                        // Sets the web request content length.
                        sendNotificationRequest.ContentLength = notificationMessage.Length;
                        sendNotificationRequest.ContentType = "text/xml";

                        // Send the notification and get the response.
                        try
                        {
                            using (Stream requestStream = sendNotificationRequest.GetRequestStream())
                            {
                                requestStream.Write(notificationMessage, 0, notificationMessage.Length);
                            }

                            HttpWebResponse response = (HttpWebResponse)sendNotificationRequest.GetResponse();
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
        }
        
        private PushNotification GetNextNotification()
        {
            PushNotification notification;
            lock (this.pushNotifications)
            {
                notification = pushNotifications.Count != 0 ? pushNotifications[0] : null;
                if (notification != null)
                {
                    this.pushNotifications.Remove(notification);
                }
                else
                {
                    this.isScheduled = false;
                }
            }

            return notification;
        }
    }
}