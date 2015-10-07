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
using YapperChat.ServiceProxy;
using YapperChat.Models;
using GalaSoft.MvvmLight.Messaging;
using YapperChat.EventMessages;
using YapperChat.Common;

namespace YapperChat.ViewModels
{
    public class EnterConfirmationCodeViewModel : INotifyPropertyChanged
    {
        private bool _isValidating = false;

        public bool IsValidating
        {
            get
            {
                return this._isValidating;
            }

            private set
            {
                this._isValidating = value;
                this.NotifyPropertyChanged("IsValidating");
            }
        }

        public void Validate(string code)
        {
            this.IsValidating = true;
            YapperServiceProxy.Instance.ValidateConfirmationCode(code, this.ValidationComplete);
        }

        private void ValidationComplete(UserCookieModel cookie)
        {
            if (cookie != null)
            {
                UserSettingsModel.Instance.Cookie = cookie.AuthCookie;
                byte[] publicKey = null, privateKey = null;

                if (publicKey == null)
                {
                    RsaEncryption.GenerateKeys(out publicKey, out privateKey);
                }

                UserSettingsModel.Instance.PrivateKey = privateKey;
                cookie.User.PublicKey = publicKey;
                UserSettingsModel.Instance.Save(cookie.User);

                YapperServiceProxy.Instance.UpdateUserPublicKey(
                    cookie.User,
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

            Messenger.Default.Send<VerificationCodeValidationCompleteEvent>(new VerificationCodeValidationCompleteEvent(cookie != null));
            this.IsValidating = false;
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
