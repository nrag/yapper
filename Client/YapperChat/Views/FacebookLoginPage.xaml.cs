using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;

namespace YapperChat.Views
{
    public partial class FacebookLoginPage : PhoneApplicationPage
    {
        public FacebookLoginPage()
        {
            InitializeComponent();
            LoadFacebookLoginPage();
        }

        private const string ExtendedPermissions = "user_about_me,read_stream,publish_stream,publish_checkins,friends_checkins,friends_status,friends_photos,user_status,user_checkins,user_photos";
        private void LoadFacebookLoginPage()
        {
            var url = "https://www.facebook.com/dialog/oauth?client_id=335442689934095&response_type=token&redirect_uri=https://www.facebook.com/connect/login_success.html&scope="+ExtendedPermissions;
            WebBrowserControl.Navigate(new Uri(url));
        }

        private void WebBrowserControl_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            if (string.IsNullOrEmpty(AccessToken))
            {
                string FragmentString = e.Uri.Fragment;
                if (!string.IsNullOrEmpty(FragmentString))
                {
                    int start = FragmentString.IndexOf('=');
                    AccessToken = FragmentString.Substring(start + 1, FragmentString.Length - start - 1);
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (!string.IsNullOrEmpty(AccessToken))
                        {
                            LoadUserProfile();
                        }
                    });
                }
            }
        }

        string AccessToken;

        void LoadUserProfile()
        {
            var urlProfile = "https://graph.facebook.com/me?fields=name,picture&access_token=" + AccessToken;
            WebRequest request = WebRequest.Create(urlProfile);
            request.BeginGetResponse(new AsyncCallback(this.ResponseCallbackProfile), request);
        }

        private void ResponseCallbackProfile(IAsyncResult asynchronousResult)
        {
            try
            {
                var request = (HttpWebRequest)asynchronousResult.AsyncState;
                using (var resp = (HttpWebResponse)request.EndGetResponse(asynchronousResult))
                {
                    using (var streamResponse = resp.GetResponseStream())
                    {
                        var facebookSerializerData = new DataContractJsonSerializer(typeof(FacebookUserProfile));
                        var facebookProfileData = facebookSerializerData.ReadObject(streamResponse) as FacebookUserProfile;
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            NavigationService.GoBack();
                        });
                    }
                }
            }
            catch (WebException ex)
            {
            }
        }

        [DataContract]
        public class FacebookUserProfile
        {
            [DataMember(Name = "name")]
            public string Name { get; set; }

            [DataMember(Name = "picture")]
            public string Picture { get; set; }
        }
    }
}