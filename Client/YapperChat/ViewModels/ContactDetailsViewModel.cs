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
using Microsoft.Phone.UserData;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace YapperChat.ViewModels
{
    public class ContactDetailsViewModel : INotifyPropertyChanged
    {
        private Contact contact;

        private bool _loaded = false;

        public string YapperName
        {
            get;
            set;
        }

        public string YapperPhone
        {
            get;
            set;
        }

        public int UserId
        {
            get;
            set;
        }

        public string MobilePhone
        {
            get
            {
                if (contact == null)
                {
                    return this.YapperPhone;
                }

                foreach (ContactPhoneNumber phone in this.contact.PhoneNumbers)
                {
                    if (phone.Kind == PhoneNumberKind.Mobile)
                    {
                        return phone.PhoneNumber;
                    }
                }

                return null;
            }
        }

        public string HomePhone
        {
            get
            {
                if (contact == null)
                {
                    return null;
                }

                foreach (ContactPhoneNumber phone in this.contact.PhoneNumbers)
                {
                    if (phone.Kind == PhoneNumberKind.Home)
                    {
                        return phone.PhoneNumber;
                    }
                }

                return null;
            }
        }


        public BitmapImage Picture
        {
            get
            {
                if (this.contact != null && this.contact.GetPicture() != null)
                {
                    BitmapImage img = new BitmapImage();
                    img.SetSource(this.contact.GetPicture());
                    return img;
                }

                return null;
            }
        }

        public bool Loaded
        {
            get
            {
                return this._loaded;
            }

            private set
            {
                this._loaded = value;
                this.NotifyPropertyChanged("Loaded");
                this.NotifyPropertyChanged("HomePhone");
                this.NotifyPropertyChanged("MobilePhone");
                this.NotifyPropertyChanged("Picture");
            }
        }

        public void Search()
        {
            if (this.Loaded)
            {
                return;
            }

            var contacts = new Contacts();

            contacts.SearchCompleted += (s, args) =>
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (args.Results != null)
                    {
                        foreach (var phoneContact in args.Results)
                        {
                            this.contact = phoneContact;
                            break;
                        }
                    }

                    this.Loaded = true;
                });
            };

            if (!string.IsNullOrEmpty(this.YapperPhone))
            {
                // get all contacts
                contacts.SearchAsync(this.YapperPhone, FilterKind.PhoneNumber, null);
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
