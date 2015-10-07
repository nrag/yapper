using System.Collections.ObjectModel;
using Microsoft.Phone.UserData;
using System;
using YapperChat.Common;
using System.ComponentModel;

namespace YapperChat.Models
{
    public class PollResponseGroup<T> : ObservableSortedList<T>, IComparable where T : class, INotifyPropertyChanged, IComparable
    {
        public PollResponseGroup(string response)
        {
            this.Response = response;
  //          this.Key = new string(firstLetter, 1);
        }

        public string Response { get; set; }
//        public string Key { get; set; }
        public bool HasItems { get { return Count > 0; } }

        public int CompareTo(object obj)
        {
            if (!(obj is ContactGroup<T>))
            {
                return -1;
            }

            PollResponseGroup<T> other = obj as PollResponseGroup<T>;

            return string.Compare(this.Response, other.Response, StringComparison.OrdinalIgnoreCase);
        }
    }
}

