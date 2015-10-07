using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YapperChat.Controls.Interactions
{
    public interface IItem
    {
        void SetTaskName(string name);

        bool IsCompleted
        {
            get;
            set;
        }

        bool IsPullDown
        {
            get;
        }

        string ItemOrder
        {
            get;
            set;
        }

        object Clone();
    }
}
