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

namespace YapperChat.Controls.Converters
{
    public class StringToBooleanConverter : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture, string language)
        {
            if (value is string)
            {
                if (string.IsNullOrEmpty((string)value))
                {
                    return false;
                }

                return true;
            }

            return false;
        }
    }
}
