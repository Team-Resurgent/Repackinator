using System;
using System.IO;
using System.Runtime.InteropServices;
using XboxToolkit.Internal.Models;

namespace XboxToolkit.Internal
{
    internal class Helpers
    {
        public static T? ByteToType<T>(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T? theStructure = (T?)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return theStructure;
        }

        public static int SizeOf<T>()
        {
            return Marshal.SizeOf(typeof(T));
        }

        public static uint ConvertEndian(uint value)
        {
            return
                (value & 0x000000ff) << 24 |
                (value & 0x0000ff00) << 8 |
                (value & 0x00ff0000) >> 8 |
                (value & 0xff000000) >> 24;
        }

        public static ushort ConvertEndian(ushort value)
        {
            return (ushort)(
                (value & 0x000ff) << 8 |
                (value & 0xff00) >> 8
            );
        }

        public static XgdHeader? GetXgdHeaer(byte[] sector)
        {
            using var sectorStream = new MemoryStream(sector);
            using var sectorReader = new BinaryReader(sectorStream);
            var header = ByteToType<XgdHeader>(sectorReader);
            return header;
        }

        public static uint RoundToMultiple(uint size, uint multiple)
        {
            return ((size + multiple - 1) / multiple) * multiple;
        }

        public static bool IsEqualTo(float a, float b)
        {
            return Math.Abs(a - b) < 0.001;
        }

        public static void FillArray<T>(T[] array, T value)
        {
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }
        }

        public static void FillArray<T>(T[] array, T value, int startIndex, int count)
        {
            for (var i = startIndex; i < startIndex + count && i < array.Length; i++)
            {
                array[i] = value;
            }
        }
    }
}
