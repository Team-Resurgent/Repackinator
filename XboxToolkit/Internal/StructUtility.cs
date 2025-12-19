using System.IO;
using System.Runtime.InteropServices;

namespace XboxToolkit.Internal
{
    internal static class StructUtility
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

    }
}
