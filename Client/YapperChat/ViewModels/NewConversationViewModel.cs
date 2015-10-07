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
using System.ComponentModel;
using System.Runtime.Serialization.Json;
using YapperChat.Common;
using System.IO;
using System.Windows.Navigation;
using YapperChat.ServiceProxy;
using YapperChat.EventMessages;
using GalaSoft.MvvmLight.Messaging;
using YapperChat.Sync;

namespace YapperChat.ViewModels
{
    public class NewConversationViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Instance of the service proxy
        /// </summary>
        private IServiceProxy serviceProxy;

        /// <summary>
        /// User Settings
        /// </summary>
        private IUserSettings userSettings;

        /// <summary>
        /// If true, a webservice all is being made
        /// </summary>
        private bool isSendingField = false;

        /// <summary>
        /// 
        /// </summary>
        private ContactDetailsViewModel contactDetails = new ContactDetailsViewModel();

        /// <summary>
        /// Creates an instance of NewConversationViewModel
        /// </summary>
        public NewConversationViewModel() : this(YapperServiceProxy.Instance, UserSettingsModel.Instance)
        {
        }

        /// <summary>
        /// Creates an instance of NewConversationViewModel
        /// </summary>
        /// <param name="serviceProxy"></param>
        public NewConversationViewModel(IServiceProxy serviceProxy, IUserSettings userSettings)
        {
            this.serviceProxy = serviceProxy;
            this.userSettings = userSettings;
            Messenger.Default.Register<NewMessageEvent>(this, this.HandleNewMessageSent);
        }

        /// <summary>
        /// UserModel corresponding to the recipient
        /// </summary>
        public UserModel Recipient
        {
            get;
            set;
        }

        public string Participants
        {
            get
            {
                if (this.Recipient != null)
                {
                    return this.Recipient.Name;
                }

                return null;
            }
        }

        public bool EncryptionRequested
        {
            get;
            set;
        }

        public bool IsSending
        {
            get
            {
                return this.isSendingField;
            }

            private set
            {
                this.isSendingField = value;
                this.NotifyPropertyChanged("IsSending");
            }
        }

        public ContactDetailsViewModel ContactDetail
        {
            get
            {
                return this.contactDetails;
            }
        }

        internal void SendNewConversation(string message)
        {
            this.IsSending = true;

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

        private void HandleNewMessageSent(NewMessageEvent newMessage)
        {
            if (newMessage.Message == null || newMessage.Message.Recipient.Id != this.Recipient.Id)
            {
                return;
            }

            if (!newMessage.IsPush)
            {
                DispatcherHelper.InvokeOnUiThread(() =>
                    {
                        this.IsSending = false;
                    });

                Messenger.Default.Send<MessageSentEvent>(
                    new MessageSentEvent() { 
                        Success = newMessage.Message != null,
                        ConversationId = newMessage.Message != null ? newMessage.Message.ConversationId : Guid.Empty,
                        Recipient = this.Recipient
                    });
            }
        }

        internal void LoadContactDetails()
        {
            this.contactDetails.YapperName = this.Recipient.Name;
            this.contactDetails.YapperPhone = this.Recipient.PhoneNumber;
            this.contactDetails.UserId = this.Recipient.Id;
            this.contactDetails.Search();
        }
    }
}
