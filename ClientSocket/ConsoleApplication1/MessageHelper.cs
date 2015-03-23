using System;
using System.Text;

namespace Serwer
{
    class MessageHelper
    {
        public static bool unserialize(byte[] received, ref CommandHeader header, ref string command, ref string data)
        {
            if (CommandHeader.getHeader(received, ref header))
            {
                if (header.command_length + header.data_length + CommandHeader.HEADER_SIZE == received.Length)
                {
                    command = Encoding.ASCII.GetString(received, CommandHeader.HEADER_SIZE, header.command_length);
                    data = Encoding.ASCII.GetString(received, CommandHeader.HEADER_SIZE + header.command_length, (int)header.data_length);
                    return true;
                }
            }
            return false;
        }

        public static byte[] serialize(ref CommandHeader header_s, string command_str, string data_str)
        {
            byte[] command = Encoding.ASCII.GetBytes(command_str);
            byte[] data = Encoding.ASCII.GetBytes(data_str);
            header_s.command_length = (byte)command.Length;
            header_s.data_length = (Int32)data.Length;
            byte[] header = header_s.toBytes();
            byte[] msg = new byte[header.Length + command.Length + data.Length];
            Array.Copy(header, 0, msg, 0, header.Length);
            Array.Copy(command, 0, msg, header.Length, command.Length);
            Array.Copy(data, 0, msg, header.Length + command.Length, data.Length);
            return msg;
        }
    }
}
