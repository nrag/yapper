using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YapperChat.Models
{
    [Flags]
    public enum MessageFlags
    {
        /// <summary>
        /// This message has image
        /// </summary>
        Image = 0x1,

        /// <summary>
        /// Message has a poll
        /// </summary>
        PollMessage = 0x2,

        /// <summary>
        /// Message representing a poll response
        /// </summary>
        PollResponseMessage = 0x4,

        /// <summary>
        /// Calendar is a poll message with it's own set or properties
        /// It
        /// </summary>
        Calendar = 0x8,

        /// <summary>
        /// Group Join message
        /// </summary>
        GroupJoinMessage = 0x10,

        /// <summary>
        /// Group remove message when members are removed
        /// </summary>
        GroupLeaveMessage = 0x20,

        /// <summary>
        /// Group Add message when members are added
        /// </summary>
        GroupCreatedMessage = 0x40,

        /// <summary>
        /// Task message
        /// </summary>
        Task = 0x80,

        /// <summary>
        /// This is an encrypted message
        /// </summary>
        EncryptedMessage = 0x100,

        /// <summary>
        /// Task item
        /// </summary>
        TaskItem = 0x200,

        /// <summary>
        /// Task info that shows up in conversation
        /// </summary>
        TaskInfo = 0x400,
    }
}
