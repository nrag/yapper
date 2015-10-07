﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using YapperChat.Models;

namespace YapperChat.EventMessages
{
    public class MessageSentEvent
    {
        public bool Success
        {
            get;
            set;
        }

        public Guid ConversationId
        {
            get;
            set;
        }

        public UserModel Recipient
        {
            get;
            set;
        }
    }
}
