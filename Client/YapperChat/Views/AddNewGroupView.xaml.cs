using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using YapperChat.EventMessages;
using YapperChat.Models;
using YapperChat.ViewModels;

namespace YapperChat.Views
{
    public partial class AddNewGroupView : PhoneApplicationPage
    {
        private GroupModel createdGroup = null;

        public AddNewGroupView()
        {
            InitializeComponent();
            this.DataContext = new AddNewGroupViewModel();
            this.ContactsListSelector.ItemsSource = ((AddNewGroupViewModel)this.DataContext).RegisteredUsers;
            Messenger.Default.Register<NewGroupEvent>(this, this.GroupCreated);
            Messenger.Default.Register<SyncEvent>(this, this.GroupSynced);
            this.ApplicationBar = (ApplicationBar)this.Resources["MainpageApplicationBar"];
            this.ApplicationBar.IsVisible = true;
            foreach (var b in this.ApplicationBar.Buttons)
            {
                ((ApplicationBarIconButton)b).IsEnabled = false;
            }

            BuildLocalizedApplicationBar();
        }

        private void AddMemberButton_Click(object sender, RoutedEventArgs e)
        {
            this.SelectUserPopup.Visibility = Visibility.Visible;
            this.SelectUserPopup.IsOpen = true;
            this.ApplicationBar = (ApplicationBar)this.Resources["AddMemberPageApplicationBar"];
            this.ApplicationBar.IsVisible = true;
        }

        private void GroupNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.GroupNameTextBox.Text))
            {
                foreach (var b in this.ApplicationBar.Buttons)
                {
                    ((ApplicationBarIconButton)b).IsEnabled = false;
                }
            }
            else
            {
                foreach (var b in this.ApplicationBar.Buttons)
                {
                    ((ApplicationBarIconButton)b).IsEnabled = true;
                }
            }
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            this.ApplicationBar.IsVisible = false;
            foreach (var b in this.ApplicationBar.Buttons)
            {
                ((ApplicationBarIconButton)b).IsEnabled = false;
            }

            AddNewGroupViewModel vm = (AddNewGroupViewModel)this.DataContext;
            vm.CreateNewGroup(this.GroupNameTextBox.Text, vm.SelectedMembers.ToList());
        }

        private void AddMemberApplicationBarDone_Click(object sender, EventArgs e)
        {
            this.ApplicationBar = (ApplicationBar)this.Resources["MainpageApplicationBar"];
            this.ApplicationBar.IsVisible = true;
            
            AddNewGroupViewModel vm = (AddNewGroupViewModel)this.DataContext;
            foreach (object user in this.ContactsListSelector.SelectedItems)
            {
                vm.SelectedMembers.Add((UserModel)user);
            }

            this.SelectUserPopup.Visibility = System.Windows.Visibility.Collapsed;
            this.SelectUserPopup.IsOpen = false;
        }

        private void GroupCreated(NewGroupEvent groupEvent)
        {
            if (groupEvent.GroupCreated != null)
            {
                this.createdGroup = groupEvent.GroupCreated;
            }
            else
            {
                foreach (var b in this.ApplicationBar.Buttons)
                {
                    ((ApplicationBarIconButton)b).IsEnabled = true;
                }

                this.ApplicationBar.IsVisible = true;
            }
        }

        private void GroupSynced(SyncEvent syncEvent)
        {
            if (syncEvent.SyncState == SyncState.Complete)
            {
                AllConversationsViewModel allConversations = ViewModelLocator.Instance.CreateOrGetViewModel<AllConversationsViewModel>(null);
                DispatcherHelper.InvokeOnUiThread(() =>
                    {
                        NavigationService.Navigate(new Uri(string.Format("/Views/ConversationMessagesView.xaml?conversationId={0}&recipientId={1}&recipientName={2}&isGroup={3}&pivot=1", allConversations.GetConversation(this.createdGroup.Id), this.createdGroup.Id, this.createdGroup.Name, true), UriKind.Relative));
                    });
            }
            else
            {
                DispatcherHelper.InvokeOnUiThread(() =>
                    {
                        NavigationService.Navigate(new Uri("/Views/YapperChatMessagesPivot.xaml", UriKind.Relative));
                    });
            }
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.SelectUserPopup.Visibility == System.Windows.Visibility.Visible)
            {
                this.SelectUserPopup.Visibility = System.Windows.Visibility.Collapsed;
                this.SelectUserPopup.IsOpen = false;
                foreach (var item in this.ContactsListSelector.ItemsSource)
                {
                    var container = this.ContactsListSelector.ContainerFromItem(item) as LongListMultiSelectorItem;
                    if (container != null)
                    {
                        container.IsSelected = false;
                    }
                }
                
                e.Cancel = true;
            }
            else
            {
                NavigationService.Navigate(new Uri("/Views/YapperChatMessagesPivot.xaml", UriKind.Relative));
            }
        }

        private void BuildLocalizedApplicationBar()
        {
            ApplicationBar appbar = (ApplicationBar)this.Resources["MainpageApplicationBar"];
            ((ApplicationBarIconButton)appbar.Buttons[0]).Text = YapperChat.Resources.Strings.AddGroupText;

            appbar = (ApplicationBar)this.Resources["AddMemberPageApplicationBar"];
            ((ApplicationBarIconButton)appbar.Buttons[0]).Text = YapperChat.Resources.Strings.AddMemberText;
        }
    }
}