using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using YapperChat.Models;
using YapperChat.Resources;
using YapperChat.ViewModels;

namespace YapperChat.Views
{
    public partial class TutorialPageWelcome : PhoneApplicationPage
    {
        string returnPage;

        public TutorialPageWelcome()
        {
            InitializeComponent();
            this.DataContext = this;
            AllConversationsViewModel conversations = ViewModelLocator.Instance.CreateOrGetViewModel<AllConversationsViewModel>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (NavigationContext.QueryString.ContainsKey("page"))
            {
                this.returnPage = NavigationContext.QueryString["page"];
            }
        }

        public string UserName
        {
            get
            {
                return UserSettingsModel.Instance.MyName;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.returnPage))
            {
                NavigationService.Navigate(new Uri("/Views/TutorialYapperIntroduction.xaml", UriKind.Relative));
            }
            else
            {
                NavigationService.Navigate(new Uri("/Views/TutorialYapperIntroduction.xaml?page=" + returnPage, UriKind.Relative));
            }
        }
    }
}