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
using Microsoft.Phone.Controls.Primitives;
using YapperChat.Models;

namespace YapperChat.Views
{
    public partial class UserSettingsView : PhoneApplicationPage
    {
        public UserSettingsView()
        {
            InitializeComponent();
        }

        private void PushnotificationToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            bool isChecked = ((ToggleSwitchButton)e.OriginalSource).IsChecked.HasValue ? ((ToggleSwitchButton)e.OriginalSource).IsChecked.Value : true;

            ((UserSettingsViewModel)this.DataContext).ChangePushNotificationSettings(isChecked);
        }

        private void LocationToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            bool isChecked = ((ToggleSwitchButton)e.OriginalSource).IsChecked.HasValue ? ((ToggleSwitchButton)e.OriginalSource).IsChecked.Value : true;
            ((UserSettingsViewModel)this.DataContext).ChangeLocationSettings(isChecked);
        }

        private void HideCompletedTasksToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            bool isChecked = ((ToggleSwitchButton)e.OriginalSource).IsChecked.HasValue ? ((ToggleSwitchButton)e.OriginalSource).IsChecked.Value : true;
            ((UserSettingsViewModel)this.DataContext).ChangeHideCompletedTasksSettings(isChecked);
        }

        private void DebugToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            bool isChecked = ((ToggleSwitchButton)e.OriginalSource).IsChecked.HasValue ? ((ToggleSwitchButton)e.OriginalSource).IsChecked.Value : true;
            ((UserSettingsViewModel)this.DataContext).ChangeDebugSettings(isChecked);
        }

        private void DeleteAllEmailsFromPhoneButton_Click(object sender, RoutedEventArgs e)
        {
            Sync.DataSync.Instance.DeleteAllMessagesFromPhone();
            UserSettingsModel.Instance.LastSyncDateTime = DateTime.Now.AddDays(-7);
            //Sync.DataSync.Instance.Sync();
        }
    }
}