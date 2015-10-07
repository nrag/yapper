using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using YapperChat.EventMessages;
using YapperChat.Models;
using YapperChat.Resources;
using YapperChat.Sync;

namespace YapperChat.ViewModels
{
    public class NewTaskCountViewModel : INotifyPropertyChanged
    {
        private List<MessageModel> newMessages;

        private bool suspend;

        public NewTaskCountViewModel()
        {
            this.LoadNewOrModifiedTaskCount();
            Messenger.Default.Register<NewMessageEvent>(this, this.HandleNewMessageSavedEvent);
            Messenger.Default.Register<SyncEvent>(this, this.HandleSyncCompleteEvent);
            Messenger.Default.Register<SuspendTaskCountEvent>(this, this.HandleSuspendTaskCountEvent);
        }

        public string Count
        {
            get
            {
                if (this.newMessages.Count > 20)
                {
                    return Strings.TwentyPlus;
                }

                return this.newMessages.Count.ToString();
            }
        }

        public bool IsNewTaskAvailable
        {
            get
            {
                if (this.newMessages.Count == 0)
                {
                    return false;
                }

                return true;
            }
        }

        private void LoadNewOrModifiedTaskCount()
        {
            lock (this)
            {
                this.newMessages = DataSync.Instance.GetTaskCount(UserSettingsModel.Instance.LastTaskPageViewTime.ToUniversalTime().Ticks);
                DispatcherHelper.InvokeOnUiThread(() =>
                    {
                        this.NotifyPropertyChanged("Count");
                        this.NotifyPropertyChanged("IsNewTaskAvailable");
                    });
            }
        }

        private void HandleSyncCompleteEvent(SyncEvent obj)
        {
            this.LoadNewOrModifiedTaskCount();
        }

        private void HandleNewMessageSavedEvent(NewMessageEvent obj)
        {
            lock (this)
            {
                if (obj.Message != null &&
                    obj.Message.IsTaskMessage.Value &&
                    !this.suspend)
                {
                    if (!this.newMessages.Contains(obj.Message))
                    {
                        this.newMessages.Add(obj.Message);
                        DispatcherHelper.InvokeOnUiThread(() =>
                        {
                            this.NotifyPropertyChanged("Count");
                            this.NotifyPropertyChanged("IsNewTaskAvailable");
                        });
                    }
                }
            }
        }

        private void HandleSuspendTaskCountEvent(SuspendTaskCountEvent obj)
        {
            this.suspend = obj.Suspend;
            if (suspend)
            {
                this.newMessages.Clear();
            }
            else
            {
                this.LoadNewOrModifiedTaskCount();
            }
        }
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}
