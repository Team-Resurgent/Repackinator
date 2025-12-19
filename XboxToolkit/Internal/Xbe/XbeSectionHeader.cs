using System;
using System.Runtime.InteropServices;

namespace XboxToolkit.Internal.Xbe
{
    [Flags]
    internal enum XbeSectionFlags : uint
    {
        Writable = 1,
        Preload = 2,
        Executable = 4,
        Inserted_File = 8,
        Head_Page_Read_Only = 16,
        Tail_Page_Read_Only = 32
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct XbeSectionHeader
    {
        public XbeSectionFlags Flags;
        public uint Virtual_Addr;
        public uint Virtual_Size;
        public uint Raw_Addr;
        public uint Sizeof_Raw;
        public uint Section_Name_Addr;
        public uint Section_Reference_Count;
        public uint Head_Shared_Ref_Count_Addr;
        public uint Tail_Shared_Ref_Count_Addr;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] Section_Digest;
    }
}
