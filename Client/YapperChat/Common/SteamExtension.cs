using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace YapperChat.Common
{
    public static class SteamExtension
    {
        public static byte[] ReadNextBlob(this Stream stream)
        {
            byte[] contentBuffer;
            int size;

            size = stream.ReadInt();
            contentBuffer = new byte[size];
            stream.Read(contentBuffer, 0, size);

            return contentBuffer;
        }

        public static int ReadInt(this Stream stream)
        {
            byte[] buffer = new byte[sizeof(int)];
            stream.Read(buffer, 0, sizeof(int));
            return BitConverter.ToInt32(buffer, 0);
        }

        public static void WriteInt(this Stream stream, int v)
        {
            stream.Write(BitConverter.GetBytes(v), 0, sizeof(int));
        }

        public static void WriteBlob(this Stream stream, byte[] buffer)
        {
            stream.WriteInt(buffer.Length);
            stream.Write(buffer, 0, buffer.Length);
        }
    }
}
