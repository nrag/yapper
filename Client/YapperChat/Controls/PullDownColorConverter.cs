using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Coding4Fun.Toolkit.Controls.Converters;
using YapperChat.Resources;

namespace YapperChat.Controls.Converters
{
    public class PullDownColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string taskName = value as string;
            if (taskName != null && (string.Compare(taskName, Strings.PullMeDownMessage) == 0 ||
                string.Compare(taskName, Strings.ReleaseToCreateNewItem) == 0))
            {
                return (SolidColorBrush)YapperChat.App.Current.Resources["PhoneAccentBrush"];
            }

            return (SolidColorBrush)YapperChat.App.Current.Resources["PhoneBackgroundBrush"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}