using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;
using System.IO;
using Microsoft.Phone;
using Windows.Devices.Geolocation;
using System.Device.Location;

namespace YapperChat.ViewModels
{
    public class DisplayLocationViewModel : INotifyPropertyChanged
    {
        double latitude;
        double longitude;

        public DisplayLocationViewModel()
        {
        }

        public DisplayLocationViewModel(double latitude, double longitude)
        {
            this.latitude = latitude;
            this.longitude = longitude;
        }

        public double Latitude
        {
            get
            {
                return latitude;
            }
        }

        public double Longitude
        {
            get
            {
                return longitude;
            }
        }

        public GeoCoordinate DisplayGeoLocation
        {
            get
            {
                return new GeoCoordinate(latitude, longitude);
            }
        }

        public string CenterLocation
        {
            get
            {
                return Convert.ToString(latitude) + "," + Convert.ToString(longitude);
            }
        }

        public void LoadMap()
        {
            NotifyPropertyChanged("latitude");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
