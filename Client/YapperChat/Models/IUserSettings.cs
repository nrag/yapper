using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YapperChat.Models
{
    public interface IUserSettings
    {
        UserModel Me
        {
            get;
        }

        int UserId
        {
            get;
        }

        string MyPhoneNumber
        {
            get;
        }

        string MyName
        {
            get;
        }

        PushNotificationSubscriptionStatus PushNotificationSubscriptionStatus
        {
            get;
            set;
        }

        string PushNotificationUrl
        {
            get;
            set;
        }

        DateTime LastTaskPageViewTime 
        { 
            get; 
            set; 
        }

        bool SendingLocationEnabled
        {
            get;
            set;
        }

        bool HideCompletedTasksEnabled
        {
            get;
            set;
        }

        bool DebugEnabled
        {
            get;
            set;
        }

        bool TutorialSeen
        {
            get;
            set;
        }

        string Cookie 
        { 
            get; 
            set; 
        }

        List<ExceptionDetails> Exceptions
        {
            get;
            set;
        }

        DateTime LastSyncDateTime
        {
            get;
            set;
        }

        DateTime LastMessagePageViewTime
        {
            get;
            set;
        }

        string DeviceUniqueId
        {
            get;
            set;
        }

        byte[] PrivateKey
        {
            get;
            set;
        }

        bool IsAuthenticated();

        void SaveException(string ex);
        
        void Save(UserModel user);

        void Clear();
    }
}
