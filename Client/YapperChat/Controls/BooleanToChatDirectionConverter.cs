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
using Coding4Fun.Toolkit.Controls;
using System.Windows.Data;
using System.Text.RegularExpressions;

namespace YapperChat.Controls.Converters
{
    public class BooleanToChatPropertyConverter : ValueConverter
    {
        public BooleanToChatPropertyConverter()
        {
        }

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture, string language)
        {
            if (parameter == null)
            {
                return 0;
            }

            if (parameter.ToString() == "direction")
            {
                return ((bool)value) ? ChatBubbleDirection.LowerRight : ChatBubbleDirection.UpperLeft;
            }

            if (parameter.ToString() == "opacity")
            {
                return ((bool)value) ? 1 : 0.5;
            }

            if (parameter.ToString() == "alignment")
            {
                HorizontalAlignment align = ((bool)value) ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                return align;
            }

            if (parameter.ToString() == "background")
            {
                Brush phoneAccentBrush = (Brush)Application.Current.Resources["PhoneAccentBrush"];
                Brush phoneAccentContrastBrush = (Brush)Application.Current.Resources["YapperAccentContrastBrush"];
                return ((bool)value) ? phoneAccentBrush : phoneAccentContrastBrush;
            }

            return null;
        }
    }
}
