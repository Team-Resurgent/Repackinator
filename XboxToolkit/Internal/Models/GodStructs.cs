using System.Runtime.InteropServices;

namespace XboxToolkit.Internal.Models
{
    internal enum XCONTENT_SIGNATURE_TYPE
    {
        CONSOLE_SIGNED = 0x434F4E20,    // CON
        LIVE_SIGNED = 0x4C495645,       // LIVE
        PIRS_SIGNED = 0x50495253        // PIRS
    }

    internal enum XCONTENT_VOLUME_TYPE
    {
        STFS_VOLUME = 0x0,
        SVOD_VOLUME = 0x1
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct XCONTENT_SIGNATURE
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x100)]
        public byte[] Signature;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x128)]
        public byte[] Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct XCONTENT_LICENSE
    {
        public ulong LicenseeId;
        public uint LicenseBits;
        public uint LicenseFlags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct XCONTENT_HEADER
    {
        public uint SignatureType;
        public XCONTENT_SIGNATURE Signature;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public XCONTENT_LICENSE[] LicenseDescriptors;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
        public byte[] ContentId;
        public uint SizeOfHeaders;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct XEX_EXECUTION_ID
    {
        public uint MediaId;
        public uint Version;
        public uint BaseVersion;
        public uint TitleId;
        public byte Platform;
        public byte ExecutableType;
        public byte DiscNum;
        public byte DiscsInSet;
        public uint SaveGameID;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SVOD_DEVICE_DESCRIPTOR
    {
        public byte DescriptorLength;
        public byte BlockCacheElementCount;
        public byte WorkerThreadProcessor;
        public byte WorkerThreadPriority;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
        public byte[] FirstFragmentHashEntry;
        public byte Features;
        public byte NumberOfDataBlocks2;
        public byte NumberOfDataBlocks1;
        public byte NumberOfDataBlocks0;
        public byte StartingDataBlock0;
        public byte StartingDataBlock1;
        public byte StartingDataBlock2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x5)]
        public byte[] Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct XCONTENT_METADATA_MEDIA_DATA
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[] SeriesId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[] SeasonId;
        public ushort SeasonNumber;
        public ushort EpisodeNumber;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct StringType
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x100)]
        public byte[] Value;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct XCONTENT_METADATA
    {
        public uint ContentType;
        public uint ContentMetadataVersion;
        public long ContentSize;
        public XEX_EXECUTION_ID ExecutionId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x5)]
        public byte[] ConsoleId;
        public long Creator;
        public SVOD_DEVICE_DESCRIPTOR SvodVolumeDescriptor;
        public uint DataFiles;
        public long DataFilesSize;
        public uint VolumeType;
        public long OnlineCreator;
        public uint Category;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        public byte[] Reserved2;
        public XCONTENT_METADATA_MEDIA_DATA Data;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
        public byte[] DeviceId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x9)]
        public StringType[] DisplayName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x9)]
        public StringType[] Description;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x80)]
        public byte[] Publisher;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x80)]
        public byte[] TitleName;
        public byte Flags;
        public uint ThumbnailSize;
        public uint TitleThumbnailSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3D00)]
        public byte[] Thumbnail;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3)]
        public StringType[] DisplayNameEx;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3D00)]
        public byte[] TitleThumbnail;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3)]
        public StringType[] DescriptionEx;
    }
}
