using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YapperChat.Models
{
    public class PollOption
    {
        public string Option
        {
            get;
            set;
        }

        public bool IsSelected
        {
            get;
            set;
        }
    }
}
