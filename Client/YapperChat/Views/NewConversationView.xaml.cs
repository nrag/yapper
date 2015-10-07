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
using GalaSoft.MvvmLight.Messaging;
using YapperChat.EventMessages;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Shell;

namespace YapperChat.Views
{
    public partial class NewConversation : PhoneApplicationPage
    {
        public NewConversation()
        {
            InitializeComponent();

            if (this.DataContext == null)
            {
                this.DataContext = new NewConversationViewModel();
            }

            BuildLocalizedApplicationBar();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string recipientName = null;
            if (NavigationContext.QueryString.ContainsKey("recipientName"))
            {
                recipientName = NavigationContext.QueryString["recipientName"];
            }

            int recipientId = -1;
            if (NavigationContext.QueryString.ContainsKey("recipientId"))
            {
                recipientId = Int32.Parse(NavigationContext.QueryString["recipientId"]);
            }

            int pivot = 0;
            if (NavigationContext.QueryString.ContainsKey("pivot"))
            {
                pivot = Int32.Parse(NavigationContext.QueryString["pivot"]);
            }

            ((NewConversationViewModel)this.DataContext).Recipient = new Models.UserModel() { Id = recipientId, Name = recipientName };
            ((NewConversationViewModel)this.DataContext).LoadContactDetails();

            this.NewConversationPivot.SelectedIndex = pivot;
        }

        private void NewConversationPage_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void SendMessage_Click(object sender, EventArgs e)
        {
            this.SendMessage();
        }

        private void SendMessage()
        {
            if (string.IsNullOrEmpty(this.NewConversationTextBox.Text))
            {
                MessageBox.Show("Enter a message");
            }
            else
            {
                ((NewConversationViewModel)this.DataContext).SendNewConversation(this.NewConversationTextBox.Text);
                Messenger.Default.Register<MessageSentEvent>(this, this.ClearTextBox);
            }
        }

        private void ClearTextBox(MessageSentEvent messageSent)
        {
            if (messageSent.Success)
            {
                bool isGroup = ((NewConversationViewModel)this.DataContext).Recipient.UserType == Models.UserType.Group;
                NavigationService.Navigate(new Uri(string.Format("/Views/ConversationMessagesView.xaml?conversationId={0}&recipientName={1}&recipientId={2}&isGroup={3}", messageSent.ConversationId, messageSent.Recipient.Name, messageSent.Recipient.Id, isGroup), UriKind.Relative));
            }

            Messenger.Default.Unregister<MessageSentEvent>(this);
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/YapperChatMessagesPivot.xaml", UriKind.Relative));
        }

        private void NewMessageTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.GridScrollViewer.ScrollToVerticalOffset(this.GridScrollViewer.ExtentHeight);
        }

        private void CallMobileButton_Click(object sender, RoutedEventArgs e)
        {
            PhoneCallTask phoneCallTask = new PhoneCallTask();

            phoneCallTask.PhoneNumber = ((ContactDetailsViewModel)this.DataContext).MobilePhone;
            phoneCallTask.DisplayName = ((ContactDetailsViewModel)this.DataContext).YapperName;

            phoneCallTask.Show();
        }

        private void CallHomeButton_Click(object sender, RoutedEventArgs e)
        {
            PhoneCallTask phoneCallTask = new PhoneCallTask();

            phoneCallTask.PhoneNumber = ((ContactDetailsViewModel)this.DataContext).HomePhone;
            phoneCallTask.DisplayName = ((ContactDetailsViewModel)this.DataContext).YapperName;

            phoneCallTask.Show();
        }

        private void BuildLocalizedApplicationBar()
        {
            ApplicationBar appbar = (ApplicationBar)this.Resources["NewChatApplicationBar"];
            ((ApplicationBarIconButton)appbar.Buttons[0]).Text = YapperChat.Resources.Strings.NewChatText;
        }
    }
}