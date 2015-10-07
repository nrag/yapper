using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Controls.Primitives;
using Microsoft.Phone.Shell;
using YapperChat.Controls;

namespace YapperChat.Views
{
    public partial class TimePickerPage : PhoneApplicationPage, IDateTimePickerPage
    {
        public TimePickerPage()
        {
            InitializeComponent();
            List<int> hours24 = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23 };
            List<int> hours12 = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            List<string> minutes = new List<string>() { "00", "30"};
            List<string> ampm = new List<string>() { "AM", "PM" };

            this.MinutesLoopingSelector.DataSource = new ListLoopingDataSource<string>() { Items = minutes, SelectedItem = "00" };
            this.AMPMLoopingSelector.DataSource = new ListLoopingDataSource<string>() { Items = ampm, SelectedItem = "AM" };

            if (DateTimeFormatInfo.CurrentInfo.ShortTimePattern.Contains("H"))
            {
                this.HoursLoopingSelector.DataSource = new ListLoopingDataSource<int>() { Items = hours24, SelectedItem = 0 };
                this.AMPMLoopingSelector.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.HoursLoopingSelector.DataSource = new ListLoopingDataSource<int>() { Items = hours12, SelectedItem = 1 };
                this.AMPMLoopingSelector.Visibility = Visibility.Visible;
            }
        }

        public void SetFlowDirection(FlowDirection flowDirection)
        {
        }

        private DateTime? value;
        public DateTime? Value
        {
            get
            {
                return this.value;
            }

            set
            {
                this.value = value;

                if (value != null)
                {
                    if (DateTimeFormatInfo.CurrentInfo.ShortTimePattern.Contains("H"))
                    {
                        ((ListLoopingDataSource<int>)this.HoursLoopingSelector.DataSource).SelectedItem = value.Value.Hour;
                    }
                    else
                    {
                        ((ListLoopingDataSource<int>)this.HoursLoopingSelector.DataSource).SelectedItem = int.Parse(value.Value.ToString("%h"));
                        ((ListLoopingDataSource<string>)this.AMPMLoopingSelector.DataSource).SelectedItem = value.Value.ToString("tt").ToUpper();
                    }

                    ((ListLoopingDataSource<string>)this.MinutesLoopingSelector.DataSource).SelectedItem = value.Value.ToString("mm");
                }
                
            }
        }

        private void ApplicationBarCancelButton_Click(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }

        private void ApplicationBarDoneButton_Click(object sender, EventArgs e)
        {
            int hour = (int)((ListLoopingDataSource<int>)this.HoursLoopingSelector.DataSource).SelectedItem;
            int minutes = int.Parse((string)((ListLoopingDataSource<string>)this.MinutesLoopingSelector.DataSource).SelectedItem);
            if (this.AMPMLoopingSelector.Visibility == Visibility.Visible)
            {
                string ampm = (string)((ListLoopingDataSource<string>)this.AMPMLoopingSelector.DataSource).SelectedItem;
                if (ampm == "PM")
                    hour += 12;
            }

            Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, minutes, 0);
            NavigationService.GoBack();
        }
    }
}