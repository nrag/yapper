using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using GalaSoft.MvvmLight.Messaging;
using YapperChat.EventMessages;
using YapperChat.Models;
using YapperChat.ServiceProxy;
using YapperChat.Sync;

namespace YapperChat.ViewModels
{
    public class AllGroupsViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ContactGroup<GroupModel>> groups = new ObservableCollection<ContactGroup<GroupModel>>();

        private bool _isLoading = true;

        public AllGroupsViewModel()
        {
            Messenger.Default.Register<RefreshGroupsEvent>(this, this.ReadGroups);
            Messenger.Default.Register<NewGroupEvent>(this, this.NewGroupCreated);
            Messenger.Default.Register<SyncEvent>(this, this.HandleSyncCompleteEvent);
            this.LoadGroups();
        }

        private void HandleSyncCompleteEvent(SyncEvent obj)
        {
            this.IsLoading = false;
        }

        public ObservableCollection<ContactGroup<GroupModel>> Groups
        {
            get
            {
                return this.groups;
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

        private void ReadGroups(RefreshGroupsEvent refresh)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (o, f) =>
                {
                    this.ReadGroups();
                };
            worker.RunWorkerAsync();
        }

        private void ReadGroups()
        {
            IEnumerable<UserModel> readGroups = DataSync.Instance.GetGroups();

            lock (this)
            {
                foreach (UserModel group in readGroups)
                {
                    if (group is GroupModel)
                    {
                        this.AddGroup((GroupModel)group);
                    }
                }
            }
        }

        private void NewGroupCreated(NewGroupEvent newGroup)
        {
            lock (this)
            {
                if (newGroup != null)
                {
                    if (!this.Contains(newGroup.GroupCreated))
                    {
                        this.AddGroup(newGroup.GroupCreated);
                    }
                }
            }
        }

        private void LoadGroups()
        {
            this.ReadGroups();
            this.IsLoading = true;
            DataSync.Instance.DownloadGroups();
        }

        private void AddGroup(GroupModel group)
        {
            if (this.Contains(group))
            {
                return;
            }

            char firstLetter = char.ToLower(group.Name[0]);

            // show # for numbers
            if (firstLetter >= '0' && firstLetter <= '9')
            {
                firstLetter = '#';
            }

            bool found = false;
            foreach (ContactGroup<GroupModel> alphaGroup in this.groups)
            {
                // create group for letter if it doesn't exist
                if (alphaGroup.FirstLetter == firstLetter)
                {
                    found = true;
                    alphaGroup.Add(group);
                }
            }

            if (!found)
            {
                var alphaGroup = new ContactGroup<GroupModel>(firstLetter);
                this.groups.Add(alphaGroup);

                // create a contact for item and add it to the relevant 
                alphaGroup.Add(group);
            }
        }

        private bool Contains(GroupModel group)
        {
            char firstLetter = char.ToLower(group.Name[0]);
            foreach (ContactGroup<GroupModel> alphaGroup in this.groups)
            {
                if (alphaGroup.FirstLetter == firstLetter)
                {
                    foreach (UserModel user in alphaGroup)
                    {
                        if (user.Id == group.Id)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
