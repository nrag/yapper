using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YapperChat.Models;

namespace YapperChat.EventMessages
{
    class ScrollToEvent
    {
        public MessageModel Message
        {
            get;
            set;
        }
    }
}
