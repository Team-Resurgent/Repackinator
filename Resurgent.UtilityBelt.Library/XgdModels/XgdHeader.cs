using System.Runtime.InteropServices;

namespace Resurgent.UtilityBelt.Library.Utilities.XgdModels
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class XgdHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] Magic = Array.Empty<byte>();

        public string MagicString => StringHelper.GetUtf8String(Magic);

        public uint RootDirSector;

        public uint RootDirSize;

        public long CreationFileTime;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x7c8)]
        public byte[] Padding = Array.Empty<byte>();

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] MagicTail = Array.Empty<byte>();

        public string MagicTailString => StringHelper.GetUtf8String(MagicTail);
    }
}
