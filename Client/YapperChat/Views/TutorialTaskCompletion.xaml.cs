using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using ImageTools;
using ImageTools.IO.Gif;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Windows.Storage;
using YapperChat.Models;

namespace YapperChat.Views
{
    public partial class TutorialTaskCompletion : PhoneApplicationPage
    {
        private string returnPage;

        public  TutorialTaskCompletion()
        {
            InitializeComponent();
            ImageTools.IO.Decoders.AddDecoder<GifDecoder>();
            this.DataContext = this;

            this.LoadGifs();
        }

        private async void LoadGifs()
        {
            StorageFile deleteFile = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Images/TaskDeleteAnimation.gif", UriKind.RelativeOrAbsolute));
            ExtendedImage delteImage = new ExtendedImage();
            delteImage.SetSource(await deleteFile.OpenStreamForReadAsync());
            this.TaskDeleteAnimation.Source = delteImage;

            StorageFile completeFile = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Images/TaskCompleteAnimation.gif", UriKind.RelativeOrAbsolute));
            ExtendedImage completeImage = new ExtendedImage();
            completeImage.SetSource(await completeFile.OpenStreamForReadAsync());
            this.TaskCompleteAnimation.Source = completeImage;
        }

        public Uri TutorialTaskDelete
        {
            get
            {
                return new Uri("/Images/TaskDeleteAnimation.gif", UriKind.RelativeOrAbsolute);
            }
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
            UserSettingsModel.Instance.TutorialSeen = true;
            if (StringComparer.OrdinalIgnoreCase.Compare(returnPage, "message") == 0)
            {
                NavigationService.Navigate(new Uri("/Views/YapperChatMessagesPivot.xaml", UriKind.Relative));
            }
            else if (StringComparer.OrdinalIgnoreCase.Compare(returnPage, "task") == 0)
            {
                NavigationService.Navigate(new Uri("/Views/YapperChatTaskPivot.xaml", UriKind.Relative));
            }
            else if (StringComparer.OrdinalIgnoreCase.Compare(returnPage, "contacts") == 0)
            {
                NavigationService.Navigate(new Uri("/Views/YapperChatContactsPivot.xaml", UriKind.Relative));
            }
            else
            {
                NavigationService.Navigate(new Uri("/Views/YapperChatMessagesPivot.xaml", UriKind.Relative));
            }
        }
    }
}