using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using Microsoft.Phone.Data.Linq;
using YapperChat.Models;

namespace YapperChat.Database
{
    public class YapperDataContext : DataContext
    {
        public static int DbVersion = 6;

        // Specify the connection string as a static
        public static string DBConnectionString = "Data Source=isostore:/Conversations.sdf";

        // Pass the connection string to the base class.
        public YapperDataContext() : base(DBConnectionString) { }

        // Table to store the user model for the conversation
        public Table<UserModel> UserModelItems;

        public Table<MessageModel> MessageModel;

        public Table<GroupMemberModel> GroupMemberTable;

        public static readonly Dictionary<int, Action<DatabaseSchemaUpdater>> NewColumnsInVersion = new Dictionary<int, Action<DatabaseSchemaUpdater>>()
        {
            {2, (u) => {
                            u.AddColumn<MessageModel>("ClientVisibleTimeTicks");
                       }
            },
            {3, (u) => {
                            u.AddColumn<MessageModel>("IsTaskMessage");
                            u.AddColumn<MessageModel>("LastTaskUpdaterId");
                            u.AddIndex<MessageModel>("task_Message");
                       }
            },
            {4, (u) => {
                            u.AddColumn<MessageModel>("LastTaskUpdaterId");
                       }
            },
            {5, (u) => {
                            u.AddColumn<MessageModel>("UnreadCount");
                       }
            },
            {6, (u) => {
                            u.AddColumn<MessageModel>("PollClientMessageId");
                       }
            }
        };

        public static readonly Dictionary<int, Action> PostDatabaseUpgradeOperation = new Dictionary<int, Action>()
        {
            {2, YapperDataContext.AddClientVisibleTime},
            {3, YapperDataContext.AddIsTaskMessage},
            {5, YapperDataContext.AddUnreadCount}
        };

        public static void AddClientVisibleTime()
        {
            using (DataContextWrapper<YapperDataContext> context = new DataContextWrapper<YapperDataContext>())
            {
                var messages = (from message in context.Table<MessageModel>()
                                where (message.MessageType != (long)MessageType.Conversation)
                                orderby message.PostDateTimeUtcTicks descending
                                select message);

                foreach (MessageModel message in messages)
                {
                    message.ClientVisibleTimeTicks = message.PostDateTimeUtcTicks;
                }

                context.SubmitChanges();
            }
        }

        public static void AddIsTaskMessage()
        {
            using (DataContextWrapper<YapperDataContext> context = new DataContextWrapper<YapperDataContext>())
            {
                var messages = (from message in context.Table<MessageModel>()
                                where (message.MessageType != (long)MessageType.Conversation)
                                orderby message.PostDateTimeUtcTicks descending
                                select message);

                foreach (MessageModel message in messages)
                {
                    message.IsTaskMessage = (message.MessageFlags & MessageFlags.Task) == MessageFlags.Task;
                }

                context.SubmitChanges();
            }
        }

        public static void AddUnreadCount()
        {
            using (DataContextWrapper<YapperDataContext> context = new DataContextWrapper<YapperDataContext>())
            {
                var messages = (from message in context.Table<MessageModel>()
                                where (message.MessageType == (long)MessageType.Conversation)
                                orderby message.PostDateTimeUtcTicks descending
                                select message);

                foreach (MessageModel message in messages)
                {
                    message.UnreadCount = 0;
                }

                context.SubmitChanges();
            }
        }
    }
}