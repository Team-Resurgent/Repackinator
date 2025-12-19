using System;
using System.IO;
using XboxToolkit.Interface;
using XboxToolkit.Internal.Models;

namespace XboxToolkit.Internal.Decoders
{
    internal class GODSectorDecoder : SectorDecoder
    {
        private GODDetails mGODDetails;
        private FileStream[] mFileStreams;
        private object mMutex;
        private bool mDisposed;

        public GODSectorDecoder(GODDetails godDetails)
        {
            mGODDetails = godDetails;
            mFileStreams = new FileStream[mGODDetails.DataFileCount];
            for (var i = 0; i < mGODDetails.DataFileCount; i++)
            {
                var filePath = Path.Combine(mGODDetails.DataPath, string.Format("Data{0:D4}", i));
                mFileStreams[i] = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            mMutex = new object();
            mDisposed = false;
        }

        private long SectorToAddress(long sector, out uint dataFileIndex)
        {
            if (sector == Constants.SVOD_START_SECTOR || sector == Constants.SVOD_START_SECTOR + 1)
            {
                dataFileIndex = 0;
                return mGODDetails.BaseAddress + (sector - Constants.SVOD_START_SECTOR) * Constants.XGD_SECTOR_SIZE;
            }

            var adjustedSector = sector - mGODDetails.StartingBlock * 2 + (mGODDetails.IsEnhancedGDF ? 2 : 0);
            dataFileIndex = (uint)(adjustedSector / 0x14388);
            if (dataFileIndex > mGODDetails.DataFileCount)
            {
                dataFileIndex = 0;
            }
            var dataSector = adjustedSector % 0x14388;
            var dataBlock = dataSector / 0x198;
            dataSector %= 0x198;

            var dataFileOffset = (dataSector + dataBlock * 0x198) * 0x800;
            dataFileOffset += 0x1000;
            dataFileOffset += dataBlock * 0x1000 + 0x1000;
            return dataFileOffset;
        }

        public override uint TotalSectors()
        {
            return mGODDetails.SectorCount;
        }

        public override bool TryReadSector(long sector, out byte[] sectorData)
        {
            var dataOffset = SectorToAddress(sector, out var dataFileIndex);
            if (dataOffset < 0)
            {
                sectorData = new byte[Constants.XGD_SECTOR_SIZE];
                return true;
            }

            sectorData = new byte[Constants.XGD_SECTOR_SIZE];
            lock (mMutex)
            {
                mFileStreams[dataFileIndex].Position = dataOffset;
                var bytesRead = mFileStreams[dataFileIndex].Read(sectorData, 0, (int)Constants.XGD_SECTOR_SIZE);
                return bytesRead == Constants.XGD_SECTOR_SIZE;
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
                    for (var i = 0; i < mGODDetails.DataFileCount; i++)
                    {
                        mFileStreams[i].Dispose();
                    }
                }
                mDisposed = true;
            }
        }
    }
}
