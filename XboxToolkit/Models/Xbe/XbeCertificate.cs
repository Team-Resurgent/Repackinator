using System;
using System.Runtime.InteropServices;
using XboxToolkit.Internal;

namespace XboxToolkit.Models.Xbe
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class XbeCertificate
    {
        public uint Size;
        public uint Time_Date;
        public uint Title_Id;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
        public byte[] Title_Name = Array.Empty<byte>();

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public uint[] Alt_Title_Id = Array.Empty<uint>();

        public uint Allowed_Media;
        public uint Game_Region;
        public uint Game_Ratings;
        public uint Disk_Number;
        public uint Version;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] Lan_Key = Array.Empty<byte>();

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] Sig_Key = Array.Empty<byte>();

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] Title_Alt_Sig_Key = Array.Empty<byte>();

        public static string GameRegionToString(uint region)
        {
            var gameRegion = string.Empty;
            var debug = (region & 0x80000000) == 0x80000000;
            if (debug)
            {
                gameRegion = "DBG";
            }

            var global = (region & 0x00000007) == 0x00000007;
            if (global)
            {
                gameRegion = gameRegion.Length > 0 ? $"GLO-{gameRegion}" : "GLO";
            }
            else
            {
                if ((region & 0x00000004) == 0x00000004)
                {
                    gameRegion = gameRegion.Length > 0 ? $"PAL-{gameRegion}" : "PAL";
                }
                if ((region & 0x00000002) == 0x00000002)
                {
                    gameRegion = gameRegion.Length > 0 ? $"JPN-{gameRegion}" : "JPN";
                }
                if ((region & 0x00000001) == 0x00000001)
                {
                    gameRegion = gameRegion.Length > 0 ? $"USA-{gameRegion}" : "USA";
                }
            }

            return gameRegion.Length == 0 ? "" : gameRegion;
        }

        public string AllowedMediaToString(uint media)
        {
            var allowedMedia = string.Empty;
            if ((media & 0x00000001) == 0x00000001)
            {
                allowedMedia = allowedMedia.Length > 0 ? $"HARD_DISK, {allowedMedia}" : "HARD_DISK";
            }
            if ((media & 0x00000002) == 0x00000002)
            {
                allowedMedia = allowedMedia.Length > 0 ? $"DVD_X2, {allowedMedia}" : "DVD_X2";
            }
            if ((media & 0x00000004) == 0x00000004)
            {
                allowedMedia = allowedMedia.Length > 0 ? $"DVD_CD, {allowedMedia}" : "DVD_CD";
            }
            if ((media & 0x00000008) == 0x00000008)
            {
                allowedMedia = allowedMedia.Length > 0 ? $"CD, {allowedMedia}" : "CD";
            }
            if ((media & 0x00000010) == 0x00000010)
            {
                allowedMedia = allowedMedia.Length > 0 ? $"DVD_5_RO, {allowedMedia}" : "DVD_5_RO";
            }
            if ((media & 0x00000020) == 0x00000020)
            {
                allowedMedia = allowedMedia.Length > 0 ? $"DVD_9_RO, {allowedMedia}" : "DVD_9_RO";
            }
            if ((media & 0x00000040) == 0x00000040)
            {
                allowedMedia = allowedMedia.Length > 0 ? $"DVD_5_RW, {allowedMedia}" : "DVD_5_RW";
            }
            if ((media & 0x00000080) == 0x00000080)
            {
                allowedMedia = allowedMedia.Length > 0 ? $"DVD_9_RW, {allowedMedia}" : "DVD_9_RW";
            }
            if ((media & 0x00000100) == 0x00000100)
            {
                allowedMedia = allowedMedia.Length > 0 ? $"DONGLE, {allowedMedia}" : "DONGLE";
            }
            if ((media & 0x00000200) == 0x00000200)
            {
                allowedMedia = allowedMedia.Length > 0 ? $"MEDIA_BOARD, {allowedMedia}" : "MEDIA_BOARD";
            }
            if ((media & 0x40000000) == 0x40000000)
            {
                allowedMedia = allowedMedia.Length > 0 ? $"NONSECURE_HARD_DISK, {allowedMedia}" : "NONSECURE_HARD_DISK";
            }
            if ((media & 0x80000000) == 0x80000000)
            {
                allowedMedia = allowedMedia.Length > 0 ? $"NONSECURE_MODE, {allowedMedia}" : "NONSECURE_MODE";
            }
            return allowedMedia;
        }
    }
}
