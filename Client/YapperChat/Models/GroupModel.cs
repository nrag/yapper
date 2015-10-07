using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using YapperChat.Sync;

namespace YapperChat.Models
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract(Name = "Group", Namespace = "http://schemas.datacontract.org/2004/07/Yapper")]
    public class GroupModel : UserModel
    {
        private int ownerId = 0;
        private UserModel owner = null;

        [DataMember]
        public List<UserModel> Members
        {
            get;
            set;
        }

        [DataMember]
        public UserModel Owner
        {
            get
            {
                if (this.owner == null && this.ownerId != 0)
                {
                    return DataSync.Instance.GetUser(this.ownerId);
                }
                
                return this.owner;
            }

            set
            {
                this.owner = value;
            }
        }

        [Column(CanBeNull = true)]
        public int OwnerId
        {
            get
            {
                if (this.ownerId == 0 && this.Owner != null)
                {
                    return this.Owner.Id;
                }

                return this.ownerId;
            }

            set
            {
                this.ownerId = value;
            }
        }

        public int MemberCount
        {
            get
            {
                if (this.Members == null)
                {
                    return 0;
                }

                return this.Members.Count;
            }
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
    }
}
