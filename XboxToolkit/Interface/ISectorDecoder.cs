using System;

namespace XboxToolkit.Interface
{
    public interface ISectorDecoder : IDisposable
    {
        public bool Init();
        public XgdInfo GetXgdInfo();
        public uint TotalSectors();
        public uint SectorSize();
        public bool TryReadSector(long sector, out byte[] sectorData);
    }
}
