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
using Microsoft.Phone.UserData;

namespace YapperChat.Models
{
    public enum SearchKind
    {
        AllPhoneNumbers,

        Picture
    }

    /// <summary>
    /// Arguments to be passed to contacts search
    /// </summary>
    public class ContactSearchArguments
    {
        public ContactSearchArguments(string filter, SearchKind searchKind, FilterKind filterKind, object state)
        {
            this.Filter = filter;
            this.FilterKind = filterKind;
            this.SearchKind = searchKind;
            this.State = state;
        }

        public SearchKind SearchKind
        {
            get;
            set;
        }

        public string Filter
        {
            get;
            private set;
        }

        public FilterKind FilterKind
        {
            get;
            private set;
        }

        public object State
        {
            get;
            private set;
        }

        public EventHandler<YapperContactsSearchEventArgs> SearchCompleted
        {
            get;
            set;
        }
    }

}
