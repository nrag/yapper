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
using YapperChat.Models;
using YapperChat.EventMessages;
using GalaSoft.MvvmLight.Messaging;
using YapperChat.ViewModels;
using Microsoft.Phone.Shell;
using YapperChat.Sync;

namespace YapperChat
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (UserSettingsModel.Instance.UserId == -1)
            {
                NavigationService.Navigate(new Uri("/Views/NewUserRegistrationView.xaml", UriKind.Relative));
            }
            else if (string.IsNullOrEmpty(UserSettingsModel.Instance.Cookie))
            {
                NavigationService.Navigate(new Uri("/Views/EnterConfirmationCodeView.xaml", UriKind.Relative));
            }
            else if (UserSettingsModel.Instance.TutorialSeen)
            {
                NavigationService.Navigate(new Uri("/Views/YapperChatMessagesPivot.xaml", UriKind.Relative));
            }
            else
            {
                NavigationService.Navigate(new Uri("/Views/TutorialPageWelcome.xaml", UriKind.Relative));
            }

            var appTile = ShellTile.ActiveTiles.FirstOrDefault();
            if (appTile == null) return; //Don't create...just update

            appTile.Update(new StandardTileData() { BackTitle = "", BackContent = "", Count = 0 });
        }
    }
}