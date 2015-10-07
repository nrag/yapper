using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Runtime.Serialization.Json;
using System.IO;

using GalaSoft.MvvmLight.Messaging;
using YapperChat.Models;
using YapperChat.ServiceProxy;
using YapperChat.EventMessages;
using YapperChat.Database;
using System.Data.Linq;
using YapperChat.Sync;
using YapperChat;

namespace YapperChat.ViewModels
{
    public class AllConversationsViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Observable collections for conversations
        /// </summary>
        private ObservableCollection<ConversationModel> _conversations = new ObservableCollection<ConversationModel>();

        /// <summary>
        /// Map between recipients and conversation
        /// </summary>
        private Dictionary<int, ConversationModel> recipientConversationMap = new Dictionary<int, ConversationModel>();

        /// <summary>
        /// Selected Conversation
        /// </summary>
        private ConversationModel selectedConversation;

        /// <summary>
        /// Indicates if initial load was completed
        /// </summary>
        private bool initialLoadCompleted = false;

        /// <summary>
        /// If false, need to register for push.
        /// </summary>
        private bool registeredForPush = false;

        /// <summary>
        /// Service proxy to call the Yapper service
        /// </summary>
        private IServiceProxy serviceProxy;

        /// <summary>
        /// 
        /// </summary>
        private IUserSettings userSettings;

        private bool _isDisposed = false;

        /// <summary>
        /// Constructor to create the AllConversationsViewModel instance
        /// </summary>
        /// <param name="phone"></param>
        public AllConversationsViewModel()
            : this(YapperServiceProxy.Instance, UserSettingsModel.Instance)
        {
        }

        /// <summary>
        /// Constructor to create the AllConversationsViewModel instance
        /// </summary>
        /// <param name="phone"></param>
        public AllConversationsViewModel(
            IServiceProxy serviceProxy,
            IUserSettings userSettings)
        {
            this.serviceProxy = serviceProxy;
            this.userSettings = userSettings;
            Messenger.Default.Register<NewMessageSavedEvent>(this, this.HandleNewMessageSavedEvent);
            Messenger.Default.Register<NewMessageEvent>(this, this.HandleNewMessageSentEvent);
            Messenger.Default.Register<DeleteEvent>(this, this.HandleDeleteEvent);
            lock (DataSync.Instance)
            {
                Messenger.Default.Register<SyncEvent>(this, this.HandleSyncCompleteEvent);
                DataSync.Instance.Sync();
            }

            this.initialLoadCompleted = DataSync.Instance.IsSyncComplete;
            this.ReadConversationFromLocalDB();
        }

        /// <summary>
        /// Public getter for the observable collections of conversations
        /// </summary>
        public ObservableCollection<ConversationModel> Conversations
        {
            get
            {
                return this._conversations;
            }
        }

        public ConversationModel SelectedConversation
        {
            get
            {
                return this.selectedConversation;
            }

            set
            {
                this.selectedConversation = value;
            }
        }

        /// <summary>
        /// Loaded 
        /// </summary>
        public bool IsLoaded
        {
            get
            {
                return this.initialLoadCompleted;
            }
        }

        /// <summary>
        /// Adds a new conversation to the viewmodel
        /// </summary>
        /// <param name="conversation"></param>
        public void AddNewConversation(ConversationModel conversation)
        {
            this.Conversations.Insert(0, conversation);
            int participantId = this.GetParticipantId(conversation);
            if (!this.recipientConversationMap.ContainsKey(participantId))
            {
                this.recipientConversationMap.Add(participantId, conversation);
                this.SelectedConversation = conversation;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="recipient"></param>
        /// <returns></returns>
        public ConversationModel GetConversation(int recipient)
        {
            if (this.recipientConversationMap.ContainsKey(recipient))
            {
                return this.recipientConversationMap[recipient];
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="recipient"></param>
        /// <returns></returns>
        public ConversationModel GetConversationFromConversationId(Guid conversationId)
        {
            if (conversationId == Guid.Empty)
            {
                return null;
            }

            for (int i = 0; i < this._conversations.Count; i++)
            {
                if (this._conversations[i].ConversationId == conversationId)
                {
                    return this._conversations[i];
                }
            }

            return null;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Handles a push notification
        /// </summary>
        /// <param name="pushEvent"></param>
        private void HandleNewMessageSavedEvent(NewMessageSavedEvent messageEvent)
        {
            this.HandleNewMessage(messageEvent.Message, false);
        }

        /// <summary>
        /// Handles a push notification
        /// </summary>
        /// <param name="pushEvent"></param>
        private void HandleNewMessageSentEvent(NewMessageEvent messageEvent)
        {
            this.HandleNewMessage(messageEvent.Message, messageEvent.IsPush);
        }

        /// <summary>
        /// Handles a delete event
        /// </summary>
        /// <param name="pushEvent"></param>
        private void HandleDeleteEvent(DeleteEvent deleteEvent)
        {
            this.ReadConversationFromLocalDB();

            DispatcherHelper.InvokeOnUiThread(() =>
            {
                this.NotifyPropertyChanged("IsLoaded");
            });
        }

        /// <summary>
        /// Handles a push notification
        /// </summary>
        /// <param name="pushEvent"></param>
        private void HandleNewMessage(MessageModel message, bool isPush)
        {
            if (message == null)
            {
                return;
            }

            DispatcherHelper.InvokeOnUiThread(
                () =>
                {
                    lock (this)
                    {
                        if (message == null ||
                            message.MessageType != MessageType.Message ||
                            (message.MessageFlags & MessageFlags.TaskItem) == MessageFlags.TaskItem ||
                            (message.MessageFlags & MessageFlags.TaskInfo) == MessageFlags.TaskInfo ||
                            (message.MessageFlags & MessageFlags.Task) == MessageFlags.Task)
                        {
                            return;
                        }

                        ConversationModel conversation = conversation = this.GetConversationFromConversationId(message.ConversationId);

                        // if we can't find the conversation, find by sender
                        if (conversation == null)
                        {
                            int otherParticipant = message.SenderId == this.userSettings.UserId ? message.RecipientId : message.SenderId;
                            if (this.recipientConversationMap.ContainsKey(otherParticipant))
                            {
                                conversation = this.recipientConversationMap[otherParticipant];
                                if (conversation != null && conversation.ConversationId == Guid.Empty)
                                {
                                    conversation.ConversationId = message.ConversationId;
                                }
                            }
                        }

                        // if it's still null, add the new conversation
                        if (conversation != null && conversation.LastPostUtcTime < message.PostDateTime)
                        {
                            conversation.LastPostUtcTime = message.PostDateTime;
                            conversation.LastPostPreview = message.TextMessageForDisplay;
                            conversation.UnreadCount = DataSync.Instance.GetUnreadCount(conversation.ConversationId);
                            this.Conversations.Remove(conversation);

                            this.Conversations.Add(conversation);
                        }

                        if (conversation == null)
                        {
                            conversation = new ConversationModel();
                            conversation.ConversationId = message.ConversationId;
                            conversation.ConversationParticipants = new Collection<UserModel>();
                            conversation.ConversationParticipants.Add(message.Sender);
                            conversation.ConversationParticipants.Add(message.Recipient);
                            conversation.LastPostPreview = message.TextMessageForDisplay;
                            conversation.LastPostUtcTime = message.LastUpdateTime;
                            conversation.UnreadCount = 1;

                            this.AddNewConversation(conversation);
                        }
                    }
                });
        }

        void SetTaskTextForDisplay(ConversationModel conversation, MessageModel message)
        {
            if (message.IsTaskDeleted.HasValue && message.IsTaskDeleted.Value)
            {
                conversation.LastPostPreview = message.SenderId == UserSettingsModel.Instance.Me.Id ? string.Format(YapperChat.Resources.Strings.YouDeletedTask, message.TaskName, message.Sender.Name) : string.Format(YapperChat.Resources.Strings.HasDeletedTask, message.Sender.Name, message.TaskName, message.TaskName);
            }
            else
            {
                conversation.LastPostPreview = message.SenderId == UserSettingsModel.Instance.Me.Id ? string.Format(YapperChat.Resources.Strings.YouUpdatedTask, message.TaskName, message.Sender.Name) : string.Format(YapperChat.Resources.Strings.HasUpdatedTask, message.Sender.Name, message.TaskName);
            }
        }

        /// <summary>
        /// Callback invoked when the API call ends.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleSyncCompleteEvent(SyncEvent syncComplete)
        {
            lock (this)
            {
                if (syncComplete.SyncState == SyncState.Start)
                {
                    this.initialLoadCompleted = false;
                }
                else
                {
                    try
                    {
                        if (syncComplete.Messages != null)
                        {
                            foreach (MessageModel m in syncComplete.Messages)
                            {
                                this.HandleNewMessage(m, false);
                            }
                        }
                        else
                        {
                            this.ReadConversationFromLocalDB();
                        }

                        this.initialLoadCompleted = true;
                    }
                    catch (Exception)
                    {
                        this.initialLoadCompleted = true;
                    }
                }
            }

            DispatcherHelper.InvokeOnUiThread(() =>
                {
                    this.NotifyPropertyChanged("IsLoaded");
                });
        }

        /// <summary>
        /// Read Messages from the database and convert them to conversations
        /// </summary>
        private void ReadConversationFromLocalDB()
        {
            using (DataContextWrapper<YapperDataContext> context = new DataContextWrapper<YapperDataContext>())
            {
                List<ConversationModel> conversationList = DataSync.Instance.GetConversations();

                if (this._conversations != null)
                {
                    foreach (ConversationModel cm in conversationList)
                    {
                        //ConversationModel  cm = DataSync.Instance.GetConversationFromMessage(context, m);

                        ConversationModel existing = this.GetConversationFromConversationId(cm.ConversationId);
                        if (existing != null)
                        {
                            if (existing.LastPostUtcTime < cm.LastPostUtcTime ||
                                string.Compare(existing.LastPostPreview, cm.LastPostPreview, StringComparison.OrdinalIgnoreCase) != 0 ||
                                existing.UnreadCount != cm.UnreadCount)
                            {
                                DispatcherHelper.InvokeOnUiThread(() =>
                                    {
                                        this.Conversations.Remove(existing);
                                        this.Conversations.Add(cm);
                                    });

                            }

                            continue;
                        }

                        int senderId = -1;
                        foreach (UserModel user in cm.ConversationParticipants)
                        {
                            if (user != null && user.Id != UserSettingsModel.Instance.UserId &&
                                (!cm.IsGroupConversation || user.UserType == UserType.Group))
                            {
                                senderId = user.Id;
                            }
                        }

                        if (!this.recipientConversationMap.ContainsKey(senderId))
                        {
                            this.recipientConversationMap.Add(senderId, cm);
                        }

                        DispatcherHelper.InvokeOnUiThread(() =>
                            {
                                this._conversations.Add(cm);
                            });
                    }
                }
            }
        }

        /// <summary>
        /// Gets the participant id for a given conversation
        /// </summary>
        /// <param name="conversationModel"></param>
        /// <returns></returns>
        private int GetParticipantId(ConversationModel conversationModel)
        {
            if (conversationModel.ConversationParticipants != null)
            {
                foreach (UserModel participant in conversationModel.ConversationParticipants)
                {
                    if (participant != null && (participant.Id != this.userSettings.UserId))
                    {
                        return participant.Id;
                    }
                }
            }

            return -1;
        }
    }
}
