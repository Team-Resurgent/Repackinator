using System.Runtime.InteropServices;

namespace XboxToolkit.Internal.Xbe
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct XbeLibraryVersion
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Name;

        public short MajorVersion;
        public short MinorVersion;
        public short BuildVersion;
        public short Flasgs; // 13 bits QfeVersion, 2 bits Approved (0-no, 1-possibly, 2-yes), 1 bit Debug Build (0-no, 1-yes)
    }
}
