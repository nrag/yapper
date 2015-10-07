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
using YapperChat.Models;
using YapperChat.PushNotification;

namespace YapperChat.ViewModels
{
    public class UserSettingsViewModel :INotifyPropertyChanged
    {
        /// <summary>
        /// If true the view model is saving settings
        /// </summary>
        private bool _isSavingSettings;

        private IUserSettings userSettings;

        private IPushNotification pushNotification;

        public UserSettingsViewModel()
            : this(UserSettingsModel.Instance, PushNotification.PushNotification.Instance)
        {
        }

        public UserSettingsViewModel(IUserSettings userSettings, IPushNotification pushNotification)
        {
            this.userSettings = userSettings;
            this.pushNotification = pushNotification;
        }

        public bool IsSavingSettings
        {
            get
            {
                return this._isSavingSettings;
            }

            private set
            {
                this._isSavingSettings = value;
                this.NotifyPropertyChanged("IsSavingSettings");
            }
        }

        public bool PushNotificationEnabled
        {
            get
            {
                if (this.userSettings.PushNotificationSubscriptionStatus == PushNotificationSubscriptionStatus.Disabled)
                {
                    return false;
                }

                return true;
            }
        }

        public bool HideCompletedTasksEnabled
        {
            get
            {
                return this.userSettings.HideCompletedTasksEnabled;
            }
        }

        public bool LocationEnabled
        {
            get
            {
                return this.userSettings.SendingLocationEnabled;
            }
        }

        public bool DebugEnabled
        {
            get
            {
                return this.userSettings.DebugEnabled;
            }
        }
        public void ChangePushNotificationSettings(bool pushNotificationState)
        {
            this.IsSavingSettings = true;

            try
            {
                // The notification state has not changed
                if ((this.userSettings.PushNotificationSubscriptionStatus == PushNotificationSubscriptionStatus.Disabled && pushNotificationState == false) ||
                    this.userSettings.PushNotificationSubscriptionStatus != PushNotificationSubscriptionStatus.Disabled && pushNotificationState == true)
                {
                    return;
                }

                if (pushNotificationState)
                {
                    this.pushNotification.Subscribe();
                }
                else
                {
                    this.pushNotification.UnSubscribe();
                }
            }
            finally
            {
                this.IsSavingSettings = false;
            }
        }

        public void ChangeLocationSettings(bool isChecked)
        {
            this.IsSavingSettings = true;

            this.userSettings.SendingLocationEnabled = isChecked;

            this.IsSavingSettings = false;
        }

        public void ChangeHideCompletedTasksSettings(bool isChecked)
        {
            this.IsSavingSettings = true;

            this.userSettings.HideCompletedTasksEnabled = isChecked;

            this.IsSavingSettings = false;
        }

        public void ChangeDebugSettings(bool isChecked)
        {
            this.IsSavingSettings = true;

            this.userSettings.DebugEnabled = isChecked;

            this.IsSavingSettings = false;
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
