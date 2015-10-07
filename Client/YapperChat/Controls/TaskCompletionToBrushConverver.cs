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
using YapperChat.Models;

namespace YapperChat.Controls.Converters
{
    public class TaskCompletionToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool completed = ((bool?)value).HasValue ? ((bool?)value).Value : false;
            string stringParameter = parameter as string;

            if (string.IsNullOrEmpty(stringParameter))
            {
                if (completed)
                {
                    return new SolidColorBrush(Colors.Green);
                }

                return new SolidColorBrush((Color)YapperChat.App.Current.Resources["PhoneAccentColor"]);
            }

            if (string.Compare(stringParameter, "opacity", StringComparison.OrdinalIgnoreCase) == 0)
            {
                if (completed)
                {
                    if (UserSettingsModel.Instance.HideCompletedTasksEnabled == true)
                    {
                        return 0;
                    }

                    return .4;
                }

                return 1.0;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}