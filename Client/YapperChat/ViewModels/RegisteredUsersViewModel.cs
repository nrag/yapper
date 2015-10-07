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
using System.Collections.Generic;
using System.ComponentModel;
using YapperChat.Models;
using Microsoft.Phone.UserData;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Collections.ObjectModel;
using YapperChat.ServiceProxy;
using GalaSoft.MvvmLight.Messaging;
using YapperChat.EventMessages;
using YapperChat.Database;
using System.Data.Linq;
using YapperChat.Sync;
using YapperChat.Common;

namespace YapperChat.ViewModels
{
    public class RegisteredUsersViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Batch size
        /// </summary>
        private const int BatchSize = 50;

        /// <summary>
        /// List of registered users
        /// </summary>
        private ObservableSortedList<ContactGroup<UserModel>> _registeredUsers;

        /// <summary>
        /// Indicates if  initial load is complete
        /// </summary>
        private bool isLoading = true;

        /// <summary>
        /// 
        /// </summary>
        private List<string> userPhoneNumbers = new List<string>();

        private Dictionary<int, UserModel> userIdUserMap = new Dictionary<int, UserModel>();

        /// <summary>
        /// 
        /// </summary>
        private IServiceProxy serviceProxy;

        /// <summary>
        /// 
        /// </summary>
        private IContactSearchController contactSearchController;

        /// <summary>
        /// 
        /// </summary>
        public RegisteredUsersViewModel()
            : this(
                    YapperServiceProxy.Instance, 
                    UserSettingsModel.Instance,
                    ContactSearchController.Instance)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceProxy"></param>
        /// <param name="dataContext"></param>
        /// <param name="contactSearchController"></param>
        public RegisteredUsersViewModel(IServiceProxy serviceProxy, IUserSettings userSettings, IContactSearchController contactSearchController)
        {
            this.serviceProxy = serviceProxy;
            this.contactSearchController = contactSearchController;

            lock (DataSync.Instance)
            {
                if (!DataSync.Instance.IsSyncComplete)
                {
                    Messenger.Default.Register<SyncEvent>(this, this.ReadRegisteredUsersFromDB);
                }

                this.isLoading = !DataSync.Instance.IsUsersSyncComplete;
            }

            this.RegisteredUsers = new ObservableSortedList<ContactGroup<UserModel>>();
            this.ReadRegisteredUsersFromDB();
        }

        /// <summary>
        /// Gets or sets the list of contact groups.
        /// </summary>
        /// <value>
        /// The list of contact groups.
        /// </value>
        public ObservableSortedList<ContactGroup<UserModel>> RegisteredUsers
        {
            get
            {
                return this._registeredUsers;
            }

            private set
            {
                this._registeredUsers = value;
                this.NotifyPropertyChanged("RegisteredUsers");
            }
        }

        /// <summary>
        /// Returns true if the list of users are being loaded.
        /// </summary>
        public bool IsLoading
        {
            get
            {
                return this.isLoading;
            }
            private set
            {
                this.isLoading = value;
                this.NotifyPropertyChanged("IsLoading");
            }
        }

        public UserModel GetUser(int id)
        {
            if (this.userIdUserMap.ContainsKey(id))
            {
                return this.userIdUserMap[id];
            }

            return null;
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

        private bool Contains(UserModel contact)
        {
            char firstLetter = char.ToLower(contact.Name[0]);
            foreach (ContactGroup<UserModel> group in this.RegisteredUsers)
            {
                if (group.FirstLetter == firstLetter)
                {
                    foreach (UserModel user in group)
                    {
                        if (user.Id == contact.Id)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="syncCompleteEvent"></param>
        private void ReadRegisteredUsersFromDB(SyncEvent syncCompleteEvent)
        {
            this.isLoading = false;
            DispatcherHelper.InvokeOnUiThread(() =>
                {
                    try
                    {
                        this.ReadRegisteredUsersFromDB();
                    }
                    catch (Exception)
                    {
                    }

                    this.NotifyPropertyChanged("IsLoading");
                });
        }

        /// <summary>
        /// Read the registered users from the local data base.
        /// </summary>
        public void ReadRegisteredUsersFromDB()
        {
            List<UserModel> dbUserList = DataSync.Instance.GetUsers();

            if (dbUserList.Count == 0)
            {
                return;
            }

            if (this.RegisteredUsers == null)
            {
                this.RegisteredUsers = new ObservableSortedList<ContactGroup<UserModel>>();
            }

            var groups = new Dictionary<char, ContactGroup<UserModel>>();

            foreach (UserModel user in dbUserList)
            {
                if (!this.userIdUserMap.ContainsKey(user.Id))
                {
                    this.userIdUserMap.Add(user.Id, user);
                }

                if (this.Contains(user))
                {
                    continue;
                }

                if (user.UserType == UserType.Group)
                {
                    if (!user.Name.Contains("(G)"))
                    {
                        user.Name += " (G)";
                    }
                }

                this.userPhoneNumbers.Add(user.PhoneNumber);
                char firstLetter = char.ToLower(user.Name[0]);

                // show # for numbers
                if (firstLetter >= '0' && firstLetter <= '9')
                {
                    firstLetter = '#';
                }

                // create group for letter if it doesn't exist
                if (!groups.ContainsKey(firstLetter))
                {
                    var group = new ContactGroup<UserModel>(firstLetter);
                    this.RegisteredUsers.Add(group);
                    groups[firstLetter] = group;
                }

                // create a contact for item and add it to the relevant 
                groups[firstLetter].Add(user);
            }

            this.isLoading = false;
        }
    }
}