using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YapperChat.EventMessages;
using YapperChat.Models;
using YapperChat.Sync;
using YapperChat.Common;

namespace YapperChat.ViewModels
{
    public class AllTasksViewModel : INotifyPropertyChanged
    {
        private ObservableSortedList<MessageModel> tasks = new ObservableSortedList<MessageModel>(
            4, 
            Comparer<MessageModel>.Create( AllTasksViewModel.CompareTasks));

        private bool _isLoading = true;

        public AllTasksViewModel()
        {
            this.LoadTasks();
            Messenger.Default.Register<NewMessageEvent>(this, this.HandleNewMessageSavedEvent);
            Messenger.Default.Register<RefreshTaskMessage>(this, this.HandleTaskRefresh);
            Messenger.Default.Register<SyncEvent>(this, this.HandleSyncCompleteEvent);
        }

        private void HandleSyncCompleteEvent(SyncEvent obj)
        {
            lock (this)
            {
                this.LoadTasks();
                DispatcherHelper.InvokeOnUiThread(() =>
                    {

                        for (int i = 0; i < tasks.Count; i++)
                        {
                            this.AddTask(tasks[i]);
                        }
                    });
            }

            this.IsLoading = false;
        }

        private void HandleTaskRefresh(RefreshTaskMessage obj)
        {
            lock (this)
            {
                DispatcherHelper.InvokeOnUiThread(() =>
                {
                    MessageModel m = DataSync.Instance.GetMessageFromClientId(obj.TaskId);
                    if (m != null)
                    {
                        this.AddTask(m);
                    }

                });
            }

            this.IsLoading = false;
        }


        private void HandleNewMessageSavedEvent(NewMessageEvent obj)
        {
            lock (this)
            {
                MessageModel task = obj.Message as MessageModel;
                if (task != null)
                {
                    DispatcherHelper.InvokeOnUiThread(() =>
                        {
                            MessageModel m = DataSync.Instance.GetMessageFromClientId(task.ClientMessageId);
                            if (m != null)
                            {
                                m.LoadTaskList();
                                this.AddTask(m);
                            }
                        });
                }
            }
        }

        public ObservableSortedList<MessageModel> Tasks
        {
            get
            {
                return this.tasks;
            }

            set
            {
                this.tasks = value;
                this.NotifyPropertyChanged("Tasks");
            }
        }

        public bool IsLoading
        {
            get
            {
                return this._isLoading;
            }

            private set
            {
                this._isLoading = value;
                this.NotifyPropertyChanged("IsLoading");
            }
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

        private void LoadTasks()
        {
            List<MessageModel> allTasksFromDB = DataSync.Instance.GetAllTasks();
            for (int i = 0; i < allTasksFromDB.Count; i++)
            {
                this.AddTask(allTasksFromDB[i]);
            }

            this.NotifyPropertyChanged("Tasks");
        }

        private void AddTask(MessageModel task)
        {
            if (task == null)
            {
                return;
            }

            DispatcherHelper.InvokeOnUiThread(() =>
            {
                if (task.IsTaskMessage.Value)
                {
                    for (int i = 0; i < this.tasks.Count; i++)
                    {
                        if (this.tasks[i].ClientMessageId == task.ClientMessageId)
                        {
                            this.tasks.RemoveAt(i);
                            if (!task.IsTaskDeleted.HasValue || !task.IsTaskDeleted.Value)
                            {
                                this.tasks.Add(task);
                            }
                            
                            return;
                        }
                    }

                    if (!task.IsTaskDeleted.HasValue || !task.IsTaskDeleted.Value)
                    {
                        this.tasks.Add(task);
                    }
                }
            });
        }

        private static int CompareTasks(MessageModel x, MessageModel y)
        {
            if (x.ClientMessageId == y.ClientMessageId)
            {
                return 0;
            }

            if (x.IsPullDown) 
            {
                return -1;
            }

            if (y.IsPullDown)
            {
                return 1;
            }

            return x.PostDateTimeUtcTicks > y.PostDateTimeUtcTicks ? -1 : 1;
        }
    }
}