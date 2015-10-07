using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Coding4Fun.Toolkit.Controls.Converters;
using System.Globalization;

namespace YapperChat.Controls.Converters
{
    public class ColorToHexConverter : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture, string language)
        {
            Color color = (Color)value;

            return color.ToString();
        }
    }
}
