using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YapperChat.Models;

namespace YapperChat.EventMessages
{
    class GroupMemberAddedEvent
    {
        public bool Success
        {
            get;
            set;
        }

        public UserModel User
        {
            get;
            set;
        }
    }
}
