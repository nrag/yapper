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
using WPControls;

namespace YapperChat.Controls.Converters
{
    public class ColorConverter : IDateToBrushConverter
    {
        public Brush Convert(DateTime dateTime, bool isSelected, Brush defaultValue, BrushType brushType)
        {
            if (isSelected)
            {
                return brushType == BrushType.Background ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
            }

            return defaultValue;
        }
    }
}