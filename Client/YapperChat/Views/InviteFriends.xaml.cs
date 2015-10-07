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
using Microsoft.Phone.Tasks;
using Microsoft.Phone.UserData;
using YapperChat.EventMessages;
using YapperChat.Models;
using YapperChat.Resources;
using YapperChat.ViewModels;

namespace YapperChat.Views
{
    public partial class InviteFriends : PhoneApplicationPage
    {
        public InviteFriends()
        {
            InitializeComponent();
            this.DataContext = new PhoneContactsJumpListViewModel();
            ((PhoneContactsJumpListViewModel)this.DataContext).Search();
        }

        private void AddMemberApplicationBarDone_Click(object sender, EventArgs e)
        {

        }

        private void InviteFriends_Click(object sender, RoutedEventArgs e)
        {
            string currentuser = UserSettingsModel.Instance.Me.Name;
            SmsComposeTask smstask = new SmsComposeTask();

            smstask.Body = string.Format(Strings.SmsInvite, currentuser);
            foreach (object user in this.ContactsListSelector.SelectedItems)
            {
                foreach(ContactPhoneNumber num in ((ContactItem)user).Contact.PhoneNumbers)
                {
                    if (num.Kind == PhoneNumberKind.Mobile)
                    {
                        smstask.To += num.PhoneNumber + ";";
                        break;
                    }
                }
            }

            smstask.Show();
        }

        private void InviteFriendsCancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri(String.Format("/views/YapperChatMessagesPivot.xaml"), UriKind.Relative));
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            NavigationService.Navigate(new Uri(String.Format("/views/YapperChatMessagesPivot.xaml"), UriKind.Relative));
        }
    }
}