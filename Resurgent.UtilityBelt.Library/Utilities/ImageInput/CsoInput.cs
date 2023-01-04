using LibDeflate;
using System.Text;

namespace Resurgent.UtilityBelt.Library.Utilities.ImageInput
{
    public class CsoInput : IImageInput
    {
        private struct IndexInfo
        {
            public ulong Value { get; set; }

            public bool Compressed { get; set; }
        }

        private struct CsoSliceInfo
        {
            public Stream Stream { get; set; }

            public long StartSector { get; set; }

            public long EndSector { get; set; }

            public IndexInfo[] IndexInfos { get; set; }
        }

        private struct SectorCache
        {
            public long StartSector { get; set; }

            public long Count { get; set; }

            public byte[] SectorData { get; set;} 
        }

        private readonly List<CsoSliceInfo> m_slices;

        private bool m_disposed = false;

        public long m_cacheStartSector = 0;

        private byte[] m_cacheSectorData = Array.Empty<byte>();

        private long m_totalSectors = 0;
        public long TotalSectors => m_totalSectors;

        public long SectorOffset => m_totalSectors == Constants.RedumpSectors ? Constants.VideoSectors : 0U;

        public byte[] ReadSectors(long startSector, long count)
        {
            if (m_cacheStartSector == startSector && m_cacheSectorData.Length == count << 11)
            {
                return m_cacheSectorData;
            }

            var result = new byte[count << 11];
            var sectorOffset = 0;

            while (count > 0)
            {
                foreach (var slice in m_slices)
                {
                    if (count > 0 && startSector >= slice.StartSector && startSector <= slice.EndSector)
                    {
                        var position = slice.IndexInfos[startSector - slice.StartSector].Value;
                        var compressed = slice.IndexInfos[startSector - slice.StartSector].Compressed;
                        var size = (int)(slice.IndexInfos[startSector - slice.StartSector + 1].Value - position);

                        slice.Stream.Position = (long)position;

                        using var reader = new BinaryReader(slice.Stream, Encoding.Default, true);

                        if (compressed)
                        {
                            var outputBuffer = new byte[2048];
                            var buffer = reader.ReadBytes(size);
                            using (Decompressor decompressor = new DeflateDecompressor())
                            {
                                decompressor.Decompress(buffer, outputBuffer, out var bytesWritten, out var bytesRead);
                                if (bytesWritten == 0 || bytesRead == 0)
                                {
                                    throw new IndexOutOfRangeException("Unable to decompress sector.");
                                }
                            }
                            Array.Copy(outputBuffer, 0, result, sectorOffset << 11, 2048);
                        }
                        else
                        {
                            var temp = reader.ReadBytes(2048);
                            Array.Copy(temp, 0, result, sectorOffset << 11, 2048);
                        }

                        sectorOffset++;
                        count--;
                    }
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
            for (int i = 0; i < m_slices.Count; i++)
            {
                CsoSliceInfo slice = m_slices[i];
                if (sector >= slice.StartSector && sector <= slice.EndSector)
                {
                    return i;
                }
            }
            throw new IndexOutOfRangeException();
        }

        public CsoInput(string[] parts)
        {
            var totalSectors = 0L;
            var startSector = 0L;
            m_slices = new List<CsoSliceInfo>();
            foreach (var part in parts)
            {
                var stream = new FileStream(part, FileMode.Open, FileAccess.Read);

                using var reader = new BinaryReader(stream, Encoding.Default, true);

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

                ulong indexOffset = 24;

                uint blockSize = reader.ReadUInt32();
                if (blockSize != 2048)
                {
                    throw new IOException("Invalid block size in cso header."); 
                }

                byte version = reader.ReadByte();
                if (version != 1)
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

                var indexInfos = new List<IndexInfo>();
                for (var i = 0; i <= entries; i++)
                {
                    var index = reader.ReadUInt32();
                    indexInfos.Add(new IndexInfo
                    {
                        Value = (ulong)(index & 0x7FFFFFFF) << indexAlignment,
                        Compressed = (index & 0x80000000) == 0
                    });
                }

                var sectorCount = entries;
                m_slices.Add(new CsoSliceInfo
                {
                    Stream = stream,
                    StartSector = startSector,
                    EndSector = startSector + sectorCount - 1,
                    IndexInfos = indexInfos.ToArray()
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
