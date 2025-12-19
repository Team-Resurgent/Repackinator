using System.Runtime.InteropServices;

namespace XboxToolkit.Internal.Xbe
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct XbeTls
    {
        public uint DataStartAddr;
        public uint DataEndAddr;
        public uint TlsIndexAddr;
        public uint TlsCallbackAddr;
        public uint SizeofZeroFill;
        public uint Characteristics;
    }
}
