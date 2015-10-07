using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

using YapperChat.Models;
using YapperChat.PushNotification;
using YapperChat.ViewModels;
using Microsoft.Phone.Shell;
using System.Windows.Data;
using System.Windows.Threading;
using System.Collections;
using GalaSoft.MvvmLight.Messaging;
using YapperChat.EventMessages;
using System.IO;
using System.Xml;
using Microsoft.Phone.Tasks;
using YapperChat.Sync;
using YapperChat.Database;

namespace YapperChat.Views
{
    public partial class YapperChatContactsPivot : PhoneApplicationPage
    {
        private CollectionViewSource contactsCollection;
        private CollectionViewSource groupsCollection;

        public YapperChatContactsPivot()
        {
            InitializeComponent();

            YapperChatViewModel viewModel = new YapperChatViewModel(false);
            this.DataContext = viewModel;

            this.contactsCollection = new System.Windows.Data.CollectionViewSource();
            this.contactsCollection.Source = viewModel.RegisteredUsers.RegisteredUsers;
            System.ComponentModel.SortDescription contactsSort = new System.ComponentModel.SortDescription("FirstLetter", System.ComponentModel.ListSortDirection.Ascending);
            this.contactsCollection.SortDescriptions.Add(contactsSort);

            this.ContactsListSelector.ItemsSource = viewModel.RegisteredUsers.RegisteredUsers;

            this.groupsCollection = new System.Windows.Data.CollectionViewSource();
            this.groupsCollection.Source = viewModel.Groups.Groups;
            System.ComponentModel.SortDescription groupsSort = new System.ComponentModel.SortDescription("Name", System.ComponentModel.ListSortDirection.Ascending);
            this.contactsCollection.SortDescriptions.Add(groupsSort);

            BuildLocalizedApplicationBar();
        }

        private void TaskPivotView_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/Views/YapperChatTaskPivot.xaml"), UriKind.Relative));
        }

        private void AddGroup_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/Views/AddNewGroupView.xaml"), UriKind.Relative));
        }

        /// <summary>
        /// Handles the Application bar settings selection. The settings page displays
        /// the current user settings for the Yapper application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ApplicationBarSettings_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/UserSettingsView.xaml", UriKind.Relative));
        }

        /// <summary>
        /// Handles the Application bar settings selection. The settings page displays
        /// the current user settings for the Yapper application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>ApplicationBarSignout_Click
        private void ApplicationBarTutorial_Click(object sender, EventArgs e)
        {
            // Go to the new user's registration page.
            NavigationService.Navigate(new Uri("/Views/TutorialPageWelcome.xaml?page=contacts", UriKind.Relative));
        }

        /// <summary>
        /// Handles the Application bar settings selection. The settings page displays
        /// the current user settings for the Yapper application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>ApplicationBarSignout_Click
        private void ApplicationBarSignout_Click(object sender, EventArgs e)
        {
            UserSettingsModel.Instance.Clear();

            // Clear the backstack because back should not go back to the page that 
            // requires signing in
            while (NavigationService.BackStack.Any())
            {
                NavigationService.RemoveBackEntry();
            }

            // Go to the new user's registration page.
            NavigationService.Navigate(new Uri("/Views/NewUserRegistrationView.xaml", UriKind.Relative));
        }

        private void ContactsPivotView_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/Views/YapperChatContactsPivot.xaml"), UriKind.Relative));
        }

        private void CalendarPivotView_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/Views/YapperChatCalendarPivot.xaml"), UriKind.Relative));
        }

        private void MessagePivotView_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/Views/YapperChatMessagesPivot.xaml"), UriKind.Relative));
        }

        private void Contact_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1)
            {
                return;
            }

            UserModel selectedUser = (UserModel)((LongListSelector)sender).SelectedItem;

            // If selected user does not exist, do not try to navigate
            if (selectedUser != null)
            {
                Uri navigationUri = this.GetConversationMessagesViewUri(selectedUser);
                NavigationService.Navigate(navigationUri);
            }
        }

        private string GetConversationMessagesViewUri(ConversationModel selectedItemData)
        {
            YapperChatViewModel viewModel = (YapperChatViewModel)this.DataContext;
            viewModel.AllConversations.SelectedConversation = selectedItemData;

            UserModel recipient = null;
            foreach (UserModel user in selectedItemData.ConversationParticipants)
            {
                if (!user.Equals(UserSettingsModel.Instance.Me))
                {
                    recipient = user;
                }
            }

            string uriString =
                string.Format(
                    "/Views/ConversationMessagesView.xaml?conversationId={0}&recipientName={1}&recipientId={2}&isGroup={3}",
                    selectedItemData.ConversationId, recipient.Name, recipient.Id, selectedItemData.IsGroupConversation);
            return uriString;
        }

        private Uri GetConversationMessagesViewUri(UserModel selectedUser)
        {
            AllConversationsViewModel existingConversations = ViewModelLocator.Instance.CreateOrGetViewModel<AllConversationsViewModel>();
            ConversationModel existingConversation = existingConversations.GetConversation(selectedUser.Id);

            Guid conversationId;
            if (existingConversation != null)
            {
                conversationId = existingConversation.ConversationId;
            }
            else
            {
                conversationId = Guid.Empty;
            }

            return new Uri(string.Format("/Views/ConversationMessagesView.xaml?conversationId={0}&recipientId={1}&recipientName={2}&isGroup={3}&pivot=1", conversationId, selectedUser.Id, selectedUser.Name, selectedUser.UserType == UserType.Group), UriKind.Relative);
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            while (NavigationService.CanGoBack)
            {
                NavigationService.RemoveBackEntry();
            }
        }

        private void InviteFriendsAppBarButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/Views/InviteFriends.xaml"), UriKind.Relative));
        }

        private void InviteFriendsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/Views/InviteFriends.xaml"), UriKind.Relative));
        }

        private void BuildLocalizedApplicationBar()
        {
            ApplicationBar appbar = (ApplicationBar)this.Resources["ChatContactsApplicationBar"];
            ((ApplicationBarIconButton)appbar.Buttons[0]).Text = YapperChat.Resources.Strings.NewGroupText;
            ((ApplicationBarIconButton)appbar.Buttons[1]).Text = YapperChat.Resources.Strings.InviteText;

            ((ApplicationBarMenuItem)appbar.MenuItems[0]).Text = YapperChat.Resources.Strings.SignoutText;
            ((ApplicationBarMenuItem)appbar.MenuItems[1]).Text = YapperChat.Resources.Strings.SettingsText;
            ((ApplicationBarMenuItem)appbar.MenuItems[2]).Text = YapperChat.Resources.Strings.TutorialText;

            ApplicationBar = (ApplicationBar)this.Resources["ChatContactsApplicationBar"];
        }
    }
}