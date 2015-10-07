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
using Microsoft.Phone.Controls;

namespace YapperChat.Controls.Converters
{
    public class NullToVisibilityConverter : ValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture, string language)
        {
            if (value is string)
            {
                if (string.IsNullOrEmpty((string)value))
                {
                    return Visibility.Collapsed;
                }

                return Visibility.Visible;
            }

            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    public static class WebBrowserHelper
    {
        public static readonly DependencyProperty HtmlProperty = DependencyProperty.RegisterAttached(
            "Html", typeof(string), typeof(WebBrowserHelper), new PropertyMetadata(OnHtmlChanged));

        public static string GetHtml(DependencyObject dependencyObject)
        {
            return (string)dependencyObject.GetValue(HtmlProperty);
        }

        public static void SetHtml(DependencyObject dependencyObject, string value)
        {
            dependencyObject.SetValue(HtmlProperty, value);
        }

        private static void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var browser = d as WebBrowser;

            if (browser == null)
                return;

            var html = e.NewValue.ToString();

            browser.NavigateToString(html);
        }
    }
}
