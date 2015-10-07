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
using YapperChat.Resources;

namespace YapperChat.Views
{
    public partial class EnterConfirmationCodeView : PhoneApplicationPage
    {
        public EnterConfirmationCodeView()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ((EnterConfirmationCodeViewModel)this.DataContext).Validate(this.ConfirmationCodeTextBox.Text);
            Messenger.Default.Register<VerificationCodeValidationCompleteEvent>(this, this.ValidationComplete);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/NewUserRegistrationView.xaml", UriKind.Relative));
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/NewUserRegistrationView.xaml", UriKind.Relative));
        }

        private void ValidationComplete(VerificationCodeValidationCompleteEvent validation)
        {
            if (validation.Success)
            {
                NavigationService.Navigate(new Uri("/Views/TutorialPageWelcome.xaml", UriKind.Relative));
            }
            else
            {
                MessageBox.Show(Strings.WrongValidationCode);
                //NavigationService.Navigate(new Uri("/Views/NewUserRegistrationView.xaml", UriKind.Relative));
            }
        }
    }
}