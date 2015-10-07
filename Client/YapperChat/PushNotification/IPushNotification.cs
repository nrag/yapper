using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YapperChat.PushNotification
{
    public interface IPushNotification
    {
        void Setup();

        void Subscribe();

        void UnSubscribe();
    }
}
