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
using System.Collections.Generic;

namespace YapperChat.Models
{
    /// <summary>
    /// Event args passed to the callback
    /// </summary>
    public sealed class YapperContactsSearchEventArgs : EventArgs
    {
        // Summary:
        //     Gets the filter that was used for a search.
        //
        // Returns:
        //     The filter that was used for the search.
        public string Filter { get; set; }
        //
        // Summary:
        //     Gets the kind of filter that was used for the search.
        //
        // Returns:
        //     The kind of filter that was used for a search.
        public FilterKind FilterKind { get; set; }
        //
        // Summary:
        //     Gets the results of a search for contacts.
        //
        // Returns:
        //     The results of the search.
        public object Results { get; set; }
        //
        // Summary:
        //     Gets a user-defined object that contains information about the operation.
        //
        // Returns:
        //     The user-defined object.
        public object State { get; set; }
    }
}
