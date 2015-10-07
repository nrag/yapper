using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using YapperChat.ViewModels;
using System.Windows.Media.Imaging;
using System.IO;
using Microsoft.Phone;
using System.IO.IsolatedStorage;

namespace YapperChat.Views
{
    public partial class DisplayFullImage : PhoneApplicationPage
    {
        public DisplayFullImage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            string img = null;
            if (NavigationContext.QueryString.ContainsKey("ImageSource"))
            {
                img = NavigationContext.QueryString["ImageSource"];
            }

            DisplayFullImageViewModel cvm = new DisplayFullImageViewModel(img);
            this.DataContext = cvm;
            cvm.LoadImage();
        }
    }
}