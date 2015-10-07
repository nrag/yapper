using GalaSoft.MvvmLight.Messaging;
using System;
using System.ComponentModel;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using YapperChat.EventMessages;

namespace YapperChat.ViewModels
{
    public class YapperChatViewModel
    {
        private AllConversationsViewModel conversationsViewModel;

        private RegisteredUsersViewModel registeredUsersViewModel;

        private AllGroupsViewModel groupsViewModel;

        private AllTasksViewModel tasksViewModel;

        private NewTaskCountViewModel newTaskCountViewModel;

        private NewMessageCountViewModel newMessageCountViewModel;

        public YapperChatViewModel(bool startup)
        {
            Messenger.Default.Register<RefreshGroupsEvent>(this, this.ReadGroupsRefreshContactsPage);

            this.conversationsViewModel = ViewModelLocator.Instance.CreateOrGetViewModel<AllConversationsViewModel>();
            this.newTaskCountViewModel = ViewModelLocator.Instance.CreateOrGetViewModel<NewTaskCountViewModel>();
            this.newMessageCountViewModel = ViewModelLocator.Instance.CreateOrGetViewModel<NewMessageCountViewModel>();

            if (!startup)
            {
                this.registeredUsersViewModel = ViewModelLocator.Instance.CreateOrGetViewModel<RegisteredUsersViewModel>();
                this.groupsViewModel = ViewModelLocator.Instance.CreateOrGetViewModel<AllGroupsViewModel>();
                this.tasksViewModel = ViewModelLocator.Instance.CreateOrGetViewModel<AllTasksViewModel>();
            }
        }

        private void ReadGroupsRefreshContactsPage(RefreshGroupsEvent groupEvent)
        {
            if (this.registeredUsersViewModel != null)
            {
                DispatcherHelper.InvokeOnUiThread(() =>
                {
                    this.registeredUsersViewModel.ReadRegisteredUsersFromDB();
                    NotifyPropertyChanged("RegisteredUsers");
                });
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
        public AllConversationsViewModel AllConversations
        {
            get
            {
                return this.conversationsViewModel;
            }
        }

        public RegisteredUsersViewModel RegisteredUsers
        {
            get
            {
                return this.registeredUsersViewModel;
            }
        }

        public AllGroupsViewModel Groups
        {
            get
            {
                return this.groupsViewModel;
            }
        }

        public NewTaskCountViewModel NewTaskCount
        {
            get
            {
                return this.newTaskCountViewModel;
            }
        }

        public NewMessageCountViewModel NewMessageCount
        {
            get
            {
                return this.newMessageCountViewModel;
            }
        }

        public bool IsContactsEmpty
        {
            get
            {
                int count = this.RegisteredUsers == null ? 0 : this.RegisteredUsers.RegisteredUsers.Count;
                bool isLoading = this.RegisteredUsers == null ? false : this.RegisteredUsers.IsLoading;
                bool conversationsLoaded = this.AllConversations == null ? true : AllConversations.IsLoaded;

                return (!isLoading && conversationsLoaded && count == 0);
            }
        }

        public bool IsContactsNotEmpty
        {
            get
            {
                return !IsContactsEmpty;
            }
        }

        public AllTasksViewModel Tasks
        {
            get
            {
                return this.tasksViewModel;
            }
        }
    }
}
