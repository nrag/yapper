using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YapperChat.Common;
using YapperChat.Controls.Interactions;
using YapperChat.Database;
using YapperChat.Sync;

namespace YapperChat.Models
{
    class TaskMessageCreator<T> : IItemManager<T> where T : class, IItem
    {
        public bool HasChanged
        {
            get;
            set;
        }

        public bool IsDeleted
        {
            get;
            set;
        }

        public T CreateItem()
        {
            T task;
            if (typeof(T).Equals(typeof(MessageModel)))
            {
                MessageModel message = new MessageModel();
                message.MessageId = Guid.NewGuid();
                message.ClientMessageId = Guid.NewGuid();
                message.TaskName = null;
                message.Sender = UserSettingsModel.Instance.Me;
                message.SenderId = UserSettingsModel.Instance.Me.Id;
                message.TaskItemList = new ObservableSortedList<MessageModel>(4, new TaskListComparer<MessageModel>());
                message.MessageType = MessageType.Message;
                message.MessageFlags = MessageFlags.Task;
                message.PostDateTimeUtcTicks = DateTime.UtcNow.Ticks;
                message.LastReadTime = new DateTime(1970, 1, 1);
                message.LastUpdateTime = DateTime.Now;
                message.LastTaskUpdaterId = UserSettingsModel.Instance.Me.Id;

                DataSync.Instance.CreateTask(message);
                task = message as T;
            }
            else
            {
                task = Activator.CreateInstance<T>();
            }

            return task;
        }

        public void CompleteItem(T item)
        {
            item.IsCompleted = true;
            if (typeof(T).Equals(typeof(MessageModel)))
            {
                MessageModel message = item as MessageModel;
                if (message == null)
                {
                    return;
                }

                using (DataContextWrapper<YapperDataContext> context = new DataContextWrapper<YapperDataContext>())
                {
                    List<MessageModel> taskList = new List<MessageModel>(message.TaskItemList);
                    foreach (MessageModel taskItem in taskList)
                    {
                        context.Attach<MessageModel>(taskItem);
                        taskItem.IsCompleted = true;
                    }

                    context.Attach<MessageModel>(message);
                    message.IsCompleted = true;
                    context.SubmitChanges();

                    if (message.Recipient != null)
                    {
                        this.SendChanges(message);
                    }
                }
            }
        }

        public void DeleteItem(T item)
        {
            if (typeof(T).Equals(typeof(MessageModel)))
            {
                MessageModel message = item as MessageModel;
                if (message == null)
                {
                    return;
                }

                using (DataContextWrapper<YapperDataContext> context = new DataContextWrapper<YapperDataContext>())
                {
                    if (message.TaskItemList != null)
                    {
                        foreach (MessageModel taskItem in message.TaskItemList)
                        {
                            context.Attach<MessageModel>(taskItem);
                            context.DeleteOnSubmit<MessageModel>(taskItem);
                        }
                    }

                    var existingQuery = from m in context.Table<MessageModel>()
                                        where m.MessageId == message.MessageId
                                        select m;

                    MessageModel existing = existingQuery.FirstOrDefault();

                    if (existing != null)
                    {
                        context.DeleteOnSubmit<MessageModel>(existing);
                    }

                    context.SubmitChanges();
                    // send a message only if it is shared
                    if (message.Recipient != null)
                    {
                        this.SendChanges(message, true);
                    }
                }
            }
        }

        public void SetItemOrder(T itemToSet, T itemBefore, T itemAfter, bool save = false)
        {
            return;
        }

        public string GetItemOrder(T itemBefore, T itemAfter)
        {
            string beforeLabel = null;
            string afterLabel = null;

            if (itemBefore != null)
            {
                beforeLabel = itemBefore.ItemOrder;
            }

            if (itemAfter != null)
            {
                afterLabel = itemAfter.ItemOrder;
            }

            return TaskMessageCreator<T>.CalculateItemOrder(beforeLabel, afterLabel);
        }

        private void SendChanges(MessageModel m, bool isDeleted = false)
        {
            MessageModel clone = m.Clone() as MessageModel;
            if (isDeleted)
            {
                clone.IsTaskDeleted = true;
                clone.TaskItemList = null;
            }

            if (clone.IsGroup)
            {
                clone.Sender = UserSettingsModel.Instance.Me;
                clone.SenderId = clone.Sender.Id;
            }

            if (clone.RecipientId == UserSettingsModel.Instance.Me.Id)
            {
                clone.Recipient = m.Sender;
                clone.RecipientId = clone.Recipient.Id;
                clone.Sender = UserSettingsModel.Instance.Me;
                clone.SenderId = clone.Sender.Id;
            }

            if (clone.Recipient != null)
            {
                DataSync.Instance.SendMessage(clone);
            }
        }

        public static string CalculateItemOrder(string beforeLabel, string afterLabel)
        {
            if (string.IsNullOrEmpty(beforeLabel) && string.IsNullOrEmpty(afterLabel))
            {
                int label = (int)Math.Ceiling(((double)(Int32.MaxValue - 0)) / 2);

                return label.ToString("D10");
            }

            if (string.IsNullOrEmpty(beforeLabel))
            {
                string[] afterNumbers = afterLabel.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                int afterNumber = Int32.Parse(afterNumbers[0]);

                int label = (int)Math.Ceiling(((double)(afterNumber - 0)) / 2);
                return label.ToString("D10");
            }

            if (string.IsNullOrEmpty(afterLabel))
            {
                string[] beforeNumbers = beforeLabel.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                int beforeNumber = Int32.Parse(beforeNumbers[0]);

                int label = (int)Math.Ceiling(beforeNumber + ((double)(Int32.MaxValue - beforeNumber)) / 2);
                return label.ToString("D10");
            }

            string[] after= afterLabel.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            string[] before = beforeLabel.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            int i = 0;

            while (i < after.Count() &&
                   i < before.Count() &&
                   Int32.Parse(before[i]) == Int32.Parse(after[i]))
            {
                i++;
            }

            int newNumber = 0;
            int ithBefore = 0;
            int ithAfter = 0;
            if (i >= after.Count())
            {
                ithAfter = Int32.MaxValue;
                ithBefore = Int32.Parse(before[i]);
            }
            else if (i >= before.Count())
            {
                ithAfter = Int32.Parse(after[i]);
                ithBefore = 0;
            }
            else if (Int32.Parse(before[i]) + 1 == Int32.Parse(after[i]))
            {
                i++;
                ithBefore = (i == after.Count()) ? 0 : Int32.Parse(before[i]);
                ithAfter = (i == after.Count()) ? Int32.MaxValue : Int32.Parse(after[i]);
            }
            else
            {
                ithBefore = Int32.Parse(before[i]);
                ithAfter = Int32.Parse(after[i]);
            }

            newNumber = (int)Math.Ceiling(ithBefore + ((double)(ithAfter - ithBefore)) / 2);

            StringBuilder newLabel = new StringBuilder();

            for (int j = 0; j < i; j++)
            {
                newLabel.Append(before[j]);
                newLabel.Append(";");
            }

            newLabel.Append(newNumber.ToString("D10"));

            return newLabel.ToString();
        }
    }
}
