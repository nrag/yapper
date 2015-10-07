using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YapperChat.Controls.Interactions
{
    public interface IItemManager<T> where T : class, IItem
    {
        T CreateItem();

        void DeleteItem(T item);

        void CompleteItem(T item);

        string GetItemOrder(T itemBefore, T itemAfter);

        void SetItemOrder(T itemToSet, T itemBefore, T itemAfter, bool save = false);

        bool HasChanged
        {
            get;
        }

        bool IsDeleted
        {
            get;
        }
    }
}
