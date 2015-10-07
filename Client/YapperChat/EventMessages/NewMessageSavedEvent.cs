using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YapperChat.Models;

namespace YapperChat.EventMessages
{
    class NewMessageSavedEvent
    {
        /// <summary>
        /// The message that's saved.
        /// </summary>
        public MessageModel Message
        {
            get;
            set;
        }
    }
}
