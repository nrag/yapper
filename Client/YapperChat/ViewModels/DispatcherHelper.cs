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

namespace YapperChat.ViewModels
{
    public class DispatcherHelper
    {
        public static bool TestHook
        {
            get;
            set;
        }

        public static void InvokeOnUiThread(Action action, bool background = false)
        {
            if (!background && Deployment.Current.Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                Deployment.Current.Dispatcher.BeginInvoke(action);
            }
        }
    }
}
