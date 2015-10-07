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
using System.Runtime.Serialization;

namespace YapperChat.Models
{
    [DataContract]
    public class UserCookieModel
    {
        public UserCookieModel(UserModel user, string authCookie)
        {
            this.User = user;
            this.AuthCookie = authCookie;
        }

        [DataMember]
        public UserModel User
        {
            get;
            set;
        }

        [DataMember]
        public string AuthCookie
        {
            get;
            set;
        }
    }
}