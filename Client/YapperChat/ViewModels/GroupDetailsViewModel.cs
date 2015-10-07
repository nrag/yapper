using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight.Messaging;
using YapperChat.Common;
using YapperChat.EventMessages;
using YapperChat.Models;
using YapperChat.Sync;

namespace YapperChat.ViewModels
{
    public class GroupDetailsViewModel : INotifyPropertyChanged
    {
        private static BitmapImage removeImage;

        private ObservableSortedList<ContactGroup<UserModel>> members = new ObservableSortedList<ContactGroup<UserModel>>();

        private ObservableCollection<UserModel> nonGroupedMembers = new ObservableCollection<UserModel>();

        private GroupModel group;

        private RegisteredUsersViewModel registeredUsersViewModel;

        private string name;

        private bool isUpdating = false;

        private string owner;

        public GroupDetailsViewModel()
        {
        }

        public void SetGroup(GroupModel group)
        {
            if (group != null)
            {
                this.group = group;
                this.Name = group.Name;
                this.Owner = group.Owner.Name;
                this.registeredUsersViewModel = ViewModelLocator.Instance.CreateOrGetViewModel<RegisteredUsersViewModel>();
                Messenger.Default.Register<GroupMemberAddedEvent>(this, this.MemberAdded);
                Messenger.Default.Register<GroupMemberRemovedEvent>(this, this.MemberRemoved);
                this.group.Members.Sort((x, y) => { return String.Compare(x.Name, y.Name); });
                for (int i = 0; i < group.Members.Count; i++)
                {
                    this.AddGroupMember(group.Members[i]);
                    this.nonGroupedMembers.Add(group.Members[i]);
                }

                this.NotifyPropertyChanged("RegisteredUsers");
            }
        }

        public ObservableSortedList<ContactGroup<UserModel>> Members
        {
            get
            {
                return this.members;
            }
        }

        public List<UserModel> NonGroupedMembers
        {
            get
            {
                return this.group.Members;
            }
        }

        public string Name
        {
            get
            {
                return this.name;
            }

            private set
            {
                this.name = value;
                this.NotifyPropertyChanged("Name");
            }
        }

        public string Owner
        {
            get
            {
                return this.owner;
            }

            set
            {
                this.owner = value;
                this.NotifyPropertyChanged("Owner");
            }
        }

        public bool IsUpdating
        {
            get
            {
                return this.isUpdating;
            }

            set
            {
                this.isUpdating = value;
                this.NotifyPropertyChanged("IsUpdating");
            }
        }

        public BitmapImage RemoveImage
        {
            get
            {
                if (GroupDetailsViewModel.removeImage == null)
                {
                    GroupDetailsViewModel.removeImage = new BitmapImage(new Uri("/Images/appbar.cancel.rest.png", UriKind.RelativeOrAbsolute));
                }

                return GroupDetailsViewModel.removeImage;
            }
        }

        public ObservableSortedList<ContactGroup<UserModel>> RegisteredUsers
        {
            get
            {
                return this.registeredUsersViewModel.RegisteredUsers;
            }
        }

        public void AddMember(UserModel user)
        {
            this.IsUpdating = true;
            DataSync.Instance.AddGroupMember(group, user);
        }

        public void RemoveMember(UserModel user)
        {
            this.IsUpdating = true;
            DataSync.Instance.RemoveGroupMember(group, user);
        }

        private void MemberAdded(GroupMemberAddedEvent memberAdded)
        {
            if (memberAdded.Success)
            {
                this.AddGroupMember(memberAdded.User);
            }

            this.IsUpdating = false;
        }

        private void MemberRemoved(GroupMemberRemovedEvent memberRemoved)
        {
            if (memberRemoved.Success)
            {
                this.RemoveGroupMember(memberRemoved.User);
            }

            this.IsUpdating = false;
        }

        private void AddGroupMember(UserModel groupMember)
        {
            if (this.Contains(groupMember))
            {
                return;
            }

            this.nonGroupedMembers.Add(groupMember);

            char firstLetter = char.ToLower(groupMember.Name[0]);

            // show # for numbers
            if (firstLetter >= '0' && firstLetter <= '9')
            {
                firstLetter = '#';
            }

            bool found = false;
            foreach (ContactGroup<UserModel> memberGroup in this.members)
            {
                // create group for letter if it doesn't exist
                if (memberGroup.FirstLetter == firstLetter)
                {
                    found = true;
                    memberGroup.Add(groupMember);
                }
            }

            if (!found)
            {
                var memberGroup = new ContactGroup<UserModel>(firstLetter);
                memberGroup.Add(groupMember);

                this.members.Add(memberGroup);
            }
        }

        private void RemoveGroupMember(UserModel groupMember)
        {
            if (!this.Contains(groupMember))
            {
                return;
            }

            this.nonGroupedMembers.Remove(groupMember);

            char firstLetter = char.ToLower(groupMember.Name[0]);

            // show # for numbers
            if (firstLetter >= '0' && firstLetter <= '9')
            {
                firstLetter = '#';
            }

            foreach (ContactGroup<UserModel> memberGroup in this.members)
            {
                // create group for letter if it doesn't exist
                if (memberGroup.FirstLetter == firstLetter)
                {
                    memberGroup.Remove(groupMember);
                    if (memberGroup.Count == 0)
                    {
                        this.members.Remove(memberGroup);
                    }

                    break;
                }
            }
        }

        public bool Contains(UserModel groupMember)
        {
            char firstLetter = char.ToLower(groupMember.Name[0]);
            foreach (ContactGroup<UserModel> alphaGroup in this.members)
            {
                if (alphaGroup.FirstLetter == firstLetter)
                {
                    foreach (UserModel user in alphaGroup)
                    {
                        if (user.Id == groupMember.Id)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
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
