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
    public partial class YapperChatMessagesPivot : PhoneApplicationPage
    {
        /// <summary>
        /// Creates an instance of YapperChatView. This is the main page for
        /// YapperChat. The pivot view displays the chats and the contacts 
        /// registered with Yapper
        /// </summary>
        public YapperChatMessagesPivot()
        {
            InitializeComponent();
            Messenger.Default.Send<SuspendTaskCountEvent>(new SuspendTaskCountEvent() { Suspend = true });

            YapperChatViewModel viewModel = new YapperChatViewModel(true);
            this.DataContext = viewModel;

            // subscribe to push notifications
            PushNotification.PushNotification.Instance.Setup();

            // CollectionViewSource is used so that the list of conversations is maintained in the
            // sorted order.
            this.FavoritesCollection = new System.Windows.Data.CollectionViewSource();
            this.FavoritesCollection.Source = viewModel.AllConversations.Conversations;
            System.ComponentModel.SortDescription favoritesSort = new System.ComponentModel.SortDescription("LastPostUtcTime", System.ComponentModel.ListSortDirection.Descending);
            this.FavoritesCollection.SortDescriptions.Add(favoritesSort);
            this.FavoritesListBox.ItemsSource = this.FavoritesCollection.View;
            
            BuildLocalizedApplicationBar();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            UserSettingsModel.Instance.LastMessagePageViewTime = DateTime.Now;
            Messenger.Default.Send<SuspendTaskCountEvent>(new SuspendTaskCountEvent() { Suspend = false });
            Messenger.Default.Unregister<SyncEvent>(this);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            YapperChatViewModel viewModel = (YapperChatViewModel)this.DataContext;
            viewModel.AllConversations.SelectedConversation = null;
            Messenger.Default.Register<SyncEvent>(this, this.HandleSyncCompleteEvent);
        }

        private void HandleSyncCompleteEvent(SyncEvent syncComplete)
        {
            DispatcherHelper.InvokeOnUiThread(
                    () =>
                    {
                        YapperChatViewModel viewModel = (YapperChatViewModel)this.DataContext;

                        if (viewModel.AllConversations == null || viewModel.AllConversations.Conversations == null || viewModel.AllConversations.Conversations.Count == 0)
                        {
                            NavigationService.Navigate(new Uri(string.Format("/Views/YapperChatContactsPivot.xaml"), UriKind.Relative));
                        }
                    });
        }

        private void AddGroup_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/Views/AddNewGroupView.xaml"), UriKind.Relative));
        }

        private void StartNewChat_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/Views/RegisteredUsersView.xaml"), UriKind.Relative));
        }

        /// <summary>
        /// Handles the Click event for the Contacts page application bar 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InviteFriendsAppBarButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/Views/InviteFriends.xaml"), UriKind.Relative));
        }

        /// <summary>
        /// Handles the BackKeyPress for the YapperChatPivotView. If this is not handled,
        /// the first time when a user signs up pressing back key will take the user back to
        /// the signup page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            while (NavigationService.CanGoBack)
            {
                NavigationService.RemoveBackEntry();
            }
        }

        /// <summary>
        /// Handles the listview selection in the chat page. This navigates to show the list of messages in the 
        /// conversation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Contact_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1)
            {
                return;
            }

            UserModel selectedUser = (UserModel)((LongListSelector)sender).SelectedItem;

            // Do not navigate, if user does not exist
            if (selectedUser != null)
            {
                Uri navigationUri = this.GetConversationMessagesViewUri(selectedUser);

                if (navigationUri != null)
                {
                    NavigationService.Navigate(navigationUri);
                }
            }
        }

        private void GroupsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1)
            {
                return;
            }

            GroupModel selectedGroup = (GroupModel)(((LongListSelector)sender)).SelectedItem;
            Uri navigationUri = this.GetConversationMessagesViewUri(selectedGroup);

            if (navigationUri != null)
            {
                NavigationService.Navigate(navigationUri);
            }
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
            NavigationService.Navigate(new Uri("/Views/TutorialPageWelcome.xaml?page=message", UriKind.Relative));
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

        private void NavigateToConversationView(object sender)
        {
            if ((sender as ListBox).SelectedIndex < 0)
            {
                return;
            }

            ConversationModel selectedItemData = (sender as ListBox).SelectedItem as ConversationModel;
            if (selectedItemData != null && NavigationService != null)
            {
                var uriString = GetConversationMessagesViewUri(selectedItemData);

                if (uriString != null)
                {
                    NavigationService.Navigate(
                        new Uri(
                            uriString,
                            UriKind.Relative));
                    selectedItemData.UnreadCount = 0;
                }
            }

            (sender as ListBox).SelectedIndex = -1;
        }

        private string GetConversationMessagesViewUri(ConversationModel selectedItemData)
        {
            YapperChatViewModel viewModel = (YapperChatViewModel)this.DataContext;
            viewModel.AllConversations.SelectedConversation = selectedItemData;
            string uriString = null;
            UserModel recipient = null;
            foreach (UserModel user in selectedItemData.ConversationParticipants)
            {
                if (!user.Equals(UserSettingsModel.Instance.Me))
                {
                    recipient = user;
                }
            }

            if (recipient != null)
            {
                uriString =
                    string.Format(
                        "/Views/ConversationMessagesView.xaml?conversationId={0}&recipientName={1}&recipientId={2}&isGroup={3}",
                        selectedItemData.ConversationId, recipient.Name, recipient.Id, selectedItemData.IsGroupConversation);
            }

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

        private void ConversationList_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigateToConversationView(sender);
        }

        private static string GetUserName(ConversationModel selectedItem)
        {
            return selectedItem.ConversationParticipants.Select(userModel => userModel.Name).LastOrDefault();
        }

        private void StackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            //StackPanel stackPanel = sender as StackPanel;
            //Storyboard anim = (Storyboard)stackPanel.Resources["liveTileAnim1"];
            //anim.Begin();
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);    //we’ve reached the end of the tree
            if (parentObject == null)
            {
                return null;
            }

            //check if the parent matches the type we’re looking for
            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                return FindParent<T>(parentObject);
            }
        }

        private void TaskPivotView_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/Views/YapperChatTaskPivot.xaml"), UriKind.Relative));
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

        private void BuildLocalizedApplicationBar()
        {
            ApplicationBar appbar = (ApplicationBar)this.Resources["MessagePivotApplicationBar"];
            ((ApplicationBarIconButton)appbar.Buttons[0]).Text = YapperChat.Resources.Strings.InviteText;
            ((ApplicationBarIconButton)appbar.Buttons[1]).Text = YapperChat.Resources.Strings.NewChatText;

            ((ApplicationBarMenuItem)appbar.MenuItems[0]).Text = YapperChat.Resources.Strings.SignoutText;
            ((ApplicationBarMenuItem)appbar.MenuItems[1]).Text = YapperChat.Resources.Strings.SettingsText;
            ((ApplicationBarMenuItem)appbar.MenuItems[2]).Text = YapperChat.Resources.Strings.TutorialText;

            ApplicationBar = (ApplicationBar)this.Resources["MessagePivotApplicationBar"];
        }
    }
}