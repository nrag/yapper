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
    public class VerificationCodeValidationCompleteEvent
    {
        public VerificationCodeValidationCompleteEvent(bool success)
        {
            this.Success = success;
        }

        public bool Success
        {
            get;
            set;
        }
    }
}
