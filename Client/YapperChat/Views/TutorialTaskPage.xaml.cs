﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace YapperChat.Views
{
    public partial class TutorialTaskPage : PhoneApplicationPage
    {
        private string returnPage; 

        public TutorialTaskPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (NavigationContext.QueryString.ContainsKey("page"))
            {
                this.returnPage = NavigationContext.QueryString["page"];
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.returnPage))
            {
                NavigationService.Navigate(new Uri("/Views/TutorialTaskCompletion.xaml", UriKind.Relative));
            }
            else
            {
                NavigationService.Navigate(new Uri("/Views/TutorialTaskCompletion.xaml?page=" + returnPage, UriKind.Relative));
            }
        }
    }
}