using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Runtime.Serialization;
using YapperChat.Models;
using System.Collections.Generic;

namespace YapperUnitTest
{
    [DataContract()]
    public class OwnerAndConversations
    {
        [DataMember]
        public UserModel Owner
        {
            get;
            set;
        }

        [DataMember]
        public List<ConversationModel> Conversations
        {
            get;
            set;
        }
    }
}
