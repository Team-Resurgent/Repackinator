using LibDeflate;
using System.IO;
using System.Text;

namespace Resurgent.UtilityBelt.Library.Utilities.ImageInput
{
    public class CsoInput : IImageInput
    {
        private struct IndexInfo
        {
            public ulong Value { get; set; }

            public bool LZ4 { get; set; }
        }

        private class CsoSliceInfo
        {
            public Stream Stream { get; set; }

            public long StartSector { get; set; }

            public long EndSector { get; set; }

            public CsoSliceInfo(Stream stream)
            {
                Stream = stream;
                StartSector = 0; 
                EndSector = 0;
            }
        }

        private struct CsoInfo
        {
            public List<CsoSliceInfo> SliceInfos { get; set; } 
            public List<IndexInfo> IndexInfos { get; set; } 

            public CsoInfo() 
            {
                SliceInfos = new List<CsoSliceInfo>();
                IndexInfos =  new List<IndexInfo>();
            }
        }

        private struct SectorCache
        {
            public long StartSector { get; set; }

            public long Count { get; set; }

            public byte[] SectorData { get; set;} 
        }

        private readonly CsoInfo m_csoInfo;

        private bool m_disposed = false;

        public long m_cacheStartSector = 0;

        private byte[] m_cacheSectorData = Array.Empty<byte>();

        private long m_totalSectors = 0;
        public long TotalSectors => m_totalSectors;

        private string[] m_parts = Array.Empty<string>();
        public string[] Parts => m_parts;

        public byte[] ReadSectors(long startSector, long count)
        {
            if (m_cacheStartSector == startSector && m_cacheSectorData.Length == count << 11)
            {
                return m_cacheSectorData;
            }

            var result = new byte[count << 11];
            var sector = startSector;
            var sectorOffset = 0;

            while (count > 0)
            {

                var indexInfo = m_csoInfo.IndexInfos[(int)sector];
                var compressed = indexInfo.LZ4;
                var position = (long)indexInfo.Value;
                var size = (long)m_csoInfo.IndexInfos[(int)sector + 1].Value - position;

                var sliceIndex = SectorInSlice(startSector);
                var stream = m_csoInfo.SliceInfos[sliceIndex].Stream;

                stream.Position = position;

                using (var reader = new BinaryReader(stream, Encoding.Default, true))
                {
                    if (compressed)
                    {
                        var outputBuffer = new byte[2048];
                        var length = reader.ReadUInt32();
                        var rawData = reader.ReadBytes((int)length);
                        reader.BaseStream.Position = position + (size - length - 4);

                        var compressedSize = K4os.Compression.LZ4.LZ4Codec.Decode(rawData, 0, rawData.Length, outputBuffer, 0, 2048);
                        if (compressedSize != 2048)
                        {
                            throw new IndexOutOfRangeException("Unable to decompress sector.");
                        }

                        Array.Copy(outputBuffer, 0, result, sectorOffset << 11, 2048);
                    }
                    else
                    {
                        var temp = reader.ReadBytes(2048);
                        Array.Copy(temp, 0, result, sectorOffset << 11, 2048);
                    }

                    sector++;
                    sectorOffset++;
                    count--;
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

        public int SectorInSlice(long sector)
        {
            for (int i = 0; i < m_csoInfo.SliceInfos.Count; i++)
            {
                CsoSliceInfo slice = m_csoInfo.SliceInfos[i];
                if (sector >= slice.StartSector && sector <= slice.EndSector)
                {
                    return i;
                }
            }
            throw new IndexOutOfRangeException();
        }

        public CsoInput(string[] parts)
        {
            m_parts = parts;

            m_csoInfo.SliceInfos = new List<CsoSliceInfo>();
            for (var i = 0; i < parts.Length; i++)
            {
                var stream = new FileStream(parts[i], FileMode.Open, FileAccess.Read);
                m_csoInfo.SliceInfos.Add(new CsoSliceInfo(stream));
            }

            using (var stream = new FileStream(parts[0], FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(stream, Encoding.Default, true))
            {
                var header = reader.ReadUInt32();
                if (header != 0x4F534943)
                {
                    throw new IOException("Invalid magic value in cso header.");
                }

                uint headerSize = reader.ReadUInt32();
                if (headerSize != 24)
                {
                    throw new IOException("Invalid header size in cso header.");
                }

                ulong uncompressedSize = reader.ReadUInt64();

                ulong indexOffset = headerSize;

                uint blockSize = reader.ReadUInt32();
                if (blockSize != 2048)
                {
                    throw new IOException("Invalid block size in cso header.");
                }

                byte version = reader.ReadByte();
                if (version != 2)
                {
                    throw new IOException("Invalid version in cso header.");
                }

                byte indexAlignment = reader.ReadByte();
                if (indexAlignment != 2)
                {
                    throw new IOException("Invalid index allignment in cso header.");
                }

                var entries = (int)(uncompressedSize / (ulong)blockSize);

                stream.Position = (long)indexOffset;

                m_csoInfo.IndexInfos = new List<IndexInfo>();
                for (var i = 0; i <= entries; i++)
                {
                    var index = reader.ReadUInt32();
                    m_csoInfo.IndexInfos.Add(new IndexInfo
                    {
                        Value = (ulong)(index & 0x7FFFFFFF) << indexAlignment,
                        LZ4 = (index & 0x80000000) > 0
                    });
                }

                var totalSectors = uncompressedSize >> 11;

                var currentPart = 0;
                for (var i = 0; i <= m_csoInfo.IndexInfos.Count - 1; i++)
                {
                    var part = m_csoInfo.SliceInfos[currentPart];
                    var position = m_csoInfo.IndexInfos[i].Value;
                    if (position == 0)
                    {
                        m_csoInfo.SliceInfos[currentPart].EndSector = i - 1;
                        currentPart++;
                        m_csoInfo.SliceInfos[currentPart].StartSector = i;
                        m_csoInfo.SliceInfos[currentPart].EndSector = m_csoInfo.IndexInfos.Count - 1;
                    }
                }
            }

            m_totalSectors = m_csoInfo.IndexInfos.Count - 1;
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
                foreach (var slice in m_csoInfo.SliceInfos)
                {
                    slice.Stream.Dispose();
                }
            }
            m_disposed = true;
        }
    }
}
