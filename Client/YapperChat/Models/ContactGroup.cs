using System.Collections.ObjectModel;
using Microsoft.Phone.UserData;
using System;
using YapperChat.Common;
using System.ComponentModel;

namespace YapperChat.Models
{
    /// <summary>
    /// Represents a page for doing the actual contact choosing
    /// </summary>
    public interface IContactChooserPage
    {
        /// <summary>
        /// Gets or sets the contact value.
        /// </summary>
        /// <value>
        /// The contact value.
        /// </value>
        Contact Value { get; set; }
    }

    public class ContactGroup<T> : ObservableSortedList<T>, IComparable where T : class, INotifyPropertyChanged, IComparable
    {
        public ContactGroup(char firstLetter)
        {
            this.FirstLetter = firstLetter;
            this.Key = new string(firstLetter, 1);
        }

        public char FirstLetter { get; set; }
        public string Key { get; set; }
        public bool HasItems { get { return Count > 0; } }

        public int CompareTo(object obj)
        {
            if (!(obj is ContactGroup<T>))
            {
                return -1;
            }

            ContactGroup<T> other = obj as ContactGroup<T>;

            return this.FirstLetter - other.FirstLetter;
        }
    }
}
