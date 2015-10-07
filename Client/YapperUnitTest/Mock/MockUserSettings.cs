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
using YapperChat.Models;

namespace YapperUnitTest.Mock
{
    public class MockUserSettings : IUserSettings
    {
        public int UserId
        {
            get;
            set;
        }

        public string MyPhoneNumber
        {
            get;
            set;
        }

        public string MyName
        {
            get;
            set;
        }

        public UserModel Me
        {
            get
            {
                return new UserModel() {Id = UserId, Name = MyName, PhoneNumber = MyPhoneNumber };
            }
        }

        public PushNotificationSubscriptionStatus PushNotificationSubscriptionStatus
        {
            get;
            set;
        }

        public string PushNotificationUrl
        {
            get;
            set;
        }

        public void Save(UserModel user)
        {
            this.MyName = user.Name;
            this.MyPhoneNumber = user.PhoneNumber;
            this.UserId = user.Id;
        }

        public void SavePushNotificationUrl(string url)
        {
            throw new NotImplementedException();
        }

        public void SavePushNotificationSubscriptionStatus()
        {
            throw new NotImplementedException();
        }


        public void Clear()
        {
            throw new NotImplementedException();
        }


        public bool ChatHeadEnabled
        {
            get;
            set;
        }


        public string Cookie
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}
