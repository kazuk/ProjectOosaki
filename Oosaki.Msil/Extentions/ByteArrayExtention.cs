using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oosaki.Msil.Extentions
{
    public static class ByteArrayExtention
    {
        public static Int16 ReadInt16(this byte[] bytes, int offset)
        {
            return BitConverter.ToInt16(bytes, offset);
        }

        public static Int32 ReadInt32(this byte[] bytes, int offset)
        {
            return BitConverter.ToInt32(bytes,offset);
        }

        public static Double ReadDouble(this byte[] bytes, int offset)
        {
            return BitConverter.ToDouble(bytes, offset);
        }
    }
}
