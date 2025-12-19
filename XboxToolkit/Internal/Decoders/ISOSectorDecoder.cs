using System;
using System.Linq;
using XboxToolkit.Interface;
using XboxToolkit.Internal.Models;

namespace XboxToolkit.Internal.Decoders
{
    internal class ISOSectorDecoder : SectorDecoder
    {
        private readonly ISODetail[] mISODetails;
        private readonly object mMutex;
        private bool mDisposed;

        public ISOSectorDecoder(ISODetail[] isoDetails)
        {
            mISODetails = isoDetails;
            mMutex = new object();
            mDisposed = false;
        }

        public override uint TotalSectors()
        {
            return (uint)(mISODetails.Max(s => s.EndSector) + 1);
        }

        public override bool TryReadSector(long sector, out byte[] sectorData)
        {
            sectorData = new byte[Constants.XGD_SECTOR_SIZE];
            lock (mMutex)
            {
                foreach (var isoDetail in mISODetails)
                {
                    if (sector >= isoDetail.StartSector && sector <= isoDetail.EndSector)
                    {
                        isoDetail.Stream.Position = (sector - isoDetail.StartSector) * Constants.XGD_SECTOR_SIZE;
                        var bytesRead = isoDetail.Stream.Read(sectorData, 0, (int)Constants.XGD_SECTOR_SIZE);
                        return bytesRead == Constants.XGD_SECTOR_SIZE;
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
                    foreach (var isoDetail in mISODetails)
                    {
                        isoDetail.Stream.Dispose();
                    }
                }
                mDisposed = true;
            }
        }
    }
}
