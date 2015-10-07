using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YapperChat.EventMessages
{
    class RefreshTaskMessage
    {
        public Guid TaskId
        {
            get;
            set;
        }
    }
}
