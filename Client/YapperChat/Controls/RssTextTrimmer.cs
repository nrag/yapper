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
using System.Collections.Generic;

namespace YapperChat.Controls.Converters
{
    public class RssTextTrimmer : IValueConverter
    {
        // Clean up text fields from each SyndicationItem. 
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;

            int maxLength = 200;
            int strLength = 0;
            string fixedString = "";

            GetHtmlImageUrlList(value.ToString());

            // Remove newline characters
            fixedString = Regex.Replace(value.ToString(), "<b>", "\n");
            fixedString = fixedString.Replace("</b>", "\n");
            fixedString = fixedString.Replace("<br />", "\n");

            // Remove HTML tags. 
            fixedString = Regex.Replace(fixedString, "<[^>]+>", "");

            // Remove newline characters
            fixedString = Regex.Replace(fixedString, @"[\r\n]{2,}", "\n"); ;


            // Remove encoded HTML characters
            fixedString = HttpUtility.HtmlDecode(fixedString);

            fixedString.Remove(fixedString.IndexOf('\n'), fixedString.IndexOf('\n', fixedString.IndexOf('\n') + 1));
            fixedString.Remove(fixedString.IndexOf('\n'), fixedString.IndexOf('\n', fixedString.IndexOf('\n') + 1));

            strLength = fixedString.ToString().Length;

            // Some feed management tools include an image tag in the Description field of an RSS feed, 
            // so even if the Description field (and thus, the Summary property) is not populated, it could still contain HTML. 
            // Due to this, after we strip tags from the string, we should return null if there is nothing left in the resulting string. 
            if (strLength == 0)
            {
                return null;
            }

            // Truncate the text if it is too long. 
            else if (strLength >= maxLength)
            {
                fixedString = fixedString.Substring(0, maxLength);

                // Unless we take the next step, the string truncation could occur in the middle of a word.
                // Using LastIndexOf we can find the last space character in the string and truncate there. 
                fixedString = fixedString.Substring(0, fixedString.LastIndexOf(" "));
            }

            fixedString += "...";

            return fixedString;
        }

        /// <summary> 
        /// Get the URL of the all pictures from the HTML. 
        /// </summary> 
        /// <param name="sHtmlText">HTML code</param> 
        /// <returns>URL list of the all pictures</returns> 
        public static List<ImageItem> GetHtmlImageUrlList(string sHtmlText)
        {
            // The definition of a regular expression to match img tag. 
            Regex regImg = new Regex(@"<img\b[^<>]*?\bsrc[\s\t\r\n]*=[\s\t\r\n]*[""']?[\s\t\r\n]*(?<imgUrl>[^\s\t\r\n""'<>]*)[^<>]*?/?[\s\t\r\n]*>", RegexOptions.IgnoreCase);

            // The search for a matching string.
            MatchCollection matches = regImg.Matches(sHtmlText);
            int i = 0;
            List<ImageItem> imgUrlList = new List<ImageItem>();

            // Get a list of matches
            foreach (Match match in matches)
            {
                imgUrlList.Add(new ImageItem("img" + i, match.Groups["imgUrl"].Value));
                i++;
            }
            return imgUrlList;
        }

        // This code sample does not use TwoWay binding and thus, we do not need to flesh out ConvertBack.  
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Image entity
    /// </summary>
    public class ImageItem
    {
        public ImageItem(string title, string url)
        {
            this.Title = title;
            this.URL = url;
        }

        public string Title { get; set; }
        public string URL { get; set; }
    }
}
