using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using YapperChat.ViewModels;
using Microsoft.Phone.Tasks;
using YapperChat.Models;

namespace YapperChat.Views
{
    public partial class PhoneContactsJumpListView : PhoneApplicationPage
    {
        public PhoneContactsJumpListView()
        {
            InitializeComponent();
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            ((PhoneContactsJumpListViewModel)this.DataContext).Search();
        }

        private void ContactPivot_SelectionChanged(object sender, RoutedEventArgs e)
        {
        }
    }
}