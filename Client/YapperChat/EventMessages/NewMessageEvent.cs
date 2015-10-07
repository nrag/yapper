﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YapperChat.Models;

namespace YapperChat.EventMessages
{
    class NewMessageEvent
    {
        public MessageModel Message
        {
            get;
            set;
        }

        public bool IsPush
        {
            get;
            set;
        }
    }
}
