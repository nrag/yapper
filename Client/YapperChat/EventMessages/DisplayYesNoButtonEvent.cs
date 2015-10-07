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

namespace YapperChat.EventMessages
{
    public class DisplayYesNoButtonEvent
    {
        public long ConversationId
        {
            get;
            set;
        }

        public bool ShouldDisplay
        {
            get;
            set;
        }

        public string YesQuestion
        {
            get;
            set;
        }

        public string NoQuestion
        {
            get;
            set;
        }

        public string PassQuestion
        {
            get;
            set;
        }
    }
}
