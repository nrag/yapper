using Microsoft.Phone.UserData;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.IO;
using System;

namespace YapperChat.Models
{
    /// <summary>
    /// Holds a contact item
    /// </summary>
    public class ContactItem : INotifyPropertyChanged, IComparable
    {
        private Contact _contact;
        private BitmapSource _image;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContactItem"/> class.
        /// </summary>
        /// <param name="contact">The contact.</param>
        public ContactItem(Contact contact)
        {
            Contact = contact;
        }

        /// <summary>
        /// Gets the contact.
        /// </summary>
        public string DisplayName
        {
            get
            {
                return _contact.DisplayName;
            }
        }

        /// <summary>
        /// Gets the contact.
        /// </summary>
        public Contact Contact
        {
            get
            {
                return _contact;
            }

            private set
            {
                if (_contact != value)
                {
                    _contact = value;

                    RaisePropertyChanged("Contact");
                }
            }
        }

        /// <summary>
        /// Gets the contact image.
        /// </summary>
        public BitmapSource Image
        {
            get
            {
                if (Contact != null)
                {
                    if (_image == null)
                    {
                        Stream stream = Contact.GetPicture();
                        if (stream != null)
                        {
                            var bmp = new BitmapImage();
                            bmp.SetSource(stream);

                            _image = bmp;

                            RaisePropertyChanged("Image");
                        }
                    }

                    return _image;
                }

                return null;
            }
        }

        public int CompareTo(object obj)
        {
            if (!(obj is ContactItem))
            {
                return -1;
            }

            ContactItem other  = obj as ContactItem;

            if (this.Contact != null && other.Contact != null)
            {
                return string.CompareOrdinal(this.Contact.DisplayName, other.Contact.DisplayName);
            }

            if (this.Contact == null && other.Contact == null)
            {
                return 0;
            }

            if (this.Contact == null)
            {
                return 1;
            }

            return -1;
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
