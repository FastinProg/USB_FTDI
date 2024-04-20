using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USB_FTDI
{
    public static class CommonFunction
    {
        static public ushort Swap16(ushort value)
        {
            value = (ushort)((byte)(value >> 8) | (ushort)((value & 0xff) << 8));
            return value;
        }
        static public uint Swap32(uint value)
        {
            value = (uint)((byte)(value >> 8) | (uint)((value & 0xff) << 8) | (uint)((value & 0xff00) << 16) | (uint)((value & 0xff0000) << 24));
            return value;
        }

    }
}
