using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IsmMp4Util
{
    class Utils
    {
        internal const ulong OFFSET_LENGTH = 4;
        internal const ulong IDENTIFIER_LENGTH = 4;
        internal const ulong HEADER_LENGTH = OFFSET_LENGTH + IDENTIFIER_LENGTH;

        internal static uint ReadUInt32(ref BinaryReader _br, UInt64 iIndex)
        {
            // Seek and read from file
            byte[] buff = new byte[4];
            _br.BaseStream.Seek((long)iIndex, SeekOrigin.Begin);
            _br.Read(buff, 0, 4);

            uint uiValue = 0;
            uiValue = buff[0];
            uiValue <<= 8;
            uiValue |= buff[1];
            uiValue <<= 8;
            uiValue |= buff[2];
            uiValue <<= 8;
            uiValue |= buff[3];

            return uiValue;
        }

        internal static uint ReadUInt32(byte[] buff, UInt64 iIndex)
        {
            uint uiValue = 0;
            uiValue = buff[iIndex];
            uiValue <<= 8;
            uiValue |= buff[iIndex + 1];
            uiValue <<= 8;
            uiValue |= buff[iIndex + 2];
            uiValue <<= 8;
            uiValue |= buff[iIndex + 3];

            return uiValue;
        }

        internal static UInt64 ReadUInt64(ref BinaryReader _br, UInt64 iIndex)
        {
            // Seek and read from file
            byte[] buff = new byte[8];
            _br.BaseStream.Seek((Int64)iIndex, SeekOrigin.Begin);
            _br.Read(buff, 0, 8);

            UInt64 uiValue = 0;
            uiValue = buff[0];
            uiValue <<= 8;
            uiValue |= buff[1];
            uiValue <<= 8;
            uiValue |= buff[2];
            uiValue <<= 8;
            uiValue |= buff[3];
            uiValue <<= 8;
            uiValue |= buff[4];
            uiValue <<= 8;
            uiValue |= buff[5];
            uiValue <<= 8;
            uiValue |= buff[6];
            uiValue <<= 8;
            uiValue |= buff[7];

            return uiValue;
        }

        internal static char[] ReadChars(ref BinaryReader _br, UInt64 iIndex, int iCount)
        {
            // Seek and read from file
            char[] buff = new char[iCount];
            _br.BaseStream.Seek((long)iIndex, SeekOrigin.Begin);
            _br.Read(buff, 0, iCount);

            return buff;
        }

        internal static byte[] ReadBytes(ref BinaryReader _br, UInt64 iIndex, int iCount)
        {
            // Seek and read from file
            byte[] buff = new byte[iCount];
            _br.BaseStream.Seek((long)iIndex, SeekOrigin.Begin);
            _br.Read(buff, 0, iCount);

            return buff;
        }

        internal static bool ReadAtom(byte[] buff, ref UInt64 iLength, out char[] sID)
        {
            if (buff.Length < 8)
            {
                sID = null;
                return true;
            }

            UInt64 iBuffLen = 0;
            iBuffLen = buff[0];
            iBuffLen <<= 8;
            iBuffLen |= buff[1];
            iBuffLen <<= 8;
            iBuffLen |= buff[2];
            iBuffLen <<= 8;
            iBuffLen |= buff[3];

            iLength = iBuffLen;

            sID = new char[4];
            sID[0] = (char)buff[4];
            sID[1] = (char)buff[5];
            sID[2] = (char)buff[6];
            sID[3] = (char)buff[7];

            return false;
        }

        internal static Int64 ReadInt64(byte[] buff, UInt64 iIndex)
        {
            Int64 uiValue = 0;
            uiValue = buff[iIndex];
            uiValue <<= 8;
            uiValue |= buff[iIndex + 1];
            uiValue <<= 8;
            uiValue |= buff[iIndex + 2];
            uiValue <<= 8;
            uiValue |= buff[iIndex + 3];
            uiValue <<= 8;
            uiValue |= buff[iIndex + 4];
            uiValue <<= 8;
            uiValue |= buff[iIndex + 5];
            uiValue <<= 8;
            uiValue |= buff[iIndex + 6];
            uiValue <<= 8;
            uiValue |= buff[iIndex + 7];

            return uiValue;
        }
    }
}
