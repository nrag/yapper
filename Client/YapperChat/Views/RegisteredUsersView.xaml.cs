using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using YapperChat.ViewModels;
using Microsoft.Phone.Tasks;
using YapperChat.Models;

namespace YapperChat.Views
{
    public partial class RegisteredUsersView : PhoneApplicationPage
    {
        public RegisteredUsersView()
        {
            InitializeComponent();
            this.DataContext = ViewModelLocator.Instance.CreateOrGetViewModel<RegisteredUsersViewModel>();
        }

        private void NewChat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1)
            {
                return;
            }

            UserModel selectedUser = (UserModel)((LongListSelector)sender).SelectedItem;

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

            NavigationService.Navigate(new Uri(string.Format("/Views/ConversationMessagesView.xaml?conversationId={0}&recipientId={1}&recipientName={2}&isGroup={3}&pivot=0", conversationId, selectedUser.Id, selectedUser.Name, selectedUser.UserType == UserType.Group), UriKind.Relative));
        }

        private void RegisteredUsersPage_Loaded(object sender, RoutedEventArgs e)
        {
            //Temp code to add contacts in the emulator
            // commenting out below as it tries to make a call when there are no existing conversations and the + is selected from the main menu
            /* 
            PhoneCallTask task = new PhoneCallTask();
            task.PhoneNumber = "000000000";
            task.DisplayName = "Test user";
            task.Show();
             */
        }
    }
}