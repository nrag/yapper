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
    class TaskListMessageCreator<T> : IItemManager<T> where T : class, IItem
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

        public List<MessageModel> DeletedItems
        {
            get;
            set;
        }

        public T CreateItem()
        {
            T task;
            if (typeof(T).Equals(typeof(MessageModel)))
            {
                MessageModel message = this.CreateTaskMessage();
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
            this.HasChanged = true;
            item.IsCompleted = true;
            if (typeof(T).Equals(typeof(MessageModel)))
            {
                DataSync.Instance.SetTaskCompleted((item as MessageModel).ClientMessageId);
            }
        }

        public void DeleteItem(T item)
        {
            this.IsDeleted = true;
            if (typeof(T).Equals(typeof(MessageModel)))
            {
                MessageModel message = item as MessageModel;

                if (this.DeletedItems == null)
                {
                    this.DeletedItems = new List<MessageModel>();
                }

                if (message == null)
                {
                    return;
                }

                using (DataContextWrapper<YapperDataContext> context = new DataContextWrapper<YapperDataContext>())
                {
                    context.Attach<MessageModel>(message);
                    context.DeleteOnSubmit<MessageModel>(message);
                    context.SubmitChanges();
                }

                this.DeletedItems.Add(message);
            }
        }

        public MessageModel CreateTaskMessage()
        {
            MessageModel message = new MessageModel();

            message.MessageId = Guid.NewGuid();
            message.ClientMessageId = Guid.NewGuid();
            message.TaskName = null;
            message.Sender = UserSettingsModel.Instance.Me;
            message.SenderId = message.Sender.Id;
            message.TaskItemList = new ObservableSortedList<MessageModel>();
            message.MessageType = MessageType.Message;
            message.MessageFlags = MessageFlags.TaskItem;
            message.LastReadTime = new DateTime(1970, 1, 1);

            return message;
        }

        public void SetItemOrder(T itemToSet, T itemBefore, T itemAfter, bool save = false)
        {
            this.HasChanged = true;
            if (!save)
            {
                itemToSet.ItemOrder = this.GetItemOrder(itemBefore, itemAfter);
                return;
            }

            if (typeof(T).Equals(typeof(MessageModel)))
            {
                MessageModel message = itemToSet as MessageModel;
                if (message == null)
                {
                    return;
                }

                using (DataContextWrapper<YapperDataContext> context = new DataContextWrapper<YapperDataContext>())
                {
                    context.Attach<MessageModel>(message);
                    message.ItemOrder = this.GetItemOrder(itemBefore, itemAfter);
                    context.SubmitChanges();
                }
            }
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
    }
}
