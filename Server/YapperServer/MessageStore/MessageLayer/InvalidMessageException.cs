using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageStore.MessageLayer
{
    public class InvalidMessageException : Exception
    {
        public InvalidMessageException(InvalidMessageError error)
        {
            this.Error = error;
        }

        public InvalidMessageError Error
        {
            get;
            set;
        }
    }
}
