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
using System.IO.IsolatedStorage;
using System.Collections.Generic;

namespace YapperChat.Models
{
    /// <summary>
    /// PushNotificationSubscriptionStatus
    /// </summary>
    public enum PushNotificationSubscriptionStatus
    {
        /// <summary>
        /// The user has disabled push notifications
        /// </summary>
        Disabled,

        /// <summary>
        /// The user has enabled push notifications and has subscribed to yapper service
        /// </summary>
        EnabledSubscribed,

        /// <summary>
        /// The user has enabled push notifications but has not yet subscribed to yapper service
        /// </summary>
        EnabledNotSubscribed,
    }

    /// <summary>
    /// UserSettingsModel
    /// </summary>
    public class UserSettingsModel : IUserSettings
    {
        private static UserSettingsModel singleton;

        private static string UserIdKey = "UserId";

        private static string PhoneNumberKey = "PhoneNumber";

        private static string PublicKeyKey = "PublicKey";

        private static string PushNotificationUrlKey = "PushNotificationUrl";

        private static string PushNotificationSubscriptionStatusKey = "PushNotificationSubscriptionStatus";

        private static string SendingLocationEnabledKey = "SendingLocationEnabled";

        private static string HideCompletedTasksEnabledKey = "HideCompletedTasksEnabled";

        private static string DebugEnabledKey = "DebugEnabled";

        private static string TutorialSeenKey = "TutorialSeen";

        private static string LastTaskPageViewTimeKey = "LastTaskPageViewTime";

        private static string LastMessagePageViewTimeKey = "LastMessagePageViewTime";

        private static string DeviceUniqueIdKey = "DeviceUniqueId";

        private static string CookieKey = "Cookie";

        private static string NameKey = "Name";

        private static string ExceptionKey = "Exceptions";

        private static string LastSyncDateTimeKey = "LastSyncDateTime";

        private static string PrivateKeyKey = "PrivateKey";

        private int userId;

        private string phoneNumber;

        private string name;

        /// <summary>
        /// Creates an instance of UsersettingsModel
        /// </summary>
        public UserSettingsModel()
        {
            if (!IsolatedStorageSettings.ApplicationSettings.Contains(UserSettingsModel.UserIdKey) ||
                !IsolatedStorageSettings.ApplicationSettings.Contains(UserSettingsModel.NameKey) ||
                !IsolatedStorageSettings.ApplicationSettings.Contains(UserSettingsModel.PhoneNumberKey))
            {
                this.userId = -1;
                this.name = null;
                this.phoneNumber = null;
            }
            else
            {
                this.userId = (int)IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.UserIdKey];
                this.name = (string)IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.NameKey];
                this.phoneNumber = (string)IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.PhoneNumberKey];
            }
        }

        /// <summary>
        /// Singleton instance for usersettings models
        /// </summary>
        public static IUserSettings Instance
        {
            get
            {
                if (UserSettingsModel.singleton == null)
                {
                    UserSettingsModel.singleton = new UserSettingsModel();
                }

                return UserSettingsModel.singleton;
            }
        }

        public UserModel Me
        {
            get
            {
                return new UserModel() { Id = this.UserId, Name = MyName, PhoneNumber = MyPhoneNumber, PublicKey = this.PublicKey };
            }
        }

        public int UserId
        {
            get
            {
                return this.userId;
            }
        }

        public string MyPhoneNumber
        {
            get
            {
                return phoneNumber;
            }
        }

        public string MyName
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        /// Getter and setter for the new pushsubscriptionstatus.
        /// The setter initiates the Subcribe Or Unsubscribe call to push notification depending on
        /// the status change
        /// </summary>
        public PushNotificationSubscriptionStatus PushNotificationSubscriptionStatus
        {
            get
            {
                if (IsolatedStorageSettings.ApplicationSettings.Contains(UserSettingsModel.PushNotificationSubscriptionStatusKey))
                {
                    return (PushNotificationSubscriptionStatus)IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.PushNotificationSubscriptionStatusKey];
                }

                return PushNotificationSubscriptionStatus.EnabledNotSubscribed;
            }

            set
            {
                this.SavePushNotificationSubscriptionStatus(value);    
            }
        }

        /// <summary>
        /// The saved push notification url
        /// </summary>
        public string PushNotificationUrl
        {
            get
            {
                if (IsolatedStorageSettings.ApplicationSettings.Contains(UserSettingsModel.PushNotificationUrlKey))
                {
                    return (string)IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.PushNotificationUrlKey];
                }

                return null;
            }

            set
            {
                this.SavePushNotificationUrl(value);
            }
        }

        public DateTime LastTaskPageViewTime
        {
            get
            {
                if (IsolatedStorageSettings.ApplicationSettings.Contains(UserSettingsModel.LastTaskPageViewTimeKey))
                {
                    return new DateTime((long)IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.LastTaskPageViewTimeKey], DateTimeKind.Local);
                }

                return new DateTime(1970, 1, 1);
            }

            set
            {
                IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.LastTaskPageViewTimeKey] = value.Ticks;
                lock (this)
                {
                    IsolatedStorageSettings.ApplicationSettings.Save();
                }
            }
        }

        public DateTime LastMessagePageViewTime
        {
            get
            {
                if (IsolatedStorageSettings.ApplicationSettings.Contains(UserSettingsModel.LastMessagePageViewTimeKey))
                {
                    return new DateTime((long)IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.LastMessagePageViewTimeKey], DateTimeKind.Local);
                }

                return new DateTime(1970, 1, 1);
            }

            set
            {
                IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.LastMessagePageViewTimeKey] = value.Ticks;
                lock (this)
                {
                    IsolatedStorageSettings.ApplicationSettings.Save();
                }
            }
        }

        public string DeviceUniqueId
        {
            get
            {
                if (IsolatedStorageSettings.ApplicationSettings.Contains(UserSettingsModel.DeviceUniqueIdKey))
                {
                    return (string)IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.DeviceUniqueIdKey];
                }

                return null;
            }

            set
            {
                IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.DeviceUniqueIdKey] = value;
                lock (this)
                {
                    IsolatedStorageSettings.ApplicationSettings.Save();
                }
            }
        }

        public bool SendingLocationEnabled
        {
            get
            {
                if (IsolatedStorageSettings.ApplicationSettings.Contains(UserSettingsModel.SendingLocationEnabledKey))
                {
                    return (bool)IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.SendingLocationEnabledKey];
                }

                return true;
            }

            set
            {
                IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.SendingLocationEnabledKey] = value;
                lock (this)
                {
                    IsolatedStorageSettings.ApplicationSettings.Save();
                }
            }
        }

        public bool HideCompletedTasksEnabled
        {
            get
            {
                if (IsolatedStorageSettings.ApplicationSettings.Contains(UserSettingsModel.HideCompletedTasksEnabledKey))
                {
                    return (bool)IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.HideCompletedTasksEnabledKey];
                }

                return false;
            }

            set
            {
                IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.HideCompletedTasksEnabledKey] = value;
                lock (this)
                {
                    IsolatedStorageSettings.ApplicationSettings.Save();
                }
            }
        }

        public bool DebugEnabled
        {
            get
            {
                if (IsolatedStorageSettings.ApplicationSettings.Contains(UserSettingsModel.DebugEnabledKey))
                {
                    return (bool)IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.DebugEnabledKey];
                }

                return false;
            }

            set
            {
                IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.DebugEnabledKey] = value;
                lock (this)
                {
                    IsolatedStorageSettings.ApplicationSettings.Save();
                }
            }
        }

        public bool TutorialSeen
        {
            get
            {
                if (IsolatedStorageSettings.ApplicationSettings.Contains(UserSettingsModel.TutorialSeenKey))
                {
                    return (bool)IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.TutorialSeenKey];
                }

                return false;
            }

            set
            {
                IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.TutorialSeenKey] = value;
                lock (this)
                {
                    IsolatedStorageSettings.ApplicationSettings.Save();
                }
            }
        }
        public string Cookie
        {
            get
            {
                if (IsolatedStorageSettings.ApplicationSettings.Contains(UserSettingsModel.CookieKey))
                {
                    return (string)IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.CookieKey];
                }

                return null;
            }

            set
            {
                IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.CookieKey] = value;
                lock (this)
                {
                    IsolatedStorageSettings.ApplicationSettings.Save();
                }
            }
        }

        public List<ExceptionDetails> Exceptions
        {
            get
            {
                if (IsolatedStorageSettings.ApplicationSettings.Contains(UserSettingsModel.ExceptionKey))
                {
                    return (List<ExceptionDetails>)IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.ExceptionKey];
                }

                return null;
            }

            set
            {
                IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.ExceptionKey] = value;
                lock (this)
                {
                    IsolatedStorageSettings.ApplicationSettings.Save();
                }
            }
        }

        public DateTime LastSyncDateTime
        {
            get
            {
                if (IsolatedStorageSettings.ApplicationSettings.Contains(UserSettingsModel.LastSyncDateTimeKey))
                {
                    return (DateTime)IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.LastSyncDateTimeKey];
                }

                return new DateTime(1970, 1, 1);
            }

            set
            {
                IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.LastSyncDateTimeKey] = value;
                lock (this)
                {
                    IsolatedStorageSettings.ApplicationSettings.Save();
                }
            }
        }

        public byte[] PublicKey
        {
            get
            {
                if (IsolatedStorageSettings.ApplicationSettings.Contains(UserSettingsModel.PublicKeyKey))
                {
                    return (byte[])IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.PublicKeyKey];
                }

                return null;
            }

            set
            {
                IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.PublicKeyKey] = value;
                lock (this)
                {
                    IsolatedStorageSettings.ApplicationSettings.Save();
                }
            }
        }

        public byte[] PrivateKey
        {
            get
            {
                if (IsolatedStorageSettings.ApplicationSettings.Contains(UserSettingsModel.PrivateKeyKey))
                {
                    return (byte[])IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.PrivateKeyKey];
                }

                return null;
            }

            set
            {
                IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.PrivateKeyKey] = value;
                lock (this)
                {
                    IsolatedStorageSettings.ApplicationSettings.Save();
                }
            }
        }

        public bool IsAuthenticated()
        {
            if (UserSettingsModel.Instance.UserId == -1)
            {
                return false;
            }
            else if (string.IsNullOrEmpty(UserSettingsModel.Instance.Cookie))
            {
                return false;
            }

            return true;
        }

        public void SaveException(string ex)
        {
            List<ExceptionDetails> exceptions = null;
            if (IsolatedStorageSettings.ApplicationSettings.Contains(UserSettingsModel.ExceptionKey))
            {
                exceptions = (List<ExceptionDetails>)IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.ExceptionKey];
            }

            if (exceptions == null)
            {
                exceptions = new List<ExceptionDetails>();
            }
         
            ExceptionDetails exception = new ExceptionDetails();
            exception.ExceptionDate = DateTime.UtcNow;
            exception.ExceptionString = ex;
            exception.UserId = this.UserId;
            exceptions.Add(exception);

            IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.ExceptionKey] = exceptions;
            lock (this)
            {
                IsolatedStorageSettings.ApplicationSettings.Save();
            }
        }

        /// <summary>
        /// Save the user to isolated storage
        /// </summary>
        /// <param name="user"></param>
        public void Save(UserModel user)
        {
            IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.NameKey] = user.Name;
            IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.PhoneNumberKey] = user.PhoneNumber;
            IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.UserIdKey] = user.Id;
            IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.PublicKeyKey] = user.PublicKey;

            IsolatedStorageSettings.ApplicationSettings.Save();

            this.userId = (int)IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.UserIdKey];
            this.name = (string)IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.NameKey];
            this.phoneNumber = (string)IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.PhoneNumberKey];
        }

        public void Clear()
        {
            lock (this)
            {
                IsolatedStorageSettings.ApplicationSettings.Clear();
                IsolatedStorageSettings.ApplicationSettings.Save();
            }
        }

        /// <summary>
        /// Saves the pushnotification url to isolated storage
        /// </summary>
        /// <param name="url"></param>
        private void SavePushNotificationUrl(string url)
        {
            IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.PushNotificationUrlKey] = url;
            lock (this)
            {
                IsolatedStorageSettings.ApplicationSettings.Save();
            }
        }

        /// <summary>
        /// Saves 
        /// </summary>
        /// <param name="status"></param>
        private void SavePushNotificationSubscriptionStatus(PushNotificationSubscriptionStatus status)
        {
            IsolatedStorageSettings.ApplicationSettings[UserSettingsModel.PushNotificationSubscriptionStatusKey] = status;
            lock (this)
            {
                IsolatedStorageSettings.ApplicationSettings.Save();
            }
        }
    }
}
