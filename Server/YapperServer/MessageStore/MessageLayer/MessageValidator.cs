using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageStore.MessageLayer
{
    static class MessageValidator
    {
        public static void Validate(this Message message, DataAccessLayer.User sender)
        {
            message.MessageId = Guid.NewGuid();
            message.LastUpdateTimeUtcTicks = DateTime.UtcNow.Ticks;
            message.PostDateTimeUtcTicks = DateTime.UtcNow.Ticks;

            if (message.Sender == null || message.Sender.Id != sender.Id)
            {
                throw new InvalidMessageException(InvalidMessageError.InvalidSenderError);
            }

            if (message.Recipient == null || message.Recipient.Id == message.Sender.Id)
            {
                throw new InvalidMessageException(InvalidMessageError.InvalidRecipientError);
            }

            if (message.ConversationId != GetConversationId(message.Sender, message.Recipient) &&
                message.ConversationId != Guid.Empty)
            {
                throw new InvalidMessageException(InvalidMessageError.InvalidSenderError);
            }

            if (message.ConversationId == Guid.Empty)
            {
                int senderId = message.Recipient.UserType == DataAccessLayer.UserType.Group ? 0 : message.SenderId;

                message.ConversationId = MessageValidator.GetConversationGuid(senderId, message.RecipientId);
            }

            if (((message.MessageFlags & MessageFlags.Image) == MessageFlags.Image) &&
                message.Image == null)
            {
                throw new InvalidMessageException(InvalidMessageError.ImageMissingError);
            }

            if (((message.MessageFlags & (MessageFlags.PollMessage | MessageFlags.Calendar)) == MessageFlags.PollMessage) &&
                (message.PollOptions == null || message.PollOptions.Count == 0))
            {
                throw new InvalidMessageException(InvalidMessageError.InvalidPollOptionsError);
            }

            if (((message.MessageFlags & MessageFlags.PollResponseMessage) == MessageFlags.PollResponseMessage) &&
                (message.PollMessageId == Guid.Empty))
            {
                throw new InvalidMessageException(InvalidMessageError.InvalidPollResponseError);
            }

            if ((message.MessageFlags & MessageFlags.PollResponseMessage) == MessageFlags.PollResponseMessage)
            {
                if (message.PollMessageId == null)
                {
                    throw new InvalidMessageException(InvalidMessageError.InvalidPollResponseError);
                }

                Message pollMessage = MessageStore.Instance.GetMessage(message.ConversationId, message.PollMessageId.Value);
                if (pollMessage == null)
                {
                    throw new InvalidMessageException(InvalidMessageError.InvalidPollResponseError);
                }

                if ((pollMessage.MessageFlags & MessageFlags.PollMessage) != MessageFlags.PollMessage)
                {
                    throw new InvalidMessageException(InvalidMessageError.InvalidPollResponseError);
                }

                if (((pollMessage.MessageFlags & MessageFlags.Calendar) == 0) &&
                    !pollMessage.PollOptions.Contains(message.PollResponse))
                {
                    throw new InvalidMessageException(InvalidMessageError.InvalidPollResponseError);
                }
            }

            if (message.ClientMessageId == Guid.Empty)
            {
                message.ClientMessageId = Guid.NewGuid();
            }
        }

        private static Guid GetConversationId(DataAccessLayer.User sender, DataAccessLayer.User recipient)
        {
            int senderId = recipient.UserType == DataAccessLayer.UserType.Group ? 0 : sender.Id;

            return MessageValidator.GetConversationGuid(senderId, recipient.Id);
        }

        private static Guid GetConversationGuid(long sender, long recipient)
        {
            long small, big;
            small = sender > recipient ? recipient : sender;
            big = sender > recipient ? sender : recipient;

            Int16 firstPart = (Int16)((small & 0xffff00000000L) >> 32);
            Int16 secondPart = (Int16)((small & 0x0000ffff0000L) >> 16);
            byte thirdPart = (byte)((small & 0xff00L) >> 16);
            byte fourthPart = (byte)(small & 0xffL); ;
            byte byte1 = (byte)((big & 0xff0000000000L) >> 40);
            byte byte2 = (byte)((big & 0x00ff00000000L) >> 32);
            byte byte3 = (byte)((big & 0x0000ff000000L) >> 24);
            byte byte4 = (byte)((big & 0x000000ff0000L) >> 16);
            byte byte5 = (byte)((big & 0x00000000ff00L) >> 8);
            byte byte6 = (byte)((big & 0x0000000000ffL));

            return new Guid(0, firstPart, secondPart, thirdPart, fourthPart, byte1, byte2, byte3, byte4, byte5, byte6);
        }
    }
}
