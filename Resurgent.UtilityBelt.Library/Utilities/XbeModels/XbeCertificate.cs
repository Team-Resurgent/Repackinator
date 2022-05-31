using System.Runtime.InteropServices;

namespace Resurgent.UtilityBelt.Library.Utilities.XbeModels
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct XbeCertificate
    {
        public uint Size;
        public uint Time_Date;
        public uint Title_Id;   

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
        public byte[] Title_Name; 

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public uint[] Alt_Title_Id;    

        public uint Allowed_Media; 
        public uint Game_Region;      
        public uint Game_Ratings;
        public uint Disk_Number;
        public uint Version;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] Lan_Key;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] Sig_Key;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] Title_Alt_Sig_Key;
    }
}
