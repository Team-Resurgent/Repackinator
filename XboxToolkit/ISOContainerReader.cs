using System;
using System.Collections.Generic;
using System.IO;
using XboxToolkit.Interface;
using XboxToolkit.Internal;
using XboxToolkit.Internal.Decoders;
using XboxToolkit.Internal.Models;

namespace XboxToolkit
{
    public class ISOContainerReader : ContainerReader, IDisposable
    {
        private string mFilePath;
        private int mMountCount;
        private SectorDecoder? mSectorDecoder;
        private bool mDisposed;

        public ISOContainerReader(string filePath)
        {
            mFilePath = filePath;
            mMountCount = 0;
        }

        public override SectorDecoder GetDecoder()
        {
            if (mSectorDecoder == null)
            {
                throw new Exception("Container not mounted.");
            }
            return mSectorDecoder;
        }

        public static bool IsISO(string filePath)
        {
            if (File.Exists(filePath) == false)
            {
                return false;
            }
            return Path.GetExtension(filePath).Equals(".iso", StringComparison.CurrentCultureIgnoreCase);
        }

        public override bool TryMount()
        {
            try
            {
                if (mMountCount > 0)
                {
                    mMountCount++;
                    return true;
                }

                if (IsISO(mFilePath) == false)
                {
                    return false;
                }

                var fileSlices = ContainerUtility.GetSlicesFromFile(mFilePath);

                var isoDetails = new List<ISODetail>();

                var sectorCount = 0L;
                foreach (var fileSlice in fileSlices)
                {
                    var stream = new FileStream(fileSlice, FileMode.Open, FileAccess.Read, FileShare.Read);
                    var sectors = stream.Length / Constants.XGD_SECTOR_SIZE;
                    var isoDetail = new ISODetail()
                    {
                        Stream = stream,
                        StartSector = sectorCount,
                        EndSector = sectorCount + sectors - 1
                    };
                    isoDetails.Add(isoDetail);
                    sectorCount += sectors;
                }

                mSectorDecoder = new ISOSectorDecoder(isoDetails.ToArray());
                if (mSectorDecoder.Init() == false)
                {
                    return false;
                }

                mMountCount++;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.ToString());
                return false;
            }
        }

        public override void Dismount()
        {
            if (mMountCount == 0)
            {
                return;
            }
            mMountCount--;
        }

        public override int GetMountCount()
        {
            return mMountCount;
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
                    mSectorDecoder?.Dispose();
                }
                mDisposed = true;
            }
        }
    }
}
