using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using GalaSoft.MvvmLight.Messaging;
using YapperChat.Common;
using YapperChat.EventMessages;
using YapperChat.Models;
using YapperChat.ServiceProxy;
using YapperChat.Sync;

namespace YapperChat.ViewModels
{
    public class AddNewGroupViewModel : INotifyPropertyChanged
    {
        private RegisteredUsersViewModel registeredUsers;

        private ObservableCollection<UserModel> selectedMembers = new ObservableCollection<UserModel>();

        private bool _isAdding = false;

        public AddNewGroupViewModel()
        {
            this.registeredUsers = ViewModelLocator.Instance.CreateOrGetViewModel<RegisteredUsersViewModel>();
            Messenger.Default.Register<NewGroupEvent>(this, this.GroupCreated);
        }

        public bool IsAdding
        {
            get
            {
                return _isAdding;
            }

            set
            {
                this._isAdding = value;
                this.NotifyPropertyChanged("IsAdding");
            }
        }

        public ObservableSortedList<ContactGroup<UserModel>> RegisteredUsers
        {
            get
            {
                return this.registeredUsers.RegisteredUsers;
            }
        }

        public ObservableCollection<UserModel> SelectedMembers
        {
            get
            {
                return this.selectedMembers;
            }
        }

        internal void CreateNewGroup(string name, List<UserModel> members)
        {
            this.IsAdding = true;
            DataSync.Instance.CreateGroup(name, members);
        }

        internal void GroupCreated(NewGroupEvent group)
        {
            DataSync.Instance.SyncMessages();
            Messenger.Default.Register<SyncEvent>(this, this.GroupSynced);
        }

        private void GroupSynced(SyncEvent sync)
        {
            DispatcherHelper.InvokeOnUiThread(() =>
                {
                    this.IsAdding = false;
                });

            Messenger.Default.Unregister<SyncEvent>(this);
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
    }
}
