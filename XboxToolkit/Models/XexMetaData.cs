using System;

namespace XboxToolkit.Models
{
    public class XexMetaData
    {
        public XexRegion GameRegion;

        public uint TitleId;

        public uint MediaId;

        public uint Version;

        public uint BaseVersion;

        public uint DiscNum;

        public uint DiscTotal;

        public string TitleName;

        public string Description;

        public string Publisher;

        public string Developer;

        public string Genre;

        public byte[] Thumbnail;

        public string Checksum;

        public XexMetaData()
        {
            TitleName = string.Empty;
            Description = string.Empty;
            Publisher = string.Empty;
            Developer = string.Empty;
            Genre = string.Empty;
            Thumbnail = Array.Empty<byte>();
            Checksum = string.Empty;
        }
    }
}
