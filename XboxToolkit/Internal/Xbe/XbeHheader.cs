using System.Runtime.InteropServices;

namespace XboxToolkit.Internal.Xbe
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct XbeHheader
    {
        public uint Magic;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] Dig_Sig;

        public uint Base;
        public uint Sizeof_Headers;
        public uint Sizeof_Image;
        public uint Sizeof_Image_Header;
        public uint Time_Date;
        public uint Certificate_Addr;
        public uint Sections;
        public uint Section_Headers_Addr;
        public uint Init_Flags; // 1 bit Mount util drive, 1 bit Format util drive, 1 bit Limit dev kit to 64mb, 1 bit Dont setup hdd, 28 bits unused
        public uint Entry;
        public uint Tls_Addr;
        public uint Pe_Stack_Commit;
        public uint Pe_Heap_Reserve;
        public uint Pe_Heap_Commit;
        public uint Pe_Base_Addr;
        public uint Pe_Sizeof_Image;
        public uint Pe_Checksum;
        public uint Pe_Time_Date;
        public uint Debug_Pathname_Addr;
        public uint Debug_Filename_Addr;
        public uint Debug_Unicode_Filename_Addr;
        public uint Kernel_image_Thunk_Addr;
        public uint Nonkernel_Import_Dir_Addr;
        public uint Library_Versions;
        public uint Library_Versions_Addr;
        public uint Kernel_Library_Version_Addr;
        public uint Xapi_Library_Version_Addr;
        public uint Logo_Bitmap_Addr;
        public uint Logo_Bitmap_Size;
    }
}
