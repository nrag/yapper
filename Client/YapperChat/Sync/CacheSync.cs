using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows.Media.Imaging;
using System.Windows;

using YapperChat.Models;
using YapperChat.ServiceProxy;
using YapperChat.EventMessages;
using YapperChat.Database;
using YapperChat.ViewModels;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone;
using Microsoft.Phone.UserData;
using System.Collections.ObjectModel;
using YapperChat.Common;
using System.Runtime.Serialization.Json;
using Windows.Devices.Geolocation;
using System.Diagnostics;
using YapperChat.Resources;
using System.Data.Linq;
using Microsoft.Phone.Shell;
using Coding4Fun.Toolkit.Controls;
using System.Windows.Media;

namespace YapperChat.Sync
{
    class DataSync
    {
        /// <summary>
        /// 
        /// </summary>
        public static DataSync Instance = new DataSync();

        /// <summary>
        /// 
        /// </summary>
        private IDataContextWrapper dataContext = new DataContextWrapper<YapperDataContext>();

        /// <summary>
        /// 
        /// </summary>
        private IServiceProxy serviceProxy = YapperServiceProxy.Instance;
        /// <summary>
        /// 
        /// </summary>
        private IUserSettings userSettings = UserSettingsModel.Instance;

        private Dictionary<int, UserModel> userCache = new Dictionary<int, UserModel>();

        /// <summary>
        /// 
        /// </summary>
        private IContactSearchController contactSearchController = ContactSearchController.Instance;

        private bool syncStarted = false;

        public DataSync()
        {
            this.IsMessageSyncComplete = false;
            this.IsUsersSyncComplete = false;
            Messenger.Default.Register<PushNotificationEvent>(this, this.HandlePushNotificationEvent);
        }

        public bool IsSyncComplete
        {
            get
            {
                return this.IsMessageSyncComplete && this.IsUsersSyncComplete;
            }
        }

        public bool IsMessageSyncComplete
        {
            get;
            set;
        }

        public bool IsUsersSyncComplete
        {
            get;
            set;
        }

        /// <summary>
        /// Sync users and messages
        /// </summary>
        public void Sync(bool resync = false)
        {
            if (!resync && this.syncStarted)
            {
                return;
            }

            if (!this.syncStarted || this.IsSyncComplete)
            {
                var result = IsolatedStorageSettings.ApplicationSettings.Contains("LastSyncDateTime");
                if (result == false)
                {
                    this.SyncMessages();
                }
                else
                {
                    this.SyncMessagesSinceLastSync((DateTime)IsolatedStorageSettings.ApplicationSettings["LastSyncDateTime"]);
                }

                this.SyncUsers();
                this.syncStarted = true;
                Messenger.Default.Send<SyncEvent>(new SyncEvent() { SyncState = SyncState.Start });
            }
        }

        /// <summary>
        /// Sync messages
        /// </summary>
        public void SyncMessagesSinceLastSync(DateTime LastSyncDateTime)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::SyncMessagesSinceLastSync", "start"));
            lock (this)
            {
                this.IsMessageSyncComplete = false;

                // kick off a background task to sync the conversation list from server
                // to phone and update the observable collection
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (o, e) =>
                {
                    try
                    {
                        serviceProxy.GetAllMessagesSinceLastSync(this.MessagesDownloaded, LastSyncDateTime);
                    }
                    catch (Exception)
                    {
                        this.IsMessageSyncComplete = true;
                    }
                };

                worker.RunWorkerAsync();
            }
        }

        /// <summary>
        /// Sync messages
        /// </summary>
        public void SyncMessages()
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::SyncMessages", "start"));
            lock (this)
            {
                this.IsMessageSyncComplete = false;

                // kick off a background task to sync the conversation list from server
                // to phone and update the observable collection
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (o, e) =>
                {
                    try
                    {
                        serviceProxy.GetAllMessages(this.MessagesDownloaded);
                    }
                    catch (Exception)
                    {
                        this.IsMessageSyncComplete = true;
                    }
                };

                worker.RunWorkerAsync();
            }
        }

        /// <summary>
        /// Sync full message for this message id. This will bring down hte blob if it was not
        /// downloaded in hte push notification.
        /// </summary>
        /// <param name="MessageId"></param>
        public void SyncMessageId(Guid conversationId, Guid messageId)
        {
            // Do not set IsMessageSyncComplete flag. That flag should be used only
            // For full/delta sync not for individual message sync.
            // Kick off a background task to sync the conversation list from server
            // to phone and update the observable collection
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (o, e) =>
            {
                try
                {
                    serviceProxy.GetFullMessageFromMessageId(this.ExistingMessageDownloaded, conversationId, messageId);
                }
                catch (Exception)
                {
                }
            };

            worker.RunWorkerAsync();
        }

        /// <summary>
        /// Initiates a search for the list of contacts registered with Yapper
        /// </summary>
        public void SyncUsers()
        {
            this.IsUsersSyncComplete = false;
            var contactSearchArgument = new ContactSearchArguments(null, SearchKind.AllPhoneNumbers, FilterKind.None, null);

            contactSearchArgument.SearchCompleted += (s, args) =>
                {
                    List<string> userPhoneNumbers = (List<string>)args.Results;
                    try
                    {
                        this.serviceProxy.GetRegisteredPhoneNumbers(userPhoneNumbers, this.RegisteredPhoneNumbersDownloaded);
                        this.DownloadGroups();
                    }
                    catch (Exception)
                    {
                        this.IsUsersSyncComplete = true;
                    }
                };

            // get all contacts
            this.contactSearchController.StartSearch(contactSearchArgument);
        }

        /// <summary>
        /// Get the list of conversations in the local database
        /// </summary>
        /// <returns></returns>
        public List<ConversationModel> GetConversations()
        {
            lock (this)
            {
                List<ConversationModel> conversations = new List<ConversationModel>();
                var conversationList = from c in this.dataContext.Table<MessageModel>()
                       where c.MessageType == MessageType.Conversation
                       orderby c.LastUpdateTimeUtcTicks descending
                       select c;

                foreach (MessageModel cm in conversationList)
                {
                    if (cm.MessageFlags != 0)
                    {
                        continue;
                    }

                    ConversationModel c = new ConversationModel();
                    c.ConversationId = cm.ConversationId;
                    c.LastPostUtcTime = cm.LastUpdateTime;
                    c.LastPostPreview = cm.TextMessage;
                    c.UnreadCount = cm.UnreadCount.HasValue? cm.UnreadCount.Value : 0;
                    c.ConversationParticipants = new Collection<UserModel>();

                    UserModel senderUser = this.GetUser(cm.SenderId);
                    if (senderUser == null)
                    {
                        senderUser = new UserModel() { Id = cm.SenderId, Name = Strings.UnknownUser };
                    }

                    UserModel recipientUser = this.GetUser(cm.RecipientId);
                    if (senderUser == null)
                    {
                        senderUser = new UserModel() { Id = cm.RecipientId, Name = Strings.UnknownUser };
                    }

                    c.ConversationParticipants.Add(senderUser);
                    c.ConversationParticipants.Add(recipientUser);

                    conversations.Add(c);
                }

                return conversations;
            }
        }

        public int GetUnreadCount(Guid conversationId)
        {
            using (DataContextWrapper<YapperDataContext> context = new DataContextWrapper<YapperDataContext>())
            {
                var conversation = from c in context.Table<MessageModel>()
                                   where c.MessageType == MessageType.Conversation &&
                                         c.ConversationId == conversationId
                                   orderby c.LastUpdateTimeUtcTicks descending
                                   select c;

                if (conversation.Any())
                {
                    MessageModel c = conversation.FirstOrDefault();
                    return (c != null && c.UnreadCount.HasValue) ? c.UnreadCount.Value : 0;
                }

                return 0;
            }
        }

        /// <summary>
        /// Get the list of users from local db
        /// </summary>
        /// <returns></returns>
        public List<UserModel> GetUsers()
        {
            lock (this)
            {
                var dbUsers = from u in this.dataContext.Table<UserModel>()
                              orderby u.Name ascending
                              select u;

                List<UserModel> users = dbUsers.ToList();
                for (int i = 0; i < users.Count; i++)
                {
                    if (!this.userCache.ContainsKey(users[i].Id))
                    {
                        this.userCache.Add(users[i].Id, users[i]);
                    }
                }

                return users;
            }
        }

        /// <summary>
        /// Get the list of users from local db
        /// </summary>
        /// <returns></returns>
        public UserModel GetUser(int userId, IDataContextWrapper context = null)
        {
            if (context == null)
            {
                context = this.dataContext;
            }

            lock (this)
            {
                if (this.userCache.ContainsKey(userId))
                {
                    return this.userCache[userId];
                }

                var dbUsers = from u in context.Table<UserModel>()
                              where u.Id == userId
                              select u;

                if (dbUsers.Count<UserModel>() > 0)
                {
                    UserModel user = dbUsers.FirstOrDefault<UserModel>();
                    this.userCache.Add(user.Id, user);
                    return user;
                }
                
                return null;
            }
        }

        public void SetLastReadTime(Guid conversationId, DateTime time, bool resetUnread)
        {
            lock (this)
            {
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync", "Start update last read time"));

                // save the last read time for conversation
                var conversationQuery = from c in this.dataContext.Table<MessageModel>()
                                   where (c.ConversationId == conversationId && c.MessageType == MessageType.Conversation)
                                   select c;

                if (conversationQuery.Any())
                {
                    MessageModel conversation = conversationQuery.FirstOrDefault();
                    conversation.LastReadTime = time;

                    if (resetUnread)
                    {
                        conversation.UnreadCount = 0;
                    }

                    this.dataContext.SubmitChanges();
                }

                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync", "End update last read time"));
            }
        }

        public void SetLastReadTime(MessageModel message)
        {
            lock (this)
            {
                using (DataContextWrapper<YapperDataContext> context = new DataContextWrapper<YapperDataContext>())
                {
                    Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync", "Start update last read time"));

                    context.Attach<MessageModel>(message);
                    message.LastReadTime = DateTime.Now;
                    context.SubmitChanges();

                    Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync", "End update last read time"));
                }
            }
        }


        /// <summary>
        /// Get the most recent 20 messages in a conversations
        /// </summary>
        /// <param name="conversationId">ConversationId</param>
        /// <returns></returns>
        public IEnumerable<MessageModel> GetMessages(DataContextWrapper<YapperDataContext> context, Guid conversationId, int pageSize = 5)
        {
            lock (this)
            {
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetMessages", "start"));
                var messages = (from message in context.Table<MessageModel>()
                                where message.ConversationId == conversationId
                                orderby message.PostDateTimeUtcTicks descending
                                select message).Take(pageSize);

                Dictionary<int, UserModel> userMessageCache = new Dictionary<int, UserModel>();
                foreach (MessageModel m in messages)
                {
                    UserModel senderUser = this.GetUser(m.SenderId);
                    if (senderUser == null)
                    {
                        senderUser = new UserModel() { Id = m.SenderId, Name = Strings.UnknownUser };
                    }

                    UserModel recipientUser = this.GetUser(m.RecipientId);
                    if (senderUser == null)
                    {
                        senderUser = new UserModel() { Id = m.RecipientId, Name = Strings.UnknownUser };
                    }

                    m.Sender = senderUser;
                    m.Recipient = recipientUser;
                }

                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetMessages", "end"));

                return messages;
            }
        }

        /// <summary>
        /// Get the most recent 20 messages in a conversations
        /// </summary>
        /// <param name="conversationId">ConversationId</param>
        /// <returns></returns>
        public IEnumerable<MessageModel> GetMessagesOlderThan(DataContextWrapper<YapperDataContext> context, Guid conversationId, MessageModel oldMessage, int pageSize = 5)
        {
            lock (this)
            {
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetMessagesOlderThan", "start"));
                var messages = (from message in context.Table<MessageModel>()
                                where (message.ConversationId == conversationId &&
                                       message.PostDateTimeUtcTicks <= oldMessage.PostDateTimeUtcTicks)
                                orderby message.PostDateTimeUtcTicks descending
                                select message).Take(pageSize);

                Dictionary<int, UserModel> userMessageCache = new Dictionary<int, UserModel>();
                foreach (MessageModel m in messages)
                {
                    UserModel senderUser = this.GetUser(m.SenderId);
                    if (senderUser == null)
                    {
                        senderUser = new UserModel() { Id = m.SenderId, Name = Strings.UnknownUser };
                    }

                    UserModel recipientUser = this.GetUser(m.RecipientId);
                    if (senderUser == null)
                    {
                        senderUser = new UserModel() { Id = m.RecipientId, Name = Strings.UnknownUser };
                    }

                    m.Sender = senderUser;
                    m.Recipient = recipientUser;
                }

                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetMessagesOlderThan", "end"));

                return messages;
            }
        }

        /// <summary>
        /// Get the most recent 20 messages in a conversations
        /// </summary>
        /// <param name="conversationId">ConversationId</param>
        /// <returns></returns>
        public List<MessageModel> GetMessagesNewerThan(long sinceUtcTime)
        {
            lock (this)
            {
                using (DataContextWrapper<YapperDataContext> context = new DataContextWrapper<YapperDataContext>())
                {
                    Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetMessagesOlderThan", "start"));
                    var messages = (from message in context.Table<MessageModel>()
                                    where (message.MessageFlags != MessageFlags.Task) &&
                                          (message.MessageFlags != MessageFlags.TaskItem) &&
                                          (message.MessageFlags != MessageFlags.TaskInfo) &&
                                          (message.ClientVisibleTimeTicks > sinceUtcTime)
                                    select message);

                    Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetMessagesOlderThan", "end"));

                    return messages.ToList();
                }
            }
        }


        /// <summary>
        /// Get the most recent 20 messages in a conversations
        /// </summary>
        /// <param name="conversationId">ConversationId</param>
        /// <returns></returns>
        public List<MessageModel> GetAllTasks()
        {
            using (DataContextWrapper<YapperDataContext> context = new DataContextWrapper<YapperDataContext>())
            {
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetAllTasks", "start"));
                var messages = (from message in context.Table<MessageModel>()
                                where (message.MessageType != (long)MessageType.Conversation && message.IsTaskMessage == true)
                                orderby message.PostDateTimeUtcTicks descending
                                select message);

                foreach (MessageModel m in messages)
                {
                    UserModel senderUser = this.GetUser(m.SenderId, context);

                    UserModel recipientUser = this.GetUser(m.RecipientId, context);

                    if (senderUser != null)
                    {
                        m.Sender = senderUser;
                    }

                    if (recipientUser != null)
                    {
                        m.Recipient = recipientUser;
                    }
                }
                
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetAllTasks", "end"));

                return messages.ToList();
            }
        }

        public IEnumerable<MessageModel> GetTaskItems(Guid taskId)
        {
            lock (this)
            {
                using (DataContextWrapper<YapperDataContext> context = new DataContextWrapper<YapperDataContext>())
                {
                    Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetTaskList", "start"));
                    var messages = (from message in context.Table<MessageModel>()
                                    where (((MessageModel)message).PollMessageId == taskId)
                                    select message);

                    foreach (MessageModel m in messages)
                    {
                        UserModel senderUser = this.GetUser(m.SenderId, context);

                        UserModel recipientUser = this.GetUser(m.RecipientId, context);

                        if (senderUser != null)
                        {
                            m.Sender = senderUser;
                        }

                        if (recipientUser != null)
                        {
                            m.Recipient = recipientUser;
                        }
                    }

                    Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetTaskList", "end"));
                    return messages.ToList();
                }
            }
        }

        public List<MessageModel> GetTaskCount(long sinceUtcTicks)
        {
            lock (this)
            {
                using (DataContextWrapper<YapperDataContext> context = new DataContextWrapper<YapperDataContext>())
                {
                    Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetAllTasks", "start"));
                    var messages = (from message in context.Table<MessageModel>()
                                    where (message.MessageType != (long)MessageType.Conversation && (message.MessageFlags & MessageFlags.Task) == MessageFlags.Task &&
                                           message.ClientVisibleTimeTicks > sinceUtcTicks)
                                    select message);

                    return messages.ToList();
                }
            }
        }

        /// <summary>
        /// Get the most recent 20 messages in a conversations
        /// </summary>
        /// <param name="conversationId">ConversationId</param>
        /// <returns></returns>
        public MessageModel GetMessage(Guid messageId)
        {
            lock (this)
            {
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetMessage", "start"));
                var messages = (from message in this.dataContext.Table<MessageModel>()
                                where (message.MessageId == messageId)
                                select message);

                if (messages.Count<MessageModel>() != 0)
                {
                    Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetMessage", "end"));
                    return messages.FirstOrDefault<MessageModel>();
                }

                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetMessage", "end"));
                return null;
            }
        }

        /// <summary>
        /// Get the most recent 20 messages in a conversations
        /// </summary>
        /// <param name="conversationId">ConversationId</param>
        /// <returns></returns>
        public MessageModel GetMessageFromClientId(Guid messageId, DataContextWrapper<YapperDataContext> context = null)
        {
            bool shouldDispose = false;
            if (context == null)
            {
                shouldDispose = true;
                context = new DataContextWrapper<YapperDataContext>();
            }

            try
            {
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetMessage", "start"));
                var messages = (from message in context.Table<MessageModel>()
                                where (message.ClientMessageId == messageId)
                                select message);

                if (messages.Count<MessageModel>() != 0)
                {
                    Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetMessage", "end"));
                    MessageModel message = messages.FirstOrDefault<MessageModel>();
                    if (message != null)
                    {
                        message.Sender = this.GetUser(message.SenderId);
                        message.Recipient = this.GetUser(message.RecipientId);
                    }

                    return message;
                }

                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetMessage", "end"));
                return null;
            }
            finally
            {
                if (shouldDispose)
                {
                    context.Dispose();
                }
            }
        }

        /// <summary>
        /// Get the most recent 20 messages in a conversations
        /// </summary>
        /// <param name="conversationId">ConversationId</param>
        /// <returns></returns>
        public IEnumerable<MessageModel> GetUnsentMessages()
        {
            lock (this)
            {
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetMessage", "start"));
                var messages = (from message in this.dataContext.Table<MessageModel>()
                                where (message.MessageId == Guid.Empty &&
                                       message.MessageType == MessageType.Message)
                                select message);

                foreach (MessageModel m in messages)
                {
                    UserModel senderUser = this.GetUser(m.SenderId);
                    if (senderUser == null)
                    {
                        senderUser = new UserModel() { Id = m.SenderId, Name = Strings.UnknownUser };
                    }

                    UserModel recipientUser = this.GetUser(m.RecipientId);
                    if (senderUser == null)
                    {
                        senderUser = new UserModel() { Id = m.RecipientId, Name = Strings.UnknownUser };
                    }

                    m.Sender = senderUser;
                    m.Recipient = recipientUser;
                }

                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetMessages", "end"));

                return messages.ToList();
            }
        }

        public IEnumerable<MessageModel> GetPollResponses(Guid pollMessageId, Guid clientMessageId)
        {
            lock (this)
            {
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetPollResponses", "start"));
                var messages = (from message in this.dataContext.Table<MessageModel>()
                                where (((MessageModel)message).PollMessageId == pollMessageId ||
                                      ((MessageModel)message).PollClientMessageId == clientMessageId)
                                select message);

                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetPollResponses", "end"));
                return messages;
            }
        }

        public MessageModel GetPollResponse(Guid pollMessageId, int userId)
        {
            lock (this)
            {
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetPollResponse", "start"));
                var messages = (from message in this.dataContext.Table<MessageModel>()
                                where ((message.IsPollResponseMessage == true) &&
                                       ((MessageModel)message).PollMessageId == pollMessageId &&
                                       message.SenderId == userId)
                                select message);

                if (messages.Count<MessageModel>() != 0)
                {
                    Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetPollResponse", "end"));
                    return (MessageModel)messages.FirstOrDefault<MessageModel>();
                }

                return null;
            } 
        }

        public IEnumerable<UserModel> GetGroups()
        {
            lock (this)
            {
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetGroups", "start"));
                var savedGroups = from g in this.dataContext.Table<UserModel>()
                                  orderby g.Name
                                  where g.UserType == UserType.Group
                                  select g;

                foreach (UserModel u in savedGroups)
                {
                    var members = from m in this.dataContext.Table<GroupMemberModel>()
                                  where m.GroupId == u.Id
                                  select m;

                    List<UserModel> membersList = new List<UserModel>();
                    foreach (GroupMemberModel m in members)
                    {
                        membersList.Add(this.GetUser(m.MemberId));
                    }

                    ((GroupModel)u).Members = membersList;
                }

                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetGroups", "end"));
                return savedGroups.ToList();
            }
        }

        public List<UserModel> GetGroupNonMembers(UserModel g)
        {
            if (g.UserType != UserType.Group)
            {
                return null;
            }

            lock (this)
            {
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetGroupNonMembers", "start"));
                var allcontacts = from u in this.dataContext.Table<UserModel>()
                              where u.UserType == UserType.User
                              orderby u.Id
                              select u;

                var allmembers= from m in this.dataContext.Table<GroupMemberModel>()
                                  where m.GroupId == g.Id
                                  orderby m.MemberId
                                  select m;

                List<UserModel> nonmembers = new List<UserModel>();
                foreach (var u in allcontacts)
                {
                    bool found = false;
                    foreach (var m in allmembers)
                    {
                        if (m.MemberId == u.Id)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        nonmembers.Add(u);
                    }
                }

                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetGroupNonMembers", "end"));
                return nonmembers;
            }
        }


        public GroupModel GetGroup(int groupId)
        {
            lock (this)
            {
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetGroup", "start"));

                // save the last read time for conversation
                var savedGroup = from g in this.dataContext.Table<UserModel>()
                                  where g.Id == groupId
                                  select g;

                if (savedGroup.Count<UserModel>() == 0)
                {
                    return null;
                }

                GroupModel group = savedGroup.First() as GroupModel;

                if (group == null)
                {
                    return null;
                }

                var members = from m in this.dataContext.Table<GroupMemberModel>()
                              where m.GroupId == savedGroup.First().Id
                              select m;

                List<UserModel> membersList = new List<UserModel>();
                foreach (GroupMemberModel m in members)
                {
                    membersList.Add(this.GetUser(m.MemberId));
                }

                group.Members = membersList;

                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::GetGroup", "end"));
                return group;
            }
        }

        /// <summary>
        /// Send new message
        /// </summary>
        public void SendMessage(MessageModel newMessageToSend)
        {
            Messenger.Default.Send<NewMessageSavedEvent>(new NewMessageSavedEvent() { Message = newMessageToSend });
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (o, f) =>
            {
                if (!newMessageToSend.IsTaskMessage.Value && !newMessageToSend.IsTaskItemMessage)
                {
                    InsertMessageToDBBeforeSending(newMessageToSend);
                }

                // If poll message has not been sent, don't send the poll response
                if (!newMessageToSend.IsPollResponseMessage || (newMessageToSend.PollMessageId.Value != Guid.Empty))
                {
                    this.serviceProxy.SendNewMessage(newMessageToSend.EncryptMessage(), this.NewMessageCreated);
                }
            };

            worker.RunWorkerAsync();
        }

        /// <summary>
        /// Send new message
        /// </summary>
        public MessageModel CreateTask(MessageModel message)
        {
            if (!message.IsTaskMessage.Value && !message.IsTaskItemMessage)
            {
                return null;
            }

            using (DataContextWrapper<YapperDataContext> context = new DataContextWrapper<YapperDataContext>())
            {
                context.Table<MessageModel>().InsertOnSubmit(message);

                context.SubmitChanges();
                Messenger.Default.Send<NewTaskSavedEvent>(new NewTaskSavedEvent() { Task = message });

                return message;
            }
        }

        public void SetTaskName(MessageModel message, string name, Guid parentId)
        {
            lock (this)
            {
                using (DataContextWrapper<YapperDataContext> context = new DataContextWrapper<YapperDataContext>())
                {
                    int i = 5;
                    while (i > 0)
                    {
                        try
                        {
                            MessageModel m = this.GetMessageFromClientId(message.ClientMessageId, context);
                            if (m == null)
                            {
                                context.InsertOnSubmit<MessageModel>(message);
                                m = message;
                            }
                            else if (!string.IsNullOrEmpty(m.TaskName))
                            {
                                m.LastTaskUpdaterId = UserSettingsModel.Instance.Me.Id;
                            }

                            m.TaskName = name;
                            m.PollMessageId = parentId;
                            m.ItemOrder = message.ItemOrder;
                            m.LastUpdateTime = DateTime.Now;
                            context.SubmitChanges();
                            break;
                        }
                        catch (ChangeConflictException ce)
                        {
                        }

                        --i;
                    }
                }
            }
        }

        public void SetTaskCompleted(Guid clientId)
        {
            lock (this)
            {
                using (DataContextWrapper<YapperDataContext> context = new DataContextWrapper<YapperDataContext>())
                {
                    int i = 5;
                    while (i > 0)
                    {
                        try
                        {
                            MessageModel m = this.GetMessageFromClientId(clientId, context);
                            m.IsCompleted = true;
                            m.LastTaskUpdaterId = this.userSettings.Me.Id;
                            context.SubmitChanges();
                            break;
                        }
                        catch (ChangeConflictException ce)
                        {
                        }

                        --i;
                    }
                }
            }
        }


        public void InsertMessageToDBBeforeSending(MessageModel message)
        {
            lock (this)
            {
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::InsertMessageToDBBeforeSending", "start"));

                if (message.IsImage)
                {
                    this.WriteImageToIsoStorage(message.MessageId, message.Image);
                }

                if (message.IsPollResponseMessage)
                {
                    MessageModel pollMessage = this.GetMessage(message.PollMessageId.Value);
                    pollMessage.MyPollResponse = message.PollResponse;
                }

                // Write the message
                this.dataContext.Table<MessageModel>().InsertOnSubmit(message);

                this.dataContext.SubmitChanges();
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::InsertMessageToDBBeforeSending", "end"));

                (Application.Current as App).PerfTrackerString += "\nQuick insert:" + (Application.Current as App).PerfTrackerStopWatch.ElapsedMilliseconds.ToString();
            }
        }

        /// <summary>
        /// Send new message
        /// </summary>
        public void PostPollResponse(MessageModel pollResponse)
        {
            if (pollResponse.PollMessageId == Guid.Empty && 
                pollResponse.PollClientMessageId.HasValue && 
                pollResponse.PollClientMessageId.Value == Guid.Empty)
            {
                MessageModel m = this.GetMessageFromClientId(pollResponse.PollClientMessageId.Value);
                if (m != null)
                {
                    pollResponse.PollMessageId = m.MessageId;
                }
            }

            this.SendMessage(pollResponse);
        }

        public void DownloadTasks()
        {
            this.SyncMessages();
        }

        public void DownloadGroups()
        {
            YapperServiceProxy.Instance.GetGroups(this.GroupsDownloaded);
        }

        public void CreateGroup(string name, List<UserModel> members)
        {
            YapperServiceProxy.Instance.CreateGroup(name, members, this.GroupCreated);
        }

        public void AddGroupMember(GroupModel group, UserModel user)
        {
            YapperServiceProxy.Instance.AddGroupMember(group, user, this.AddGroupMemberCompleted);
        }

        public void RemoveGroupMember(GroupModel group, UserModel user)
        {
            YapperServiceProxy.Instance.RemoveGroupMember(group, user, this.RemoveGroupMemberCompleted);
        }

        private void HandlePushNotificationEvent(PushNotificationEvent pushEvent)
        {
            lock (this)
            {
                this.SyncMessageId(pushEvent.ConversationId, pushEvent.MessageId);
            }
        }

        /// <summary>
        /// Callback invoked when the API call ends.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MessagesDownloaded(List<MessageModel> result)
        {
            long utcTicks = DateTime.UtcNow.Ticks;
            if (result != null)
            {
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::MessagesDownloaded", "start"));

                List<MessageModel> decryptedMessages = new List<MessageModel>();
                foreach (MessageModel encryptedMessage in result)
                {
                    MessageModel message = null;

                    try
                    {
                        message = encryptedMessage.DecryptMessage();
                    }
                    catch (Exception)
                    {
                        message = null;
                    }

                    if (message == null)
                    {
                        continue;
                    }

                    // Set the message properties
                    // Set the sender id and recipient id
                    // Clear the image blob
                    // set the last read time.
                    encryptedMessage.CopyNonEncryptedProperties(message);
                    message.ClientVisibleTimeTicks = utcTicks;
                    decryptedMessages.Add(message);
                }

                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::MessagesDownloaded encryption", "end"));
                var deltaSync = IsolatedStorageSettings.ApplicationSettings.Contains("LastSyncDateTime");

                if (deltaSync)
                {
                    Messenger.Default.Send<SyncEvent>(new SyncEvent() { SyncState = SyncState.Complete, Messages = decryptedMessages });
                }

                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::MessagesDownloaded Write", "start"));
                List<UserModel> users = new List<UserModel>();
                List<MessageModel> conversations = new List<MessageModel>();
                List<MessageModel> pollResponseMessages = new List<MessageModel>();
                List<MessageModel> taskMessages = new List<MessageModel>();
                foreach (MessageModel message in decryptedMessages)
                {
                    if (message.IsPollResponseMessage)
                    {
                        pollResponseMessages.Add(message);
                    }
                    else if (message.IsTaskMessage.Value)
                    {
                        taskMessages.Add(message);
                    }
                    else
                    {
                        this.SaveSingleMessage(message, users);
                    }

                }

                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::MessagesDownloaded Write", "end"));

                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::MessagesDownloaded Poll response write", "start"));
                // Process poll response messages
                foreach (MessageModel message in pollResponseMessages)
                {
                    this.SavePollResponse(message);
                }

                this.SaveAllTaskMessages(taskMessages, users);

                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::MessagesDownloaded Poll response write", "end"));
                if (result.Count != 0)
                {
                    UserSettingsModel.Instance.LastSyncDateTime = result.First().LastUpdateTime;
                }
            }

            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::MessagesDownloaded", "end"));
            this.IsMessageSyncComplete = true;
            if (this.IsUsersSyncComplete)
            {
                Messenger.Default.Send<SyncEvent>(new SyncEvent() { SyncState = SyncState.Complete });
            }
        }

        private void SaveSingleMessage(MessageModel message, List<UserModel> users, IDataContextWrapper context = null)
        {
            if (context == null)
            {
                context = this.dataContext;
            }

            lock (this)
            {
                try
                {
                    if (message.IsImage)
                    {
                        this.WriteImageToIsoStorage(message.MessageId, message.Image);
                    }

                    // Add the sender/recipient if they are not inthe contact list already
                    if (!this.ContainsContact(message.Sender) && !users.Contains(message.Sender))
                    {
                        this.AddContact(message.Sender, users);
                        users.Add(message.Sender);
                    }

                    if (!this.ContainsContact(message.Recipient) && !users.Contains(message.Recipient))
                    {
                        this.AddContact(message.Recipient, users);
                        users.Add(message.Recipient);
                    }

                    this.UpdateConversation(message, context);
                    Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::MessagesDownloaded", "end find existing conversation"));

                    Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::MessagesDownloaded", "start find existing message"));
                    // If the message already exists, delete it and reinsert it
                    // The reason is that the server version is more authoritative.
                    IQueryable<MessageModel> existingMessages = from m in context.Table<MessageModel>()
                                                                where
                                                                (m.ConversationId == message.ConversationId && m.MessageId == message.MessageId) ||
                                                                (m.ClientMessageId == message.ClientMessageId && m.MessageId == Guid.Empty)
                                                                select m;

                    // Delete the message only if it's a quick send message
                    // otherwise update it
                    MessageModel existingMessage = existingMessages.FirstOrDefault<MessageModel>();
                    if (existingMessage != null && existingMessage.MessageId == Guid.Empty)
                    {
                        message.LastReadTime = existingMessage.LastReadTime;
                        if (message.IsPollMessage)
                        {
                            message.MyPollResponse = existingMessage.MyPollResponse;
                        }

                        message.ClientVisibleTimeTicks = existingMessage.ClientVisibleTimeTicks;
                        context.DeleteOnSubmit(existingMessage);
                        existingMessage = null;
                    }

                    Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::MessagesDownloaded", "end find existing message"));

                    // Insert it only if the existing message is null
                    if (existingMessage == null)
                    {
                        context.InsertOnSubmit<MessageModel>(message);
                    }

                    context.SubmitChanges();
                }
                catch (Exception ex)
                {
                    this.dataContext.Dispose();
                    this.dataContext = new DataContextWrapper<YapperDataContext>();
                }
            }
        }

        private void SaveAllTaskMessages(List<MessageModel> taskMessages, List<UserModel> users)
        {
            List<MessageModel> toRemove = new List<MessageModel>();
            List<MessageModel> deletedMessage = new List<MessageModel>();
            foreach (MessageModel message in taskMessages)
            {
                if (message.IsTaskDeleted.HasValue && message.IsTaskDeleted.Value)
                {
                    List<MessageModel> original = taskMessages.FindAll((m) => { return m.ClientMessageId == message.ClientMessageId; });

                    if (original != null)
                    {
                        toRemove.AddRange(original);
                    }

                    deletedMessage.Add(message);
                }
            }

            foreach (MessageModel deleted in toRemove)
            {
                for (int i = taskMessages.Count - 1; i >= 0; i--)
                {
                    if (deleted.Equals(taskMessages[i]))
                    {
                        if (deleted.IsTaskDeleted == taskMessages[i].IsTaskDeleted)
                        {
                            taskMessages.Remove(taskMessages[i]);
                        }
                    }
                }
            }

            taskMessages.AddRange(deletedMessage);

            taskMessages.Sort(new Comparison<MessageModel>((x, y) => { return (int)(x.PostDateTimeUtcTicks - y.PostDateTimeUtcTicks); }));

            foreach (MessageModel message in taskMessages)
            {
                this.SaveSingleTaskMessage(message, users);
            }
        }

        private void SaveSingleTaskMessage(MessageModel message, List<UserModel> users, IDataContextWrapper context = null)
        {
            if (context == null)
            {
                context = this.dataContext;
            }

            MessageModel taskInfoMessage = null;
            lock (this)
            {
                try
                {
                    if (!message.IsTaskMessage.Value)
                    {
                        return;
                    }

                    // Add the sender/recipient if they are not inthe contact list already
                    if (!this.ContainsContact(message.Sender) && !users.Contains(message.Sender))
                    {
                        this.AddContact(message.Sender, users);
                        users.Add(message.Sender);
                    }

                    if (!this.ContainsContact(message.Recipient) && !users.Contains(message.Recipient))
                    {
                        this.AddContact(message.Recipient, users);
                        users.Add(message.Recipient);
                    }

                    Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::MessagesDownloaded", "end find existing conversation"));

                    Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::MessagesDownloaded", "start find existing message"));


                    // Task exists
                    // Task exists and deleted
                    // Task doesn't exist
                    // If the message already exists, delete it and reinsert it
                    // The reason is that the server version is more authoritative.
                    IQueryable<MessageModel> existingInfoMessageQuery = from m in context.Table<MessageModel>()
                                                                        where
                                                                        (m.TaskId == message.ClientMessageId)
                                                                        select m;
                    taskInfoMessage = existingInfoMessageQuery.FirstOrDefault<MessageModel>();
                    bool shouldInsertTaskInfoMessage = false;
                    if (taskInfoMessage == null)
                    {
                        shouldInsertTaskInfoMessage = true;
                        taskInfoMessage = new MessageModel();
                        taskInfoMessage.ConversationId = message.ConversationId;
                        taskInfoMessage.MessageId = message.MessageId;
                        taskInfoMessage.ClientMessageId = Guid.NewGuid();
                        taskInfoMessage.Sender = message.Sender;
                        taskInfoMessage.Recipient = message.Recipient;
                        taskInfoMessage.SenderId = taskInfoMessage.Sender.Id;
                        taskInfoMessage.RecipientId = taskInfoMessage.Recipient.Id;
                        taskInfoMessage.MessageType = MessageType.Message;
                        taskInfoMessage.MessageFlags = MessageFlags.TaskInfo;
                        taskInfoMessage.TaskId = message.ClientMessageId;
                    }

                    taskInfoMessage.PostDateTimeUtcTicks = message.PostDateTimeUtcTicks;
                    taskInfoMessage.ClientVisibleTimeTicks = DateTime.UtcNow.Ticks;
                    taskInfoMessage.LastReadTime = new DateTime(1970, 1, 1);
                    taskInfoMessage.LastUpdateTime = DateTime.Now;

                    // If the message already exists, delete it and reinsert it
                    // The reason is that the server version is more authoritative.
                    IQueryable<MessageModel> existingMessages = from m in context.Table<MessageModel>()
                                                                where
                                                                (m.ClientMessageId == message.ClientMessageId)
                                                                select m;
                    MessageModel existingMessage = existingMessages.FirstOrDefault<MessageModel>();

                    if (existingMessage != null && (!message.IsTaskDeleted.HasValue || !message.IsTaskDeleted.Value))
                    {
                        if (existingMessage.PostDateTimeUtcTicks > message.PostDateTimeUtcTicks)
                        {
                            return;
                        }

                        existingMessage.PostDateTimeUtcTicks = message.PostDateTimeUtcTicks - 1;
                        if (message.Sender.Id != this.userSettings.Me.Id)
                        {
                            existingMessage.ClientVisibleTimeTicks = DateTime.UtcNow.Ticks;
                        }

                        if (message.Sender.Id == this.userSettings.Me.Id)
                        {
                            existingMessage.LastReadTime = (new DateTime(message.PostDateTimeUtcTicks, DateTimeKind.Utc)).ToLocalTime();
                        }

                        existingMessage.TaskName = message.TaskName;
                        //existingMessage.IsCompleted = message.IsCompleted;
                        if (existingMessage.RecipientId == 0)
                        {
                            existingMessage.RecipientId = message.RecipientId;
                        }

                        existingMessage.LastTaskUpdaterId = message.SenderId;

                        taskInfoMessage.TextMessage = taskInfoMessage.SenderId == UserSettingsModel.Instance.Me.Id ? string.Format(Strings.YouUpdatedTask, message.TaskName, message.Sender.Name) : string.Format(Strings.HasUpdatedTask, message.Sender.Name, message.TaskName);

                        foreach (var m in message.TaskItemList)
                        {
                            IQueryable<MessageModel> existingTaskItemQuery = from item in context.Table<MessageModel>()
                                                                             where
                                                                             (item.ClientMessageId == m.ClientMessageId)
                                                                             select item;

                            MessageModel existingTaskItem = existingTaskItemQuery.FirstOrDefault<MessageModel>();
                            if (m.IsTaskDeleted.HasValue && m.IsTaskDeleted.Value)
                            {
                                if (existingTaskItem != null)
                                {
                                    context.DeleteOnSubmit<MessageModel>(existingTaskItem);
                                }

                                continue;
                            }

                            if (existingTaskItem == null)
                            {
                                m.LastReadTime = new DateTime(1970, 1, 1);
                                m.PollMessageId = message.ClientMessageId;
                                m.MessageType = MessageType.Message;
                                context.InsertOnSubmit<MessageModel>(m);
                                continue;
                            }

                            existingTaskItem.ClientVisibleTimeTicks = DateTime.UtcNow.Ticks;
                            if (0 != string.Compare(existingTaskItem.TaskName, m.TaskName, StringComparison.OrdinalIgnoreCase) ||
                                existingTaskItem.IsCompleted != m.IsCompleted)
                            {
                                existingTaskItem.TaskName = m.TaskName;
                                existingTaskItem.IsCompleted = m.IsCompleted;
                                existingTaskItem.LastTaskUpdaterId = message.SenderId;
                            }

                            if (0 != StringComparer.OrdinalIgnoreCase.Compare(existingTaskItem.ItemOrder, m.ItemOrder))
                            {
                                existingTaskItem.ItemOrder = m.ItemOrder;
                            }
                        }
                    }
                    else if (existingMessage != null)
                    {
                        if (existingMessage.TaskItemList != null)
                        {
                            foreach (var m in existingMessage.TaskItemList)
                            {
                                context.Attach<MessageModel>(m);
                                context.DeleteOnSubmit<MessageModel>(m);
                            }
                        }

                        context.DeleteOnSubmit<MessageModel>(existingMessage);

                        taskInfoMessage.TextMessage = taskInfoMessage.SenderId == UserSettingsModel.Instance.Me.Id ? string.Format(Strings.YouDeletedTask, message.TaskName, message.Sender.Name) : string.Format(Strings.HasDeletedTask, message.Sender.Name, existingMessage.TaskName, message.TaskName);
                    }
                    else if (existingMessage == null && message.IsTaskDeleted.HasValue && message.IsTaskDeleted.Value)
                    {
                        taskInfoMessage.TextMessage = taskInfoMessage.SenderId == UserSettingsModel.Instance.Me.Id ? string.Format(Strings.YouDeletedTask, message.TaskName, message.Sender.Name) : string.Format(Strings.HasDeletedTask, message.Sender.Name, existingMessage.TaskName, message.TaskName);
                    }
                    else if (existingMessage == null && (!message.IsTaskDeleted.HasValue || !message.IsTaskDeleted.Value))
                    {
                        message.ClientVisibleTimeTicks = DateTime.UtcNow.Ticks;
                        message.ConversationId = Guid.Empty;
                        message.MessageId = Guid.NewGuid();
                        message.LastReadTime = new DateTime(1970, 1, 1);

                        context.InsertOnSubmit<MessageModel>(message);
                        if (message.TaskItemList != null)
                        {
                            foreach (var m in message.TaskItemList)
                            {
                                if (m.ClientMessageId != Guid.Parse("bbaf9b96-b875-4a23-97ac-77912ca74832"))
                                {
                                    m.LastReadTime = new DateTime(1970, 1, 1);
                                    context.InsertOnSubmit<MessageModel>(m);
                                }
                            }
                        }

                        taskInfoMessage.TextMessage = taskInfoMessage.SenderId == UserSettingsModel.Instance.Me.Id ? string.Format(Strings.YouSharedTask, message.TaskName, message.Sender.Name) : string.Format(Strings.HasSharedTask, message.Sender.Name, message.TaskName);
                    }

                    if (shouldInsertTaskInfoMessage)
                    {
                        //context.InsertOnSubmit<MessageModel>(taskInfoMessage);
                        //this.UpdateConversation(taskInfoMessage, context);
                    }

                    Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::MessagesDownloaded", "end find existing message"));

                    context.SubmitChanges();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "Error in DataSync::SaveSingleTaskMessage for task ", message.TaskName));
                    this.dataContext.Dispose();
                    this.dataContext = new DataContextWrapper<YapperDataContext>();
                }
            }

            Messenger.Default.Send<NewMessageEvent>(new NewMessageEvent() { Message = taskInfoMessage, IsPush = false });
        }

        private void SavePollResponse(MessageModel message, IDataContextWrapper context = null)
        {
            if (context == null)
            {
                context = this.dataContext;
            }

            lock (this)
            {
                try
                {
                    // If this is a poll response message, update the last update time for the poll message
                    if (message.IsPollResponseMessage && message.IsMine && message.PollMessageId.HasValue)
                    {
                        Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::MessagesDownloaded", "start update my poll response"));
                        MessageModel pollMessage = this.GetMessage(message.PollMessageId.Value);

                        if (pollMessage != null && pollMessage.LastUpdateTime < message.LastUpdateTime)
                        {
                            pollMessage.LastUpdateTime = message.LastUpdateTime;
                        }

                        if (pollMessage != null)
                        {
                            pollMessage.MyPollResponse = message.PollResponse;
                        }

                        Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::MessagesDownloaded", "start update my poll response"));
                    }

                    if (message.IsPollResponseMessage && !message.IsGroup && !message.IsMine && message.PollMessageId.HasValue)
                    {
                        Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::MessagesDownloaded", "start update other poll response"));
                        MessageModel pollMessage = this.GetMessage(message.PollMessageId.Value);

                        if (pollMessage != null && pollMessage.LastUpdateTime < message.LastUpdateTime)
                        {
                            pollMessage.LastUpdateTime = message.LastUpdateTime;
                        }

                        if (pollMessage != null)
                        {
                            pollMessage.OtherPollResponse = message.PollResponse;
                        }

                        Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::MessagesDownloaded", "start update other poll response"));
                    }

                    this.UpdateConversation(message, context);

                    var existingMessages = from m in context.Table<MessageModel>()
                                           where
                                           (m.ConversationId == message.ConversationId && m.MessageId == message.MessageId) ||
                                           (m.ConversationId == message.ConversationId && m.ClientMessageId == message.ClientMessageId && m.MessageId == Guid.Empty)
                                           select m;

                    // Delete the message only if it's a quick send message
                    // otherwise update it
                    MessageModel existingMessage = existingMessages.FirstOrDefault<MessageModel>();

                    if (existingMessage != null && existingMessage.MessageId == Guid.Empty)
                    {
                        message.LastReadTime = existingMessage.LastReadTime;
                        message.ClientVisibleTimeTicks = existingMessage.ClientVisibleTimeTicks;
                        context.DeleteOnSubmit(existingMessage);
                        existingMessage = null;
                    }

                    if (existingMessage == null)
                    {
                        context.InsertOnSubmit<MessageModel>(message);
                    }

                    context.SubmitChanges();
                }
                catch (Exception ex)
                {
                    this.dataContext.Dispose();
                    this.dataContext = new DataContextWrapper<YapperDataContext>();
                }
            }
        }

        private void UpdateConversation(MessageModel message, IDataContextWrapper context)
        {
            // Update the conversation object corresponding to the message
            MessageModel existingConversation = null;
            IQueryable<MessageModel> existingConversationIQueryable = null;

            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::MessagesDownloaded", "start find existing conversation"));

            if (message.ConversationId != Guid.Empty && existingConversation == null)
            {
                existingConversationIQueryable = from m in context.Table<MessageModel>()
                                                 where m.ConversationId == message.ConversationId && m.MessageType == MessageType.Conversation
                                                 select m;

                existingConversation = existingConversationIQueryable.FirstOrDefault<MessageModel>();

                if (existingConversation != null)
                {
                    if (existingConversation.LastUpdateTime < message.LastUpdateTime)
                    {
                        existingConversation.LastUpdateTime = message.LastUpdateTime;
                        existingConversation.TextMessage = message.TextMessage;
                    }

                    if (existingConversation.LastReadTime.Ticks < message.LastUpdateTime.Ticks)
                    {
                        if (!existingConversation.UnreadCount.HasValue || message.Sender.Id == this.userSettings.Me.Id)
                        {
                            existingConversation.UnreadCount = 0;
                        }
                        else
                        {
                            existingConversation.UnreadCount = existingConversation.UnreadCount.Value + 1;
                        }
                    }
                }
                else
                {
                    existingConversation = new MessageModel(message);
                    existingConversation.MessageId = Guid.Empty;
                    existingConversation.ClientMessageId = Guid.Empty;
                    existingConversation.MessageType = MessageType.Conversation;
                    existingConversation.MessageFlags = 0;
                    existingConversation.Image = null;
                    existingConversation.LastUpdateTime = message.LastUpdateTime;
                    existingConversation.TextMessage = message.TextMessage;
                    existingConversation.LastReadTime = new DateTime(1970, 1, 1);
                    existingConversation.UnreadCount = 1;

                    context.InsertOnSubmit<MessageModel>(existingConversation);
                }
            }
        }

        /// <summary>
        /// Callback invoked when the API call ends.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RegisteredPhoneNumbersDownloaded(List<UserModel> tempItems)
        {
            lock (this)
            {
                try
                {
                    Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::RegisteredPhoneNumbersDownloaded", "start"));
                    if (tempItems != null)
                    {
                        foreach (UserModel user in tempItems)
                        {
                            this.AddContact(user, null, true);
                        }

                        this.dataContext.SubmitChanges();
                    }

                    this.IsUsersSyncComplete = true;
                }
                catch (Exception)
                {
                    this.IsUsersSyncComplete = true;
                }
            }

            Messenger.Default.Send<NewContactEvent>(null);

            if (this.IsMessageSyncComplete)
            {
                Messenger.Default.Send<SyncEvent>(new SyncEvent() { SyncState = SyncState.Complete });
            }

            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::RegisteredPhoneNumbersDownloaded", "end"));
        }

        private void GroupsDownloaded(List<GroupModel> groups)
        {
            if (groups != null)
            {
                lock (this)
                {
                    List<UserModel> users = new List<UserModel>();
                    for (int i = 0; i < groups.Count; i++)
                    {
                        this.AddOrUpdateGroup(groups[i], users);
                        this.dataContext.SubmitChanges();
                    }
                }
            }

            this.RemoveGroups(groups);

            Messenger.Default.Send<RefreshGroupsEvent>(new RefreshGroupsEvent());
        }

        private void RemoveGroups(List<GroupModel> groups)
        {
            if (groups == null)
            {
                return;
            }

            IEnumerable<UserModel> existing = this.GetGroups();

            foreach (var g in existing)
            {
                if (!groups.Contains(g))
                {
                    lock (this)
                    {
                        var member = from m in this.dataContext.Table<GroupMemberModel>()
                                     where m.GroupId == g.Id && m.MemberId == UserSettingsModel.Instance.Me.Id
                                       select m;

                        if (member.Count<GroupMemberModel>() != 0)
                        {
                            this.dataContext.Table<GroupMemberModel>().DeleteOnSubmit(member.First());
                        }

                        this.dataContext.SubmitChanges();
                    }
                }
            }
        }

        private void GroupCreated(GroupModel createdGroup)
        {
            if (createdGroup != null)
            {
                lock (this)
                {
                    this.AddOrUpdateGroup(createdGroup, null);
                    this.dataContext.SubmitChanges();
                }
            }

            Messenger.Default.Send<NewGroupEvent>(new NewGroupEvent() { GroupCreated = createdGroup});
        }

        private void AddGroupMemberCompleted(GroupModel group, UserModel user)
        {
            if (user != null)
            {
                lock (this)
                {
                    this.dataContext.Table<GroupMemberModel>().InsertOnSubmit(new GroupMemberModel() { GroupId = group.Id, MemberId = user.Id });
                    this.dataContext.SubmitChanges();
                }
            }

            Messenger.Default.Send<GroupMemberAddedEvent>(new GroupMemberAddedEvent() { Success = user != null, User = user });
        }

        private void RemoveGroupMemberCompleted(GroupModel gp, UserModel user)
        {
            if (user != null)
            {
                lock (this)
                {
                    var existing = from m in this.dataContext.Table<GroupMemberModel>()
                                   where m.GroupId == gp.Id && m.MemberId == user.Id
                                   select m;

                    if (existing.Count<GroupMemberModel>() != 0)
                    {
                        this.dataContext.Table<GroupMemberModel>().DeleteOnSubmit(existing.First());
                    }
                    
                    this.dataContext.SubmitChanges();
                }
            }

            Messenger.Default.Send<GroupMemberRemovedEvent>(new GroupMemberRemovedEvent() { Success = user != null, User = user });
        }

        private void AddOrUpdateGroup(GroupModel createdGroup, List<UserModel> users)
        {
            lock (this)
            {
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::AddOrUpdateGroup", "start"));
                createdGroup.UserType = UserType.Group;
                var existing = from u in this.dataContext.Table<UserModel>()
                               where u.Id == createdGroup.Id
                               select u;

                // If the group doesn't exist add it
                if (existing.Count<UserModel>() == 0)
                {
                    this.dataContext.Table<UserModel>().InsertOnSubmit(createdGroup);
                }
                else if (existing.First().UserType == UserType.User)
                {
                    // IF the group exists but is of wrong type, fix it.
                    existing.First().UserType = UserType.Group;
                }

                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::AddOrUpdateGroup", "start find members"));
                var existingMember = from member in this.dataContext.Table<GroupMemberModel>()
                                     where member.GroupId == createdGroup.Id
                                     select member;
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::AddOrUpdateGroup", "end find members"));

                Dictionary<int, GroupMemberModel> members = new Dictionary<int, GroupMemberModel>();

                // Get existing member list
                foreach (GroupMemberModel g in existingMember)
                {
                    members.Add(g.MemberId, g);
                }

                // Add or remove members
                for (int i = 0; i < createdGroup.Members.Count; i++)
                {
                    if (createdGroup.Members[i] == null)
                    {
                        continue;
                    }

                    // Add new members
                    if (!members.ContainsKey(createdGroup.Members[i].Id))
                    {
                        Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::AddOrUpdateGroup", "start find new member in user table"));
                        var existingMemberInUserTable = from u in this.dataContext.Table<UserModel>()
                                                        where u.Id == createdGroup.Members[i].Id
                                                        select u;
                        Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::AddOrUpdateGroup", "end find new member in user table"));

                        // If the group doesn't exist add it
                        if (existingMemberInUserTable.Count<UserModel>() == 0 && 
                            users != null && 
                            !users.Contains(createdGroup.Members[i]))
                        {
                            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::AddOrUpdateGroup", "start add new member to user table"));

                            this.dataContext.Table<UserModel>().InsertOnSubmit(
                                new UserModel()
                                {
                                    Id = createdGroup.Members[i].Id,
                                    Name = createdGroup.Members[i].Name,
                                    UserType = createdGroup.Members[i].UserType,
                                    PhoneNumber = createdGroup.Members[i].PhoneNumber,
                                    PublicKey = createdGroup.Members[i].PublicKey
                                });
                            users.Add(createdGroup.Members[i]);

                            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::AddOrUpdateGroup", "end add new member to user table"));
                        }

                        this.dataContext.Table<GroupMemberModel>().InsertOnSubmit(new GroupMemberModel() { GroupId = createdGroup.Id, MemberId = createdGroup.Members[i].Id });
                    }
                    else
                    {
                        members.Remove(createdGroup.Members[i].Id);
                    }
                }

                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::AddOrUpdateGroup", "start remove members"));
                // Delete existing members that are no longer members.
                foreach (int m in members.Keys)
                {
                    this.dataContext.Table<GroupMemberModel>().DeleteOnSubmit(members[m]);
                }

                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::AddOrUpdateGroup", "end remove members"));

                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::AddOrUpdateGroup", "end"));
            }
        }

        private void AddContact(UserModel contact, List<UserModel> users, bool updateContact = false)
        {
            if (contact is GroupModel)
            {
                this.AddOrUpdateGroup(contact as GroupModel, users);
            }
            else
            {
                this.AddUser(contact, updateContact);
            }
        }

        private void AddUser(UserModel contact, bool updateContact = false)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::AddUser", "start"));

            var existing = from u in this.dataContext.Table<UserModel>()
                           where u.Id == contact.Id
                           select u;

            if (existing.Count<UserModel>() == 0)
            {
                this.dataContext.Table<UserModel>().InsertOnSubmit(contact);
            }
            else if (updateContact)
            {
                UserModel existingUser = existing.First();
                if (string.Compare(existingUser.Name, contact.Name, StringComparison.OrdinalIgnoreCase) != 0 ||
                    string.Compare(existingUser.PhoneNumber, contact.PhoneNumber, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    existingUser.PhoneNumber = contact.PhoneNumber;
                    existingUser.Name = contact.Name;
                }

                if (IsPublicKeyUpdateNeeded(existingUser.PublicKey, contact.PublicKey))
                {
                    existingUser.PublicKey = contact.PublicKey;
                }
            }

            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::AddUser", "end"));
        }

        private static bool IsPublicKeyUpdateNeeded(byte[] localUserKey, byte[] userKeyOnServer)
        {
            if (localUserKey != userKeyOnServer)
            {
                if (localUserKey != null && userKeyOnServer != null)
                {
                    return !localUserKey.SequenceEqual(userKeyOnServer);
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        private bool ContainsContact(UserModel contact)
        {
            if (contact == null)
            {
                return true;
            }

            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::ContainsContact", "start"));

            var existing = from u in this.dataContext.Table<UserModel>()
                           where u.Id == contact.Id
                           select u;

            if (existing.Count<UserModel>() == 0)
            {
                return false;
            }

            UserModel user = existing.FirstOrDefault();

            if (user != null &&
                0 != StringComparer.OrdinalIgnoreCase.Compare(user.Name, contact.Name))
            {
                user.Name = contact.Name;
            }

            if (user != null &&
                !user.IsGroupOwner &&
                user.UserType != UserType.Group &&
                !user.PublicKey.SequenceEqual(contact.PublicKey))
            {
                user.PublicKey = contact.PublicKey;
            }

            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::ContainsContact", "end"));

            return true;
        }

        public void NewMessageCreated(MessageModel message)
        {
            this.NewMessageCreated(message, false);
        }

        private void NewMessageCreated(MessageModel encryptedMessage, bool isPush)
        {
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::NewMessageCreated", "start"));
            (Application.Current as App).PerfTrackerString += "\nInsert to DB:" + (Application.Current as App).PerfTrackerStopWatch.ElapsedMilliseconds.ToString();

            if (encryptedMessage == null)
            {
                Messenger.Default.Send<NewMessageEvent>(new NewMessageEvent() { Message = null, IsPush = isPush });
                return;
            }

            MessageModel message = null;

            try
            {
                message = encryptedMessage.DecryptMessage();
            }
            catch (Exception)
            {
                message = null;
            }

            if (message == null)
            {
                Messenger.Default.Send<NewMessageEvent>(new NewMessageEvent() { Message = null, IsPush = isPush });
                return;
            }

            message.LastReadTime = new DateTime(1970, 1, 1);
            encryptedMessage.CopyNonEncryptedProperties(message);

            if (message.IsPollResponseMessage)
            {
                this.SavePollResponse(message);
            }
            else if (message.IsTaskMessage.Value)
            {
                this.SaveSingleTaskMessage(message, new List<UserModel>());
            }
            else
            {
                this.SaveSingleMessage(message, new List<UserModel>());
            }

            // Set the last sync time on the server so the tile count is correct when the
            // server gets a new notification. This is computed based on the last sync time on the 
            // server.
            YapperServiceProxy.Instance.SetLastSyncDateTime(message.PostDateTime);

            (Application.Current as App).PerfTrackerString += "\nRefresh ui:" + (Application.Current as App).PerfTrackerStopWatch.ElapsedMilliseconds.ToString();

            // If message send was successful, the View should clear the text box
            // Otherwise, the textbox should not be cleared.
            Messenger.Default.Send<NewMessageEvent>(new NewMessageEvent() { Message = message, IsPush = isPush });

            if (isPush)
            {
                // Vibrate the phone
                Deployment.Current.Dispatcher.BeginInvoke(
                    () =>
                    {
                        this.ShowToast(message);
                        this.VibratePhone();
                    });
            }

            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::NewMessageCreated", "end"));
        }

        private void ExistingMessageDownloaded(MessageModel message)
        {
            if (message == null)
            {
                Messenger.Default.Send<NewMessageEvent>(new NewMessageEvent() { Message = null, IsPush = true });
                return;
            }

            this.NewMessageCreated(message, true); 

            // If message send was successful, the View should clear the text box
            // Otherwise, the textbox should not be cleared.
            Messenger.Default.Send<ExistingMessageEvent>(new ExistingMessageEvent() { Message = message, IsPush = false });
        }

        public void WriteImageToIsoStorage(Guid messageId, byte[] imageBlob)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (o, f) =>
                {
                    Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::WriteImageToIsoStorage", "start"));
                    if (imageBlob != null)
                    {
                        byte[] imagebytes = imageBlob;
                        using (var isoFile = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            using (var stream = isoFile.CreateFile(messageId.ToString()))
                            {
                                stream.Write(imagebytes, 0, imagebytes.Length);
                            }
                        }
                    }
                    Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::WriteImageToIsoStorage", "end"));
                };

            worker.RunWorkerAsync();
        }

        private void ShowToast(MessageModel message)
        {
            ToastPrompt toast = new ToastPrompt();
            toast.Title = message.Sender.Name;
            //toast.ImageSource = this.GetUser(message.Sender.Id).ContactPhoto;
            toast.HorizontalContentAlignment = HorizontalAlignment.Center;
            toast.Message = message.IsTaskMessage.Value ? string.Format(Strings.UpdatedTask, message.TaskName) : message.TextMessage;
            toast.FontSize = 25;
            toast.Height = 70;
            toast.Show();
            toast.VerticalAlignment = VerticalAlignment.Top;
        }

        /// <summary>
        /// This vibrates the phone. This is used when the app is in foreground and receives a push notification
        /// </summary>
        private void VibratePhone()
        {
            Microsoft.Devices.VibrateController.Default.Start(TimeSpan.FromMilliseconds(500));
            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            timer.Tick += (tsender, tevt) =>
            {
                var t = tsender as System.Windows.Threading.DispatcherTimer;
                t.Stop();
                Microsoft.Devices.VibrateController.Default.Stop();
            };

            timer.Start();
        }

        public void DeleteMessage(MessageModel m)
        {
            lock (this)
            {
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::DeleteMessage", "start"));
                this.dataContext.DeleteOnSubmit<MessageModel>(m);

                this.dataContext.SubmitChanges();
            }
        }

        public void DeleteAllMessagesFromPhone()
        {
            lock (this)
            {
                Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::DeleteAllMessagesFromPhone", "start"));
                var messages = from m in this.dataContext.Table<MessageModel>() where m.MessageType != 0 select m;
                this.dataContext.DeleteAllOnSubmit<MessageModel>(messages);

                this.dataContext.SubmitChanges();
            }

            Messenger.Default.Send<DeleteEvent>(new DeleteEvent() { DeleteState = DeleteState.Complete });
            Debug.WriteLine(string.Format("{0} {1} {2}", DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), "DataSync::DeleteAllMessagesFromPhone", "end"));
        }
    }
}