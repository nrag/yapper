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
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace YapperChat.Models
{
    /// <summary>
    /// Controller that executes contact search in a serialized fashion
    /// </summary>
    public class ContactSearchController : IContactSearchController
    {
        private static IContactSearchController instance = new ContactSearchController();

        private List<ContactSearchArguments> searches = new List<ContactSearchArguments>();

        private object syncObject = new object();

        public static IContactSearchController Instance
        {
            get
            {
                return ContactSearchController.instance;
            }
        }

        public void StartSearch(ContactSearchArguments search)
        {
            lock (this.syncObject)
            {
                this.searches.Add(search);

                if (this.searches.Count == 1)
                {
                    this.RunSearch(search);
                }
            }
        }

        private void RunSearch(ContactSearchArguments search)
        {
            var contacts = new Contacts();

            contacts.SearchCompleted += (s, args) =>
                {
                    lock (this.syncObject)
                    {
                        YapperContactsSearchEventArgs eventArgs = new YapperContactsSearchEventArgs();
                        eventArgs.Filter = args.Filter;
                        eventArgs.FilterKind = args.FilterKind;
                        eventArgs.State = args.State;

                        switch (search.SearchKind)
                        {
                            case SearchKind.AllPhoneNumbers:
                                {
                                    List<string> userPhoneNumbers = new List<string>();
                                    foreach (var contact in args.Results)
                                    {
                                        foreach (var phoneNumber in contact.PhoneNumbers)
                                        {
                                            userPhoneNumbers.Add(phoneNumber.PhoneNumber);
                                        }
                                    }

                                    eventArgs.Results = userPhoneNumbers;
                                }
                                break;
                            case SearchKind.Picture:
                                {
                                    if (args.Results != null)
                                    {
                                        foreach (var phoneContact in args.Results)
                                        {
                                            if (phoneContact.GetPicture() != null)
                                            {
                                                BitmapImage image = new BitmapImage();
                                                image.SetSource(phoneContact.GetPicture());
                                                eventArgs.Results = image;
                                                break;
                                            }

                                            break;
                                        }
                                    }
                                }
                                break;
                            default:
                                break;
                        }

                        search.SearchCompleted(s, eventArgs);
                        this.searches.Remove(search);
                        if (this.searches.Count != 0)
                        {
                            this.RunSearch(this.searches[0]);
                        }
                    }
                };

            contacts.SearchAsync(search.Filter, search.FilterKind, search.State);
        }
    }
}
