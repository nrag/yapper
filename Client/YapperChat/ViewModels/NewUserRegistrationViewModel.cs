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
using System.ComponentModel;
using YapperChat.Common;
using YapperChat.ServiceProxy;
using YapperChat.Models;
using GalaSoft.MvvmLight.Messaging;
using YapperChat.EventMessages;
using YapperChat.Sync;

namespace YapperChat.ViewModels
{
    /// <summary>
    /// The view model for new user's registration
    /// </summary>
    public class NewUserRegistrationViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Boolean value to indicate if registration is in progress
        /// </summary>
        private bool _isRegistering = false;

        private bool _phoneNumberEntered = false;

        private IServiceProxy serviceProxy;

        private ObservableSortedList<ContactGroup<UserModel>> _registeredUsers;

        private bool registeredUsersLoaded = false;

        RegisteredUsersViewModel trial;

        /// <summary>
        /// Creates an instance of NewUserRegistrationViewModel
        /// </summary>
        public NewUserRegistrationViewModel() : this(YapperServiceProxy.Instance)
        {
                DataSync.Instance.SyncUsers();
                trial = new RegisteredUsersViewModel();
                Messenger.Default.Register<NewContactEvent>(this, this.ReadRegisteredUsersFromDB);
        }

        private void ReadRegisteredUsersFromDB(NewContactEvent sync)
        {
            DispatcherHelper.InvokeOnUiThread(() =>
            {
                trial.ReadRegisteredUsersFromDB();
                RegisteredUsers = trial.RegisteredUsers;
                registeredUsersLoaded = true;
                this.NotifyPropertyChanged("PreInstallText");
                this.NotifyPropertyChanged("RegisteredUsersLoaded");
                this.NotifyPropertyChanged("RegisteredUsersNotLoaded");
            });

            Messenger.Default.Unregister<NewContactEvent>(this);
        }

        /// <summary>
        /// Creates an instance of NewUserRegistrationViewModel
        /// </summary>
        /// <param name="serviceProxy"></param>
        public NewUserRegistrationViewModel(IServiceProxy serviceProxy)
        {
            this.serviceProxy = serviceProxy;
        }

        public bool RegisteredUsersNotLoaded
        {
            get
            {
                return !registeredUsersLoaded;
            }
        }

        public bool RegisteredUsersLoaded
        {
            get
            {
                return registeredUsersLoaded;
            }
        }

        /// <summary>
        /// If true, Name and phone number was entered in the registration page
        /// </summary>
        public bool PhoneNumberEntered
        {
            get
            {
                return this._phoneNumberEntered;
            }

            set
            {
                this._phoneNumberEntered = value;
                this.NotifyPropertyChanged("PhoneNumberEntered");
            }
        }

        public ObservableSortedList<ContactGroup<UserModel>> RegisteredUsers
        {
            get
            {
                return this._registeredUsers;
            }

            set
            {
                this._registeredUsers = value;
                this.NotifyPropertyChanged("RegisteredUsers");
            }
        }

        public string PreInstallText
        {
            get
            {
                if (RegisteredUsersLoaded == false)
                {
                    return String.Empty;
                }

                if (RegisteredUsers != null && RegisteredUsers.Count > 0)
                {
                    return YapperChat.Resources.Strings.PreInstallText;
                }
                else
                {
                    return YapperChat.Resources.Strings.PreInstallNoUserText;
                }
            }
        }

        /// <summary>
        /// Getter for isregistering
        /// </summary>
        public bool IsRegistering
        {
            get
            {
                return this._isRegistering;
            }

            private set
            {
                this._isRegistering = value;
                this.NotifyPropertyChanged("IsRegistering");
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

        internal void Register(string phoneNumber, string name)
        {
            this.IsRegistering = true;
            UserSettingsModel.Instance.Save(new UserModel() { PhoneNumber = phoneNumber, Name = name });
            YapperServiceProxy.Instance.RegisterNewUser(phoneNumber, name, this.RegistrationCompleted);
        }

        private void RegistrationCompleted(UserCookieModel userCookie)
        {
            if (userCookie != null && userCookie.User != null)
            {
                byte[] publicKey = null, privateKey = null;

                if (publicKey == null)
                {
                    RsaEncryption.GenerateKeys(out publicKey, out privateKey);
                }

                UserSettingsModel.Instance.PrivateKey = privateKey;
                userCookie.User.PublicKey = publicKey;
                UserSettingsModel.Instance.Save(userCookie.User);
                UserSettingsModel.Instance.Cookie = userCookie.AuthCookie;

                YapperServiceProxy.Instance.UpdateUserPublicKey(
                    userCookie.User,
                    delegate(bool success)
                    {
                        if (!success)
                        {
                            // clear the private key if public key update fails
                            UserSettingsModel.Instance.PrivateKey = null;
                        }
                    }
                    );
            }
            else
            {
                this.IsRegistering = false;
            }

            Messenger.Default.Send<RegistrationCompleteEvent>(new RegistrationCompleteEvent() { User = userCookie == null ? null : userCookie.User });
        }
    }
}
