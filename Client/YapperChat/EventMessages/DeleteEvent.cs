using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YapperChat.EventMessages
{
    enum DeleteState
    {
        Start,

        Complete
    }

    class DeleteEvent
    {
        public DeleteState DeleteState
        {
            get;
            set;
        }
    }
}
