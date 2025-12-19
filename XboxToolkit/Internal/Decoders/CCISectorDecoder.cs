using System;
using System.Linq;
using XboxToolkit.Interface;
using XboxToolkit.Internal.Models;

namespace XboxToolkit.Internal.Decoders
{
    internal class CCISectorDecoder : SectorDecoder
    {
        private readonly CCIDetail[] mCCIDetails;
        private readonly object mMutex;
        private bool mDisposed;

        public CCISectorDecoder(CCIDetail[] cciDetails)
        {
            mCCIDetails = cciDetails;
            mMutex = new object();
            mDisposed = false;
        }

        public override uint TotalSectors()
        {
            return (uint)(mCCIDetails.Max(s => s.EndSector) + 1);
        }

        public override bool TryReadSector(long sector, out byte[] sectorData)
        {
            sectorData = new byte[Constants.XGD_SECTOR_SIZE];
            lock (mMutex)
            {
                foreach (var cciDetail in mCCIDetails)
                {
                    if (sector >= cciDetail.StartSector && sector <= cciDetail.EndSector)
                    {
                        var sectorOffset = sector - cciDetail.StartSector;
                        var position = cciDetail.IndexInfo[sectorOffset].Value;
                        var LZ4Compressed = cciDetail.IndexInfo[sectorOffset].LZ4Compressed;
                        var size = (int)(cciDetail.IndexInfo[sectorOffset + 1].Value - position);

                        cciDetail.Stream.Position = (long)position;
                        if (size != Constants.XGD_SECTOR_SIZE || LZ4Compressed)
                        {
                            var padding = cciDetail.Stream.ReadByte();
                            var decompressBuffer = new byte[size];
                            var decompressBytesRead = cciDetail.Stream.Read(decompressBuffer, 0, size);
                            if (decompressBytesRead != size)
                            {
                                sectorData = Array.Empty<byte>();
                                return false;
                            }
                            var decodeBuffer = new byte[Constants.XGD_SECTOR_SIZE];
                            var decompressedSize = K4os.Compression.LZ4.LZ4Codec.Decode(decompressBuffer, 0, size - (padding + 1), decodeBuffer, 0, (int)Constants.XGD_SECTOR_SIZE);
                            if (decompressedSize < 0)
                            {
                                sectorData = Array.Empty<byte>();
                                return false;
                            }
                            sectorData = decodeBuffer;
                            return true;
                        }

                        sectorData = new byte[Constants.XGD_SECTOR_SIZE];
                        var sectorBytesRead = cciDetail.Stream.Read(sectorData, 0, (int)Constants.XGD_SECTOR_SIZE);
                        return sectorBytesRead == Constants.XGD_SECTOR_SIZE;
                    }
                }
                return false;
            }
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (mDisposed == false)
            {
                if (disposing)
                {
                    foreach (var cciDetail in mCCIDetails)
                    {
                        cciDetail.Stream.Dispose();
                    }
                }
                mDisposed = true;
            }
        }
    }
}
