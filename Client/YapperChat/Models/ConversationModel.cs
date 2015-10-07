using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Runtime.Serialization;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using Microsoft.Phone.UserData;

namespace YapperChat.Models
{
    [DataContract(Namespace="http://schemas.datacontract.org/2004/07/Yapper")]
    public class ConversationModel: INotifyPropertyChanged
    {
        public ConversationModel()
        {
        }

        private double rotateAngle;
        public double RotateAngle
        {
            get
            {
                return this.rotateAngle;
            }

            set
            {
                this.rotateAngle = value;
                this.NotifyPropertyChanged("RotateAngle");
            }
        }
        private string otherParticipants;

        private bool isUnRead = true;

        private int _unreadCount = 0;

        /// <summary>
        /// Image to be displayed for the other participant
        /// </summary>
        private BitmapImage contactPhoto = null;

        [DataMember]
        public Guid ConversationId
        {
            get;
            set;
        }

        [DataMember]
        public ICollection<UserModel> ConversationParticipants
        {
            get;
            set;
        }

        [DataMember]
        public DateTime LastPostUtcTime
        {
            get;
            set;
        }

        [DataMember]
        public string LastPostPreview
        {
            get;
            set;
        }

        public string ConversationPreviewMessage
        {
            get
            {
                return this.LastPostPreview;
            }
        }

        public bool IsUnRead
        {
            get
            {
                return this.UnreadCount != 0;
            }
        }

        public int UnreadCount
        {
            get
            {
                return this._unreadCount;
            }

            set
            {
                this._unreadCount = value;
                this.NotifyPropertyChanged("UnreadCount");
                this.NotifyPropertyChanged("HasUnread");
            }
        }

        public bool HasUnread
        {
            get
            {
                return this._unreadCount != 0;
            }
        }

        public UserModel Recipient
        {
            get
            {
                // If one of them is a group, return the group
                foreach (UserModel participant in this.ConversationParticipants)
                {
                    if (participant != null && participant.UserType == UserType.Group)
                    {
                        return participant;
                    }
                }

                // otherwise return the user that's not me
                foreach (UserModel participant in this.ConversationParticipants)
                {
                    if (participant != null && participant.Id != UserSettingsModel.Instance.UserId)
                    {
                        return participant;
                    }
                }

                return null;
            }
        }

        public string OtherParticipants
        {
            get
            {
                if (!string.IsNullOrEmpty(this.otherParticipants))
                {
                    return this.otherParticipants;
                }

                StringBuilder builder = new StringBuilder();
                foreach (UserModel participant in this.ConversationParticipants)
                {
                    if (participant != null && participant.Id != UserSettingsModel.Instance.UserId &&
                        (!this.IsGroupConversation || participant.UserType == UserType.Group))
                    {
                        builder.Append(participant.Name);
                        builder.Append(',');
                    }
                }

                if (builder.Length == 0)
                {
                    this.otherParticipants = "Unknown";
                }
                else
                {
                    builder.Remove(builder.Length - 1, 1);
                    this.otherParticipants = builder.ToString();
                }

                return this.otherParticipants;
            }
        }

        public BitmapImage ContactPhoto
        {
            get
            {
                return this.Recipient.ContactPhoto;
            }
        }

        public string SimpleDateTime
        {
            get
            {
                return this.LastPostUtcTime.SimpleDate();
            }
        }

        public bool IsGroupConversation
        {
            get
            {
                foreach (UserModel um in this.ConversationParticipants)
                {
                    if (um.UserType == UserType.Group)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify that a property changed
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        internal static Guid BuildConversationIdFromParticipant(UserModel userModel, UserModel selectedUser)
        {
            throw new NotImplementedException();
        }
    }
}
