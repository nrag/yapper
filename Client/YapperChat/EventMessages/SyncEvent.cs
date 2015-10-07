using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YapperChat.Models;

namespace YapperChat.EventMessages
{
    enum SyncState
    {
        Start,

        Complete
    }

    class SyncEvent
    {
        public SyncState SyncState
        {
            get;
            set;
        }

        public List<MessageModel> Messages
        {
            get;
            set;

        }
    }
}
