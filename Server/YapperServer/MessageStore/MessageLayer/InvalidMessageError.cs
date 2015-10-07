using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageStore.MessageLayer
{
    public enum InvalidMessageError
    {
        InvalidSenderError,

        InvalidRecipientError,

        ImageMissingError,

        InvalidPollOptionsError,

        InvalidPollResponseError,

        InvalidGroupMessageError,
    }
}
