using System.Runtime.InteropServices;

namespace XboxToolkit.Models.Dds
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DdsHeader
    {
        public uint Size;
        public uint Flags;
        public uint Height;
        public uint Width;
        public uint PitchOrLinearSize;
        public uint Depth;
        public uint MipMapCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
        public uint[] Reserved1;

        public DdsPixelFormat PixelFormat;

        public uint Caps;
        public uint Caps2;
        public uint Caps3;
        public uint Caps4;
        public uint Reserved2;
    }
}
