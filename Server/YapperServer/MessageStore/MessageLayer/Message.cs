using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace MessageStore.MessageLayer
{
    public partial class Message
    {
        private User sender;

        private User recipient;

        [DataMember]
        public User Sender
        {
            get
            {
                if (this.sender == null)
                {
                    this.sender = UserService.Instance.GetUserFromId(this.SenderId);
                }

                return this.sender;
            }

            set
            {
                this.SenderId = value.Id;
                this.sender = value;
            }
        }

        [DataMember]
        public User Recipient
        {
            get
            {
                if (this.recipient == null)
                {
                    this.recipient = UserService.Instance.GetUserFromId(this.RecipientId);
                }

                return this.recipient;
            }

            set
            {
                if (value != null)
                {
                    this.RecipientId = value.Id;
                    this.recipient = value;
                }
            }
        }

        public bool IsEncrypted
        {
            get
            {
                return ((this.MessageFlags & MessageFlags.EncryptedMessage) == MessageFlags.EncryptedMessage);
            }
        }

        private byte[] GetMessageBlobValue()
        {
            return this.SerializeToProtocolBuffer();
        }

        private void ParseMessageBlobValue(byte[] value)
        {
            throw new NotImplementedException();
        }
    }
}
