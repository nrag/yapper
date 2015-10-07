using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;
using System.IO;
using Microsoft.Phone;

namespace YapperChat.ViewModels
{
    public class DisplayFullImageViewModel : INotifyPropertyChanged
    {
        WriteableBitmap wimg;
        string image;

        public DisplayFullImageViewModel()
        {
        }

        public DisplayFullImageViewModel(string img)
        {
            this.image = img;
            this.wimg = getImageFromIsolatedStorage(image);
        }

        public WriteableBitmap FullImage
        {
            get
            {
                return wimg;
            }
        }

        public int ImageHeight
        {
            get
            {
                return this.FullImage == null ? 0 : this.FullImage.PixelHeight;
            }
        }

        public int ImageWidth
        {
            get
            {
                return this.FullImage == null ? 0 : this.FullImage.PixelWidth;
            }
        }

        public void LoadImage()
        {
            NotifyPropertyChanged("FullImage");
        }

        private WriteableBitmap getImageFromIsolatedStorage(string imagename)
        {
            try
            {
                var isoFile = IsolatedStorageFile.GetUserStoreForApplication();
                using (var imageStream = isoFile.OpenFile(imagename, FileMode.Open, FileAccess.Read))
                {
                    var imageSource = PictureDecoder.DecodeJpeg(imageStream);
                    return new WriteableBitmap(imageSource);
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
