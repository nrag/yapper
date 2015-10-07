using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace YapperChat.Controls
{
    public partial class ImageAndUnreadCount : UserControl
    {
        private static BitmapImage defaultImage = new BitmapImage(new Uri("/Images/default.profile.png", UriKind.RelativeOrAbsolute));
        public static DependencyProperty UnreadCountProperty = DependencyProperty.Register("UnreadCount", typeof(int), typeof(ImageAndUnreadCount), new PropertyMetadata((int)0, ImageAndUnreadCount.OnPropertySet));
        public static DependencyProperty ImageProperty = DependencyProperty.Register("Image", typeof(BitmapImage), typeof(ImageAndUnreadCount), new PropertyMetadata(ImageAndUnreadCount.defaultImage, ImageAndUnreadCount.OnPropertySet));

        public ImageAndUnreadCount()
        {
            InitializeComponent();
            this.Animate();
        }

        public int UnreadCount
        {
            get
            {
                return (int)GetValue(UnreadCountProperty);
            }

            set
            {
                SetValue(UnreadCountProperty, value);
            }
        }

        public BitmapImage Image
        {
            get
            {
                return (BitmapImage)GetValue(ImageProperty);
            }

            set
            {
                SetValue(ImageProperty, value);
            }
        }

        private static void OnPropertySet(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            ImageAndUnreadCount imageAndUnread = (ImageAndUnreadCount)depObj;
            imageAndUnread.Animate();
        }

        private void liveTileAnimTop1_Part1_Completed(object sender, EventArgs e)
        {
            Storyboard anim = (Storyboard)this.Resources["liveTileAnimTop1_Part2"];
            anim.Begin();
        }

        private void liveTileAnimTop2_Part1_Completed(object sender, EventArgs e)
        {
            Storyboard anim = (Storyboard)this.Resources["liveTileAnimTop2_Part2"];
            anim.Begin();
        }

        private void liveTileAnimTop1_Part2_Completed(object sender, object e)
        {
            Storyboard anim = (Storyboard)this.Resources["liveTileAnimTop2_Part1"];
            anim.Begin();
        }

        private void liveTileAnimTop2_Part2_Completed(object sender, object e)
        {
            Storyboard anim = (Storyboard)this.Resources["liveTileAnimTop1_Part1"];
            anim.Begin();
        }

        private void Animate()
        {
            if (this.UnreadCount > 0)
            {
                Storyboard anim = (Storyboard)this.Resources["liveTileAnimTop1_Part1"];
                anim.Begin();
            }
        }
    }
}
