using System;
using System.Linq;
using Microsoft.Phone.Notification;
using System.Windows.Threading;
using System.Text;
using YapperChat.Models;
using YapperChat.ServiceProxy;
using YapperChat.EventMessages;
using GalaSoft.MvvmLight.Messaging;
using System.ComponentModel;
using Microsoft.Devices;
using Microsoft.Phone.Shell;
using System.Windows;

namespace YapperChat.PushNotification
{
    public class PushNotification : IPushNotification
    {
        /// <summary>
        /// Singleton instance of push notification
        /// </summary>
        private static IPushNotification singleton = new PushNotification();

        /// <summary>
        /// Channel name for the toast notifications
        /// </summary>
        private static string ChannelName = "YapperToastChannel";

        /// <summary>
        /// Relative Uri prefix that is sent by the server as part of toast notification
        /// </summary>
        private static string RelativeUriPrefix = "/Views/ConversationMessagesView.xaml?";

        /// <summary>
        /// The uri parameter prefix for the conversationid in the uri in toast notification
        /// </summary>
        private static string ConversationIdPrefix = "conversationId=";

        /// <summary>
        /// The uri parameter prefix for the messageid in the uri in toast notification
        /// </summary>
        private static string MessageIdPrefix = "messageId=";

        /// <summary>
        /// The uri parameter prefix for the date in the uri in toast notification
        /// </summary>
        private static string DatePrefix = "date=";

        /// <summary>
        /// The uri parameter prefix for the senderid in the uri in the toastnotification
        /// </summary>
        private static string SenderIdPrefix = "recipientId=";

        /// <summary>
        /// The uri parameter prefix for the senderid in the uri in the toastnotification
        /// </summary>
        private static string SenderNamePrefix = "recipientName=";

        /// <summary>
        /// Indicates if this conversation is for a group
        /// </summary>
        private static string IsGroupPrefix = "isGroup=";

        /// <summary>
        /// Indicates if this conversation is for a group
        /// </summary>
        private static string GroupIdPrefix = "GroupId=";

        /// <summary>
        /// Indicates if this conversation is for a group
        /// </summary>
        private static string GroupNamePrefix = "GroupName=";

        /// <summary>
        /// Singleton instance for usersettings models
        /// </summary>
        public static IPushNotification Instance
        {
            get
            {
                if (PushNotification.singleton == null)
                {
                    PushNotification.singleton = new PushNotification();
                }

                return PushNotification.singleton;
            }
        }

        /// <summary>
        /// This sets up the push notification
        /// </summary>
        public void Setup()
        {
            if (UserSettingsModel.Instance.PushNotificationSubscriptionStatus == PushNotificationSubscriptionStatus.Disabled)
            {
                return;
            }

            this.Subscribe();
        }

        /// <summary>
        /// This subscribes this phone for push notification with MPNS and saves the push notification url in Yapper services
        /// </summary>
        public void Subscribe()
        {
            // Try to find the push channel.
            HttpNotificationChannel pushChannel = HttpNotificationChannel.Find(PushNotification.ChannelName);
            
            // If the channel was not found, then create a new connection to the push service.
            if (pushChannel == null || pushChannel.ChannelUri == null)
            {
                if (pushChannel != null)
                {
                    pushChannel.UnbindToShellToast();
                    pushChannel.Close();
                    pushChannel.Dispose();
                }

                pushChannel = new HttpNotificationChannel(PushNotification.ChannelName);

                // Register for all the events before attempting to open the channel.
                pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);

                // Register for this notification only if you need to receive the notifications while your application is running.
                pushChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(PushChannel_ShellToastNotificationReceived);

                pushChannel.Open();

                // Bind this new channel for toast events.
                pushChannel.BindToShellToast();
                pushChannel.BindToShellTile();
            }
            else
            {
                // The channel was already open, so just register for all the events.
                pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);

                // Register for this notification only if you need to receive the notifications while your application is running.
                pushChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(PushChannel_ShellToastNotificationReceived);

                if (UserSettingsModel.Instance.PushNotificationSubscriptionStatus == PushNotificationSubscriptionStatus.EnabledNotSubscribed)
                {
                    this.SubscribePushNotificationUrlInService();
                }

                // Display the URI for testing purposes. Normally, the URI would be passed back to your web service at this point.
                //System.Diagnostics.Debug.WriteLine(pushChannel.ChannelUri.ToString());
            }
        }

        /// <summary>
        /// This unsubscribes the phone from push notification and removes the url from the yapper service
        /// </summary>
        public void UnSubscribe()
        {
            // Try to find the push channel.
            using (HttpNotificationChannel pushChannel = HttpNotificationChannel.Find(PushNotification.ChannelName))
            {
                // If the channel was not found, then create a new connection to the push service.
                if (pushChannel != null)
                {
                    // If the channel is not null, close the channel
                    pushChannel.UnbindToShellToast();
                    pushChannel.Close();
                }

                UserSettingsModel.Instance.PushNotificationSubscriptionStatus = PushNotificationSubscriptionStatus.Disabled;

                this.UnSubscribePushNotificationUrlInService();
            }
        }

        /// <summary>
        /// Event handler for when the push channel Uri is updated.
        /// This saves the URL in the user's settings store and calls the yapper
        /// webservice to save the subscription url.
        /// In the callback for the webservice call, a boolean value is saved to
        /// indicate if the webservice call succeeded or not.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PushChannel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e)
        {
            // Save the subscription status
            UserSettingsModel.Instance.PushNotificationSubscriptionStatus = PushNotificationSubscriptionStatus.EnabledNotSubscribed;

            UserSettingsModel.Instance.PushNotificationUrl = e.ChannelUri.ToString();

            // Make the WebService call to subscribe the push notification url
            // with the yapper webservice
            this.SubscribePushNotificationUrlInService();
        }

        /// <summary>
        /// Event handler for when a push notification error occurs.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PushChannel_ErrorOccurred(object sender, NotificationChannelErrorEventArgs e)
        {
            // Don't do anything. Next time, this will be fixed
            //this.Subscribe();
        }

        /// <summary>
        /// Event handler for when a toast notification arrives while your application is running.  
        /// The toast will not display if your application is running so you must add this
        /// event handler if you want to do something with the toast notification.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PushChannel_ShellToastNotificationReceived(object sender, NotificationEventArgs e)
        {
            // Parse out the information that was part of the message.
            string message = null;
            string messageSenderName = null;
            Guid conversationId = Guid.Empty;
            int senderId = -1;
            Guid messageId = Guid.Empty;
            long messagePostDate = -1;
            bool isGroup = false;
            string groupName = null;
            int groupId = -1;

            foreach (string key in e.Collection.Keys)
            {
                if (string.Compare(
                    key,
                    "wp:Param",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.CompareOptions.IgnoreCase) == 0)
                {
                    PushNotification.GetMessageProperties(
                        e.Collection[key].Substring(PushNotification.RelativeUriPrefix.Length),
                        out conversationId,
                        out senderId,
                        out messageId,
                        out messagePostDate,
                        out groupId,
                        out groupName);

                    if (conversationId == Guid.Empty ||
                        messageId == Guid.Empty ||
                        senderId == -1 ||
                        messagePostDate == -1)
                    {
                        return;
                    }
                }

                if (string.Compare(
                    key,
                    "wp:Text2",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.CompareOptions.IgnoreCase) == 0)
                {
                    message = e.Collection[key];
                }

                if (string.Compare(
                    key,
                    "wp:Text1",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.CompareOptions.IgnoreCase) == 0)
                {
                    messageSenderName = e.Collection[key];
                }
            }

            // Handle the push only if it's not from this user
            // Or this is from group notifications
            if (senderId != UserSettingsModel.Instance.UserId)
            {
                PushNotificationEvent pushEvent = 
                    new PushNotificationEvent(
                        this, 
                        messageId, 
                        conversationId, 
                        message, 
                        messagePostDate, 
                        messageSenderName, 
                        senderId, 
                        groupId,
                        groupName);

                Messenger.Default.Send<PushNotificationEvent>(pushEvent);
                this.ClearTileCount();
            }
        }

        private void ClearTileCount()
        {
            var appTile = ShellTile.ActiveTiles.FirstOrDefault();
            if (appTile == null) return; //Don't create...just update

            appTile.Update(new StandardTileData() { BackTitle = "", BackContent = "", Count = 0 });
        }

        /// <summary>
        /// This gets the message properties from the toast notification when the app is in foreground
        /// </summary>
        /// <param name="relativeSuffix"></param>
        /// <param name="conversationId"></param>
        /// <param name="senderId"></param>
        /// <param name="messageId"></param>
        /// <param name="messagePostDate"></param>
        private static void GetMessageProperties(
            string relativeSuffix,
            out Guid conversationId,
            out int senderId,
            out Guid messageId,
            out long messagePostDate,
            out int groupId,
            out string groupName)
        {
            string[] parameters = relativeSuffix.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            conversationId = Guid.Empty;
            senderId = -1;
            messageId = Guid.Empty;
            messagePostDate = -1;
            groupId = -1;
            groupName = null;

            foreach (string parameter in parameters)
            {
                if (parameter.StartsWith(PushNotification.ConversationIdPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string conversationIdString = parameter.Substring(PushNotification.ConversationIdPrefix.Length);
                    if (!Guid.TryParse(conversationIdString, out conversationId))
                    {
                        // Ignore the notification because it has an invalid conversationId
                        return;
                    }
                }
                else if (parameter.StartsWith(PushNotification.MessageIdPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string messageIdString = parameter.Substring(PushNotification.MessageIdPrefix.Length);
                    if (!Guid.TryParse(messageIdString, out messageId))
                    {
                        // Ignore the notification because it has an invalid conversationId
                        return;
                    }
                }
                else if (parameter.StartsWith(PushNotification.DatePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string dateString = parameter.Substring(PushNotification.DatePrefix.Length);
                    if (!long.TryParse(dateString, out messagePostDate))
                    {
                        // Ignore the notification because it has an invalid conversationId
                        return;
                    }
                }
                else if (parameter.StartsWith(PushNotification.SenderIdPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string senderIdString = parameter.Substring(PushNotification.SenderIdPrefix.Length);
                    if (!int.TryParse(senderIdString, out senderId))
                    {
                        // Ignore the notification because it has an invalid conversationId
                        return;
                    }
                }
                else if (parameter.StartsWith(PushNotification.SenderNamePrefix, StringComparison.OrdinalIgnoreCase))
                {
                }
                else if (parameter.StartsWith(PushNotification.GroupIdPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string groupIdString = parameter.Substring(PushNotification.GroupIdPrefix.Length);
                    if (!int.TryParse(groupIdString, out groupId))
                    {
                        return;
                    }
                }
                else if (parameter.StartsWith(PushNotification.GroupNamePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    groupName = parameter.Substring(PushNotification.GroupNamePrefix.Length);
                }
                else
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Helper method to make the webservice call to save the subscription url
        /// </summary>
        private void SubscribePushNotificationUrlInService()
        {
            YapperServiceProxy.Instance.SubscribeToPush(UserSettingsModel.Instance.PushNotificationUrl, this.PushNotificationUrlSaved);
        }

        /// <summary>
        /// Helper method to make the webservice call to unsubscribe the subscription
        /// </summary>
        private void UnSubscribePushNotificationUrlInService()
        {
            YapperServiceProxy.Instance.UnSubscribeToPush();
        }

        /// <summary>
        /// callback invoked when the webservice call to save the subcription url succeeds
        /// </summary>
        /// <param name="success"></param>
        private void PushNotificationUrlSaved(bool success)
        {
            // If the save was successful remember that in the user settings
            if (success)
            {
                UserSettingsModel.Instance.PushNotificationSubscriptionStatus = PushNotificationSubscriptionStatus.EnabledSubscribed;
            }
        }
    }
}
