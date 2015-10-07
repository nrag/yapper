using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;

namespace YapperChat.Models
{
    [Table]
    public class GroupMemberModel
    {
        [Column(IsPrimaryKey = true)]
        public int GroupId
        {
            get;
            set;
        }

        [Column(IsPrimaryKey = true)]
        public int MemberId
        {
            get;
            set;
        }
    }
}
