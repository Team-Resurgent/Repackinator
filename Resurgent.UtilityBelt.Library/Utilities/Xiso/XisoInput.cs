namespace Resurgent.UtilityBelt.Library.Utilities.Xiso
{
    public class XisoInput : IImageInput
    {
        public struct IsoSliceInfo
        {
            public Stream Stream { get; set; }

            public long StartSector { get; set; }

            public long EndSector { get; set; }
        }

        private readonly List<IsoSliceInfo> m_slices;

        private bool m_disposed = false;

        public long m_cacheStartSector = 0;

        private byte[] m_cacheSectorData = Array.Empty<byte>();

        private long m_totalSectors = 0;
        public long TotalSectors => m_totalSectors;

        public byte[] ReadSectors(long startSector, long count)
        {
            if (m_cacheStartSector == startSector && m_cacheSectorData.Length == count << 11)
            {
                return m_cacheSectorData;
            }

            var result = new byte[count << 11];
            var sectorOffset = 0;
            var sectorsRemaining = count;
            foreach (var slice in m_slices)
            {
                if (count > 0 && startSector >= slice.StartSector && startSector <= slice.EndSector)
                {
                    var sliceSectorsToRead = (int)(Math.Min((startSector - slice.StartSector) + sectorsRemaining, (slice.EndSector - slice.StartSector)) - (startSector - slice.StartSector));
                    slice.Stream.Position = (startSector - slice.StartSector) << 11;
                    slice.Stream.Read(result, sectorOffset << 11, sliceSectorsToRead << 11);
                    sectorOffset += sliceSectorsToRead;
                    count -= sliceSectorsToRead;
                }                
            }

            m_cacheStartSector = startSector;
            m_cacheSectorData = result;

            return result;
        }

        public ulong ReadUint64(long position)
        {
            return ReadUint32(position + 4) << 32 | ReadUint32(position);
        }

        public uint ReadUint32(long position)
        {
            return (uint)(ReadUint16(position + 2) << 16 | ReadUint16(position));
        }

        public ushort ReadUint16(long position)
        {
            return (ushort)(ReadByte(position + 1) << 8 | ReadByte(position));
        }

        public byte[] ReadBytes(long position, uint length)
        {
            byte[] result = new byte[length];
            for (var i = 0; i < length; i++)
            {
                result[i] = ReadByte(position + i);
            }
            return result;
        }

        public byte ReadByte(long position)
        {
            var sector = ReadSectors(position >> 11, 1);
            return sector[position % 2048];
        }

        public XisoInput(string[] parts)
        {
            var totalSectors = 0L;
            var startSector = 0L;
            m_slices = new List<IsoSliceInfo>();  
            foreach (var part in parts)
            {
                var stream = new FileStream(part, FileMode.Open, FileAccess.Read);
                stream.Seek(0, SeekOrigin.End);
                stream.Seek(0, SeekOrigin.Begin);
                var sectorCount = stream.Length >> 11;
                m_slices.Add(new IsoSliceInfo
                {
                    Stream = stream,
                    StartSector = startSector,
                    EndSector = startSector + sectorCount - 1
                });
                startSector += sectorCount;
                totalSectors += sectorCount;
            }
            m_totalSectors = totalSectors;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_disposed) 
            { 
                return;
            }
            if (disposing)
            {
                foreach (var slice in m_slices)
                {
                    slice.Stream.Dispose();
                }
            }
            m_disposed = true;
        }
    }
}
