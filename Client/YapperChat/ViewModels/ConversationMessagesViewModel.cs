using System;
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
using System.Collections.ObjectModel;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Collections.Generic;
using System.Text;

using YapperChat.Models;
using System.ComponentModel;
using YapperChat.ServiceProxy;
using GalaSoft.MvvmLight.Messaging;
using YapperChat.EventMessages;
using YapperChat.Database;
using YapperChat.Sync;
using Windows.Devices.Geolocation;
using YapperChat.Common;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Threading;

namespace YapperChat.ViewModels
{
    public class ConversationMessagesViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// The conversationid for that's being displayed
        /// </summary>
        private Guid conversationId;

        /// <summary>
        /// If true, the page is loading
        /// </summary>
        private bool isSyncing= true;

        private bool isLoading = false;

        /// <summary>
        /// Whether this is a group 
        /// it's nullable if it's passed in the constructor
        /// </summary>
        private bool? isGroup;

        /// <summary>
        /// If true, this means the message is being sent
        /// </summary>
        private bool _isSending = false;

        private bool noMoreMessages = false;

        /// <summary>
        /// Name of the participant
        /// </summary>
        private UserModel recipient;

        /// <summary>
        /// The list of messages
        /// </summary>
        private ObservableSortedList<MessageModel> messages;

        /// <summary>
        /// ServiceProxy
        /// </summary>
        private IServiceProxy serviceProxy;

        /// <summary>
        /// UserSettings
        /// </summary>
        private IUserSettings userSettings;

        /// <summary>
        /// latest message
        /// </summary>
        private MessageModel latestMessage;

        /// <summary>
        /// 
        /// </summary>
        private ContactDetailsViewModel contactDetails = new ContactDetailsViewModel();

        private GroupDetailsViewModel groupDetails = new GroupDetailsViewModel();

        /// <summary>
        /// List of questions that need to be displayed
        /// </summary>
        private ObservableCollection<StringBuilder> listOfQuestions;

        private ObservableCollection<AppointmentDateTime> listOfAppointments;

        private ObservableCollection<StringBuilder> listOfTaskItems;
        /// <summary>
        /// Creates an instance of ConversationMessagesViewModel
        /// </summary>
        public ConversationMessagesViewModel(Guid conversationId, UserModel recipient, bool? isGroup)
            : this(
                YapperServiceProxy.Instance, 
                UserSettingsModel.Instance,
                conversationId,
                recipient,
                isGroup)
        {
        }

        /// <summary>
        /// Creates a ConversationMessagesViewModel
        /// </summary>
        /// <param name="phone"></param>
        public ConversationMessagesViewModel(
            IServiceProxy serviceProxy,
            IUserSettings userSettings,
            Guid conversationId,
            UserModel recipient,
            bool? isGroup)
        {
            this.serviceProxy = serviceProxy;
            this.userSettings = userSettings;
            this.messages = new ObservableSortedList<MessageModel>();
            this.conversationId = conversationId;
            this.recipient = recipient;
            this.isGroup = isGroup;
            this.listOfQuestions = new ObservableCollection<StringBuilder>();
            this.listOfAppointments = new ObservableCollection<AppointmentDateTime>();
            this.listOfTaskItems = new ObservableCollection<StringBuilder>();
            this.contactDetails.YapperName = recipient.Name;
            this.contactDetails.YapperPhone = recipient.PhoneNumber;
            this.contactDetails.UserId = recipient.Id;
            this.contactDetails.Search();

            if (this.IsGroup)
            {
                this.groupDetails.SetGroup(DataSync.Instance.GetGroup(this.recipient.Id));
            }

            string[] questions = new string[] { "Yes", "No", "Pass" };

            foreach (string s in questions)
            {
                this.listOfQuestions.Add(new StringBuilder(s));
            }

            AppointmentDateTime [] appts = new AppointmentDateTime[] { new AppointmentDateTime(DateTime.Now.Ticks), new AppointmentDateTime(DateTime.Now.Ticks), new AppointmentDateTime(DateTime.Now.Ticks) };

            foreach (AppointmentDateTime s in appts)
            {
                this.listOfAppointments.Add(s);
            }

            // Register the view to handle push notification when the app is running
            Messenger.Default.Register<NewMessageSavedEvent>(this, this.HandleNewMessageSavedEvent);
            Messenger.Default.Register<NewMessageEvent>(this, this.HandleNewMessageEvent);
            Messenger.Default.Register<ExistingMessageEvent>(this, this.HandleExistingMessageEvent);
            Messenger.Default.Register<DeleteEvent>(this, this.HandleDeleteEvent);

            lock (DataSync.Instance)
            {
                Messenger.Default.Register<SyncEvent>(this, this.HandleSyncCompleteEvent);

                DataSync.Instance.Sync();
                this.IsSyncing = !DataSync.Instance.IsSyncComplete;
            }

            this.IsCurrentyViewing = true;
        }

        /// <summary>
        /// The getter for conversationId
        /// </summary>
        public Guid ConversationId
        {
            get
            {
                return this.conversationId;
            }

            set
            {
                this.conversationId = value;
            }
        }

        public UserModel Recipient
        {
            get
            {
                return this.recipient;
            }

            set
            {
                this.recipient = value;
            }
        }

        /// <summary>
        /// Name of the participant
        /// </summary>
        public string ParticipantName
        {
            get
            {
                if (this.recipient != null)
                {
                    return this.recipient.Name;
                }

                return "No Name";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ObservableSortedList<MessageModel> Messages
        {
            get
            {
                return this.messages;
            }
        }

        public ContactDetailsViewModel ContactDetail
        {
            get
            {
                return this.contactDetails;
            }
        }

        public GroupDetailsViewModel GroupDetail
        {
            get
            {
                return this.groupDetails;
            }
        }

        /// <summary>
        /// Indicates if the user is currently viewing the conversation
        /// </summary>
        public bool IsCurrentyViewing
        {
            get;
            set;
        }

        public ObservableCollection<StringBuilder> ListOfQuestions
        {
            get
            {
                return this.listOfQuestions;
            }
        }

        public ObservableCollection<AppointmentDateTime> ListOfAppointments
        {
            get
            {
                return this.listOfAppointments;
            }
        }

        public ObservableCollection<StringBuilder> ListOfTaskItems
        {
            get
            {
                return this.listOfTaskItems;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsSyncing
        {
            get
            {
                return this.isSyncing;
            }

            private set
            {
                this.isSyncing = value;
                this.NotifyPropertyChanged("IsSyncing");
            }
        }

        public bool IsLoading
        {
            get
            {
                return this.isLoading;
            }

            private set
            {
                this.isLoading = value;
                this.NotifyPropertyChanged("IsLoading");
            }
        }

        public bool IsGroup
        {
            get
            {
                if (this.isGroup.HasValue)
                {
                    return this.isGroup.Value;
                }

                return this.Recipient.UserType == UserType.Group;
            }
        }

        public bool EncryptionRequested
        {
            get;
            set;
        }

        public bool IsSendingMessage
        {
            get
            {
                return this._isSending;
            }

            private set
            {
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "ConversationMessagesViewModel::IsSending is ", value.ToString()));

                this._isSending = value;
                this.NotifyPropertyChanged("IsSendingMessage");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Participants
        {
            get
            {
                if (!string.IsNullOrEmpty(this.ParticipantName))
                {
                    return this.ParticipantName;
                }

                return "NoName";
            }
        }

        public void PostPollResponse(Guid messageId, Guid clientMessageId, string message)
        {
            this.IsSendingMessage = true;
            MessageModel pollMessage = this.messages.First((x) => { return x.MessageId == messageId;});
            MessageModel pollResponse = new MessageModel();
            pollResponse.LastUpdateTime = pollResponse.PostDateTime = DateTime.UtcNow;
            pollResponse.PollMessageId = messageId;
            pollResponse.PollClientMessageId = clientMessageId;
            pollResponse.ConversationId = this.conversationId;
            pollResponse.MessageFlags |= MessageFlags.PollResponseMessage;
            pollResponse.MessageType = MessageType.Message;
            pollResponse.ClientMessageId = Guid.NewGuid();
            pollResponse.LastReadTime = new DateTime(1970, 1, 1);
            pollResponse.MessageId = Guid.Empty;
            pollResponse.Recipient = this.Recipient;
            pollResponse.RecipientId = this.Recipient.Id;
            pollResponse.Sender = this.userSettings.Me;
            pollResponse.SenderId = this.userSettings.Me.Id;
            pollResponse.PollResponse = message;
            pollResponse.TextMessage = this.userSettings.MyName + " responded " + message + " to question " + pollMessage.TextMessage;

            DataSync.Instance.PostPollResponse(pollResponse);
        }

        public void SendNewMessage(MessageModel message)
        {
            this.IsSendingMessage = true;
            message.ConversationId = this.conversationId;
            message.Sender = this.userSettings.Me;
            message.SenderId = message.Sender.Id;
            message.Recipient = this.recipient;
            message.RecipientId = this.recipient.Id;

            this.AddNewMessage(message);
            DataSync.Instance.SendMessage(message);
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

        private void HandleExistingMessageEvent(ExistingMessageEvent newMessageEvent)
        {
            DispatcherHelper.InvokeOnUiThread(() =>
            {
                lock (this)
                {
                    if (this.Messages.Contains(newMessageEvent.Message))
                    {
                        this.Messages.Remove(newMessageEvent.Message);
                        this.Messages.Add(newMessageEvent.Message);
                        
                        if ((newMessageEvent.Message.MessageFlags & MessageFlags.PollResponseMessage) == MessageFlags.PollResponseMessage)
                        {
                            this.ProcessPollResponse(newMessageEvent.Message);
                        }
                    }
                }
            });
        }

        private void HandleNewMessageSavedEvent(NewMessageSavedEvent newMessageEvent)
        {
            this.HandleNewMessageEvent(newMessageEvent.Message, false);
        }

        private void HandleNewMessageEvent(NewMessageEvent newMessageEvent)
        {
            this.HandleNewMessageEvent(newMessageEvent.Message, newMessageEvent.IsPush);
        }

        private void HandleDeleteEvent(DeleteEvent deleteEvent)
        {
            DispatcherHelper.InvokeOnUiThread(() =>
            {
                lock (this)
                {
                    try
                    {
                        this.messages.Clear();
                        this.LoadMessagesForConversations();
                    }
                    catch (Exception)
                    {
                    }
                }
            });
        }

        private void HandleNewMessageEvent(MessageModel newMessage, bool isPush)
        {
            if (newMessage == null)
            {
                return;
            }

            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "ConversationMessagesViewModel::HandleNewMessageEvent ", "Dispatch"));
            DispatcherHelper.InvokeOnUiThread(() =>
            {
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "ConversationMessagesViewModel::HandleNewMessageEvent ", "Start"));

                bool added = false;

                lock (this)
                {
                    if (newMessage != null)
                    {
                        added = this.AddNewMessage(newMessage);
                    }

                    if (!isPush)
                    {
                        this.IsSendingMessage = false;
                    }

                    if (newMessage != null)
                    {
                        BackgroundWorker worker = new BackgroundWorker();
                        worker.DoWork += (o, f) =>
                        {
                            if (added)
                            {
                                this.SendScrolltoBottomEvent(newMessage);
                            }

                            if (newMessage.MessageId != Guid.Empty)
                            {
                                DataSync.Instance.SetLastReadTime(this.conversationId, DateTime.UtcNow, this.IsCurrentyViewing);
                            }
                        };

                        worker.RunWorkerAsync();
                    }
                }
                (Application.Current as App).PerfTrackerString += "\nUI refreshed:" + (Application.Current as App).PerfTrackerStopWatch.ElapsedMilliseconds.ToString();

                if (newMessage.MessageId != Guid.Empty)
                {
                    if (UserSettingsModel.Instance.DebugEnabled == true)
                    {
                        if (UserSettingsModel.Instance.DebugEnabled == true)
                        {
                            MessageBox.Show((Application.Current as App).PerfTrackerString);
                        }
                        (Application.Current as App).PerfTrackerString = string.Empty;
                        (Application.Current as App).PerfTrackerStopWatch.Reset();
                    }
                }
            });

            if (!isPush)
            {
                // If message send was successful, the View should clear the text box
                // Otherwise, the textbox should not be cleared.
                Messenger.Default.Send<MessageSentEvent>(
                    new MessageSentEvent()
                    {
                        Success = newMessage != null,
                        ConversationId = this.conversationId,
                        Recipient = this.Recipient
                    });
            }
        }

        private bool AddNewMessage(MessageModel message)
        {
            if (message == null || 
                message.MessageType != MessageType.Message || 
                (message.MessageFlags & MessageFlags.TaskItem) == MessageFlags.TaskItem ||
                (message.MessageFlags & MessageFlags.TaskInfo) == MessageFlags.TaskInfo ||
                (message.MessageFlags & MessageFlags.Task) == MessageFlags.Task)
            {
                return false;
            }

            if (this.conversationId != Guid.Empty &&
                this.conversationId != message.ConversationId)
            {
                return false;
            }

            // If this is a new conversation, check the participants
            // If it's a group, just check the group
            // otherwise match both sender and recipient
            if (this.recipient.UserType != UserType.Group && message.Sender != null && message.Recipient != null &&
                message.Sender.Id != this.recipient.Id &&
                message.Recipient.Id != this.recipient.Id)
            {
                return false;
            }

            if (this.recipient.UserType == UserType.Group &&
                message.Recipient.Id != this.recipient.Id)
            {
                return false;
            }

            if (this.conversationId == Guid.Empty &&
                message.ConversationId != Guid.Empty)
            {
                this.conversationId = message.ConversationId;
            }

            if (this.IsDuplicateMessage(message))
            {
                return false;
            }

            // Don't add poll responses. Instead just update the
            // last update time of the poll message
            if ((message.MessageFlags & MessageFlags.PollResponseMessage) != MessageFlags.PollResponseMessage)
            {
                this.messages.Add(message);
                return true;
            }
            
            // If this is a poll response message, add to the polls observable collection and
            // update the poll responses in the original message
            if ((message.MessageFlags & MessageFlags.PollResponseMessage) == MessageFlags.PollResponseMessage)
            {
                this.ProcessPollResponse(message);
            }

            return false;
        }

        private bool IsDuplicateMessage(MessageModel m)
        {
            foreach (MessageModel mes in this.Messages)
            {
                if (mes.ClientMessageId == m.ClientMessageId)
                {
                    // reset the message if the message id is not set
                    if (mes.MessageId == Guid.Empty ||
                        mes.IsTaskInfoMessage)
                    {
                        this.Messages.Remove(mes);
                        this.Messages.Add(m);
                    }

                    return true;
                }
            }

            return false;
        }

        private void SendScrolltoBottomEvent(MessageModel message)
        {
            DispatcherHelper.InvokeOnUiThread(() =>
                {
                    // If message send was successful, the View should clear the text box
                    // Otherwise, the textbox should not be cleared.
                    Messenger.Default.Send<ScrollToEvent>(
                        new ScrollToEvent()
                        {
                            Message = message,
                        });
                });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleSyncCompleteEvent(SyncEvent syncEvent)
        {
            DispatcherHelper.InvokeOnUiThread(() =>
            {
                lock (this)
                {
                    if (syncEvent.SyncState == SyncState.Start)
                    {
                        this.IsSyncing = true;
                    }
                    else
                    {
                        try
                        {
                            if (syncEvent.Messages != null)
                            {
                                foreach (MessageModel m in syncEvent.Messages)
                                {
                                    this.HandleNewMessageEvent(m, false);
                                }
                            }
                            else
                            {
                                this.LoadMessagesForConversations();
                                BackgroundWorker worker = new BackgroundWorker();
                                worker.DoWork += (o, f) =>
                                {
                                    this.SendScrolltoBottomEvent(null);
                                };

                                worker.RunWorkerAsync();
                            }

                            this.IsSyncing = false;
                        }
                        catch (Exception)
                        {
                            this.IsSyncing = false;
                        }

                    }
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        internal void LoadMessagesForConversations()
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (o, f) =>
            {
                DispatcherHelper.InvokeOnUiThread(() =>
                {
                    lock (this)
                    {
                        using (DataContextWrapper<YapperDataContext> context = new DataContextWrapper<YapperDataContext>())
                        {
                            var newMessages = DataSync.Instance.GetMessages(context, this.conversationId, 30);
                            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "ConversationMessagesViewModel::LoadMessagesForConversations", "start Add messages"));
                            this.AddMessages(newMessages);
                            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "ConversationMessagesViewModel::LoadMessagesForConversations", "End add messages"));

                            DataSync.Instance.SetLastReadTime(this.conversationId, DateTime.UtcNow, this.IsCurrentyViewing);
                        }
                    }
                });
            };

            worker.RunWorkerAsync();
        }

        /// <summary>
        /// The logic to load messages is as follows:
        /// 1. Load all the messages in the last one week
        /// 2. Load at least 30 messages
        /// 3. For older messages, show explore older messages
        /// </summary>
        internal void LoadMoreMessages()
        {
            if (this.noMoreMessages)
            {
                return;
            }

            DispatcherHelper.InvokeOnUiThread(() =>
            {
                lock (this)
                {
                    using (DataContextWrapper<YapperDataContext> context = new DataContextWrapper<YapperDataContext>())
                    {
                        if (this.IsLoading)
                        {
                            return;
                        }

                        this.IsLoading = true;
                        var newMessages = DataSync.Instance.GetMessagesOlderThan(context, this.conversationId, this.Messages[0]);

                        int count = this.AddMessages(newMessages);

                        if (count == 0)
                        {
                            this.noMoreMessages = true;
                        }

                        this.IsLoading = false;
                    }
                }
            }, 
            true/*background*/);
        }

        private int AddMessages(IEnumerable<MessageModel> messagesToAdd)
        {
            int count = 0;
            int addedCount = 0;
            foreach (MessageModel m in messagesToAdd)
            {
                ++count;
                bool added = this.AddNewMessage(m);
                if (added)
                {
                    ++addedCount;
                }

                if (this.latestMessage == null)
                {
                    this.latestMessage = m;
                    continue;
                }

                if (this.latestMessage.PostDateTime <= m.PostDateTime)
                {
                    this.latestMessage = m;
                }

                if (addedCount >= 4)
                {
                    break;
                }
            }

            return count;
        }

        private void ProcessPollResponse(MessageModel message)
        {
            if ((message.MessageFlags & MessageFlags.PollResponseMessage) != MessageFlags.PollResponseMessage)
            {
                return;
            }

            MessageModel pollMessage = this.messages.Where(x => x.MessageId == message.PollMessageId).FirstOrDefault();

            if (pollMessage != null)
            {
                //bool responseExists = false;
                //if (pollMessage.Responses != null)
                //{
                //    foreach (KeyValuePair<string, int> response in pollMessage.Responses)
                //    {
                //        if (response.Value == message.SenderId)
                //        {
                //            responseExists = true;
                //        }
                //    }
                //}

                //if (!responseExists)
                //{
                //    pollMessage.AddResponse(message);
                //    pollMessage.LastUpdateTime = message.LastUpdateTime;
                //}

                if (message.IsMine)
                {
                    pollMessage.MyPollResponse = message.PollResponse;
                }
                else if (!message.IsGroup)
                {
                    pollMessage.OtherPollResponse = message.PollResponse;
                }
            }
        }
        
        private void NewMessageCreated(MessageModel message)
        {
            if (message != null)
            {
                this.messages.Add(message);
            }

            // If message send was successful, the View should clear the text box
            // Otherwise, the textbox should not be cleared.
            Messenger.Default.Send<MessageSentEvent>(new MessageSentEvent() { Success = message != null, ConversationId = this.conversationId, Recipient = message != null ? message.Recipient : null});

            this.IsSendingMessage = false;
        }
    }
}
