using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using Microsoft.Phone.UserData;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using PhoneNumbers;

using YapperChat.Models;
using System.Windows.Threading;
using YapperChat.Common;

namespace YapperChat.ViewModels
{
    /// <summary>
    /// This page shows a jump list view for choosing a contact
    /// </summary>
    public class PhoneContactsJumpListViewModel : INotifyPropertyChanged
    {
        private bool IsInitialized = false;

        private IEnumerable<ContactGroup<ContactItem>> items;

        /// <summary>
        /// Gets or sets the list of contact groups.
        /// </summary>
        /// <value>
        /// The list of contact groups.
        /// </value>
        public IEnumerable<ContactGroup<ContactItem>> Items
        {
            get
            {
                return this.items;
            }

            set
            {
                items = value;
                NotifyPropertyChanged("Items");
            }
        }

        public bool IsLoading
        {
            get
            {
                return !IsInitialized;
            }
        }

        public bool IsContactsEmpty
        {
            get
            {
                if (IsInitialized == false)
                {
                    return false;
                }

                if (items == null || items.Count() == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsContactsNotEmpty
        {
            get
            {
                if (IsInitialized == false)
                {
                    return false;
                }

                return !IsContactsEmpty;
            }
        }

        RegisteredUsersViewModel vm;

        public PhoneContactsJumpListViewModel()
        {
             vm = new RegisteredUsersViewModel();
        }

        private bool IsContactRegisteredUser(Contact contactUser)
        {
            char firstLetter = char.ToLower(contactUser.DisplayName[0]);

            foreach (ContactGroup<UserModel> group in this.vm.RegisteredUsers)
            {
                if (group.FirstLetter == firstLetter)
                {
                    foreach (UserModel user in group)
                    {
                        if (!string.IsNullOrEmpty(user.PhoneNumber))
                        {
                            if (IsAnyPhoneNumberMatching(contactUser, user))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public bool IsAnyPhoneNumberMatching(Contact contactUser, UserModel registereduser)
        {
            foreach (var pn in contactUser.PhoneNumbers)
            {
                PhoneNumbers.PhoneNumber p1;
                PhoneNumbers.PhoneNumber p2;

                try
                {
                    p1 = PhoneNumbers.PhoneNumberUtil.GetInstance().Parse(registereduser.PhoneNumber, "US");
                    p2 = PhoneNumbers.PhoneNumberUtil.GetInstance().Parse(pn.PhoneNumber, "US");
                }
                catch (NumberParseException)
                {
                    return false;
                }

                if (p1.Equals(p2))
                {
                    return true;
                }
            }

            return false;
        }

        public void Search()
        {
            var contacts = new Contacts();

            contacts.SearchCompleted += (s, args) =>
            {
                ObservableSortedList<ContactGroup<ContactItem>> tempItems = new ObservableSortedList<ContactGroup<ContactItem>>();

                var groups = new Dictionary<char, ContactGroup<ContactItem>>();

                foreach (var contact in args.Results)
                {
                    if (!contact.PhoneNumbers.Any(number => number.Kind == PhoneNumberKind.Mobile))
                    {
                        continue;
                    }

                    if (IsContactRegisteredUser(contact))
                    {
                        continue;
                    }

                    char firstLetter = char.ToLower(contact.DisplayName[0]);

                    // show # for numbers
                    if (firstLetter >= '0' && firstLetter <= '9')
                    {
                        firstLetter = '#';
                    }

                    // create group for letter if it doesn't exist
                    if (!groups.ContainsKey(firstLetter))
                    {
                        var group = new ContactGroup<ContactItem>(firstLetter);
                        tempItems.Add(group);
                        groups[firstLetter] = group;
                    }

                    // create a contact for item and add it to the relevant group
                    var contactItem = new ContactItem(contact);
                    groups[firstLetter].Add(contactItem);
                }

                this.items = tempItems;
                Deployment.Current.Dispatcher.BeginInvoke(() => { this.NotifyPropertyChanged("Items"); });
                IsInitialized = true;
                NotifyPropertyChanged("IsContactsEmpty");
                NotifyPropertyChanged("IsContactsNotEmpty");
                NotifyPropertyChanged("IsLoading");
            };

            // get all contacts
            contacts.SearchAsync(null, FilterKind.None, null);
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