using System;
using System.Text;

namespace Serwer
{
    struct CommandHeader
    {
        public const Int32 HEADER_SIZE = 18;
        public Byte command_length;
        public Int32 id_num;
        public Int32 port;
        public Int32 data_length;
        public Int32 part_num;
        public Byte last_part;

        public static bool getHeader(byte[] received, ref CommandHeader header)
        {
            if (received.Length < CommandHeader.HEADER_SIZE)
            {
                return false;
            }
            else
            {
                header.command_length = received[0];
                header.last_part = received[CommandHeader.HEADER_SIZE - 1];

                if (BitConverter.IsLittleEndian)
                {
                    byte[] copy = new byte[CommandHeader.HEADER_SIZE];
                    Array.Copy(received, copy, CommandHeader.HEADER_SIZE);

                    Array.Reverse(copy, 1, 4);
                    header.id_num = BitConverter.ToInt32(copy, 1);

                    Array.Reverse(copy, 5, 4);
                    header.port = BitConverter.ToInt32(copy, 5);

                    Array.Reverse(copy, 9, 4);
                    header.data_length = BitConverter.ToInt32(copy, 9);

                    Array.Reverse(copy, 13, 4);
                    header.part_num = BitConverter.ToInt32(copy, 13);
                }
                else
                {
                    header.id_num = BitConverter.ToInt32(received, 1);
                    header.port = BitConverter.ToInt32(received, 5);
                    header.data_length = BitConverter.ToInt32(received, 9);
                    header.part_num = BitConverter.ToInt32(received, 13);
                }
                return true;
            }
        }

        public byte[] toBytes()
        {
            byte[] bytes = new byte[CommandHeader.HEADER_SIZE];
            bytes[0] = this.command_length;
            Array.Copy(BitConverter.GetBytes(this.id_num), 0, bytes, 1, 4);
            Array.Copy(BitConverter.GetBytes(this.port), 0, bytes, 5, 4));
            Array.Copy(BitConverter.GetBytes(this.data_length), 0, bytes, 9, 4);
            Array.Copy(BitConverter.GetBytes(this.part_num), 0, bytes, 13, 4);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes, 1, 4);
                Array.Reverse(bytes, 5, 4);
                Array.Reverse(bytes, 9, 4);
                Array.Reverse(bytes, 13, 4);
            }
            bytes[CommandHeader.HEADER_SIZE - 1] = this.last_part;
            return bytes;
        }
    }
}
