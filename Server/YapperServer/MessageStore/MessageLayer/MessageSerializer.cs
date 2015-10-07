using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace MessageStore.MessageLayer
{
    static class MessageSerializer
    {
        public static byte[] SerializeToProtocolBuffer(this Message message)
        {
            MemoryStream stream = new MemoryStream();
            ProtoBuf.Serializer.Serialize<Message>(stream, message);

            return stream.ToArray();
        }

        public static Message DeSerializeFromProtocolBuffer(byte[] bytes)
        {
            MemoryStream stream = new MemoryStream(bytes);
            return ProtoBuf.Serializer.Deserialize<Message>(stream);
        }
    }
}
