using System.Runtime.InteropServices;

namespace XboxToolkit.Models.Dds
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DdsPixelFormat
    {
        public uint Size;
        public uint Flags;
        public uint FourCC;
        public uint RGBBitCount;
        public uint RBitMask;
        public uint GBitMask;
        public uint BBitMask;
        public uint ABitMask;
    }
}
