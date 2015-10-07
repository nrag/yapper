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

namespace YapperChat.Views
{
    public partial class DisplayLocation : PhoneApplicationPage
    {
        public DisplayLocation()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            double latitude = 0;
            double longitude = 0;
            if (NavigationContext.QueryString.ContainsKey("Latitude"))
            {
                latitude = Convert.ToDouble(NavigationContext.QueryString["Latitude"]);
            }

            if (NavigationContext.QueryString.ContainsKey("Longitude"))
            {
                longitude = Convert.ToDouble(NavigationContext.QueryString["Longitude"]);
            }

            
            DisplayLocationViewModel cvm = new DisplayLocationViewModel(latitude, longitude);
            this.DataContext = cvm;
            LocationMap.SetView(cvm.DisplayGeoLocation, 15);
            cvm.LoadMap();
        }
    }
}