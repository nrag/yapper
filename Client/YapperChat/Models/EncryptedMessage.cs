using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YapperChat.Common;

using ProtoBuf;

namespace YapperChat.Models
{
    /// <summary>
    /// Extensions to encrypt and decrypt messages
    /// </summary>
    public static class EncryptedMessage
    {
        public static MessageModel DecryptMessage(this MessageModel message)
        {
            if ((message.MessageFlags & MessageFlags.EncryptedMessage) != MessageFlags.EncryptedMessage)
            {
                return message;
            }

            if (message.EncryptedMessage == null)
            {
                throw new Exception("Encrypted blob is missing");
            }

            if (UserSettingsModel.Instance.PrivateKey == null)
            {
                throw new Exception("Private key is missing");
            }

            try
            {
                using (MemoryStream stream = new MemoryStream(message.EncryptedMessage))
                {
                    // Read the encrypted AES encryption keys
                    // encrypted using sender's public key and recipient's public key
                    byte[] encryptedDataKeyForRecipient = stream.ReadNextBlob();
                    byte[] encryptedDataKeyForSender = stream.ReadNextBlob();

                    // Decrypt my copy of the AES key
                    byte[] aesKey = RsaEncryption.DecryptMessage(message.IsMine ? encryptedDataKeyForSender : encryptedDataKeyForRecipient, UserSettingsModel.Instance.PrivateKey);

                    // Decrypt the message encrypted using the AES key
                    byte[] serializedMessage = AesEncryption.DecryptMessage(stream.ReadNextBlob(), aesKey);

                    // Deserialize the protobuf format
                    return ProtoBuf.Serializer.Deserialize<MessageModel>(new MemoryStream(serializedMessage));
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static MessageModel EncryptMessage(this MessageModel message)
        {
            if (message.Recipient!= null && message.Recipient.PublicKey == null)
            {
                return message;
            }

            // Serialize the message
            MemoryStream stream = new MemoryStream();
            ProtoBuf.Serializer.Serialize<MessageModel>(stream, message);

            // Encrypt it using AES
            byte[] aesKey;
            byte[] encryptedSerializedMessage = AesEncryption.EncryptMessage(stream.ToArray(), out aesKey);

            // Encrypt the AES key using sender's Public Key and recipient's public key
            byte[] senderEncryptedKey = RsaEncryption.EncryptMessage(aesKey, message.Sender.PublicKey);
            byte[] recipientEncryptedKey = RsaEncryption.EncryptMessage(aesKey, message.Recipient.PublicKey);

            // Put them together to create an encrypted message
            MemoryStream encryptedMessageBytes = new MemoryStream();
            encryptedMessageBytes.WriteBlob(recipientEncryptedKey);
            encryptedMessageBytes.WriteBlob(senderEncryptedKey);
            encryptedMessageBytes.WriteBlob(encryptedSerializedMessage);

            // Copy only the minimal amount of data
            MessageModel encryptedMessage = new MessageModel();
            encryptedMessage.EncryptedMessage = encryptedMessageBytes.ToArray();
            encryptedMessage.MessageFlags = MessageFlags.EncryptedMessage;
            message.CopyNonEncryptedProperties(encryptedMessage);

            encryptedMessage.ClientMessageId = message.ClientMessageId;

            return encryptedMessage;
        }

        public static void CopyNonEncryptedProperties(this MessageModel message, MessageModel other)
        {
            other.MessageId = message.MessageId;
            other.Sender = message.Sender;
            other.Recipient = message.Recipient;
            other.ConversationId = message.ConversationId;
            other.PostDateTime = message.PostDateTime;
            other.LastUpdateTime = message.LastUpdateTime;

            // Set the columns that have to be saved in the table
            other.SenderId = other.Sender.Id;
            other.RecipientId = other.Recipient.Id;
            other.LastReadTime = new DateTime(1970, 1, 1);
            other.MessageType = MessageType.Message;
        }
    }
}
