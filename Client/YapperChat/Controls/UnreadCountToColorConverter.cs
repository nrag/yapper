using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using Coding4Fun.Toolkit.Controls.Converters;

namespace YapperChat.Controls.Converters
{
    public class UnreadCountToColorConverter : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture, string language)
        {
            if (targetType != typeof(Brush))
                throw new InvalidOperationException("The target must be an Brush");

            Color color = (Color)App.Current.Resources["PhoneContrastForegroundColor"];

            Brush defaultColor = (Brush)App.Current.Resources["PhoneForegroundBrush"];

            if ((int)value == 0)
            {
                return defaultColor;
            }

            return new SolidColorBrush(color);
        }
    }
}
