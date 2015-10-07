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
    public class NewMessageCountViewModel : INotifyPropertyChanged
    {
        private List<MessageModel> newMessages = new List<MessageModel>();

        private bool suspend;

        public NewMessageCountViewModel()
        {
            // Don't load the messages initially. Because message page is the default landing page
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

        public bool IsNewMessageAvailable
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

        private void LoadNewMessageCount()
        {
            lock (this)
            {
                this.newMessages = DataSync.Instance.GetMessagesNewerThan(UserSettingsModel.Instance.LastMessagePageViewTime.ToUniversalTime().Ticks);
                DispatcherHelper.InvokeOnUiThread(() =>
                {
                    this.NotifyPropertyChanged("Count");
                    this.NotifyPropertyChanged("IsNewMessageAvailable");
                });
            }
        }

        private void HandleSyncCompleteEvent(SyncEvent obj)
        {
            this.LoadNewMessageCount();
        }

        private void HandleNewMessageSavedEvent(NewMessageEvent obj)
        {
            lock (this)
            {
                if (obj.Message != null &&
                    !obj.Message.IsTaskMessage.Value &&
                    !obj.Message.IsTaskInfoMessage &&
                    !obj.Message.IsTaskItemMessage &&
                    !this.suspend)
                {
                    if (!this.newMessages.Contains(obj.Message))
                    {
                        this.newMessages.Add(obj.Message);
                        DispatcherHelper.InvokeOnUiThread(() =>
                        {
                            this.NotifyPropertyChanged("Count");
                            this.NotifyPropertyChanged("IsNewMessageAvailable");
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
                this.LoadNewMessageCount();
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
