namespace DataAccessLayer
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;

    [KnownType(typeof(Group))]
    [DataContract(Name = "User", Namespace = "http://schemas.datacontract.org/2004/07/Yapper")]
    public class User
    {
        public User()
        {
        }

        public User(int userId, string phoneNumber, string name, string secret) :
            this(userId, phoneNumber, name, secret, DateTime.UtcNow)
        {
        }

        public User(int userId, string phoneNumber, string name, string secret, DateTime lastsynctime)
            : this(userId, phoneNumber, name, secret, lastsynctime, null, null, 0)
        {
        }

        public User(int userId, string phoneNumber, string name, string secret, DateTime lastsynctime, byte[] publicKey, string device, long registrationDate)
        {
            this.Id = userId;
            this.Secret = secret;
            this.PhoneNumber = phoneNumber;
            this.Name = name;
            this.LastSyncTime = lastsynctime;
            this.SubscriptionUrls = Subscription.GetSubscriptionsForUser(this.Id, device);
            this.GroupIds = Group.GetGroupIdsForUser(this.Id);
            this.PublicKey = publicKey;
            this.RegisteredDevice = device;
            this.RegistrationDate = registrationDate;
        }

        [DataMember]
        public int Id
        {
            get;
            set;
        }

        [DataMember]
        public string PhoneNumber
        {
            get;
            set;
        }

        [DataMember]
        public string Name
        {
            get;
            set;
        }

        public string Secret
        {
            get;
            set;
        }

        public string RegisteredDevice
        {
            get;
            set;
        }

        public long RegistrationDate
        {
            get;
            set;
        }

        public DateTime LastSyncTime
        {
            get;
            set;
        }

        [DataMember]
        public byte[] PublicKey
        {
            get;
            set;
        }

        public List<string> SubscriptionUrls
        {
            get;
            set;
        }

        public List<int> GroupIds
        {
            get;
            set;
        }

        [DataMember]
        public UserType UserType
        {
            get
            {
                if (this is Group)
                {
                    return UserType.Group;
                }

                return UserType.User;
            }

            set
            { 
            }
        }

        public override bool Equals(object obj)
        {
            User other = obj as User;

            if (obj == null)
            {
                return false;
            }

            return this.Id == other.Id;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        internal virtual bool CanSendMessage(User sender)
        {
            return true;
        }
    }
}
