using System;
using XboxToolkit.Internal;
using XboxToolkit.Internal.Models;

namespace XboxToolkit.Interface
{
    public abstract class SectorDecoder : ISectorDecoder
    {
        public bool Init()
        {
            var found = false;
            var baseSector = 0U;

            var header = new XgdHeader();

            if (TotalSectors() >= Constants.XGD_MAGIC_SECTOR_XDKI)
            {
                if (TryReadSector(Constants.XGD_MAGIC_SECTOR_XDKI, out var sector) == true)
                {
                    header = Helpers.GetXgdHeaer(sector);
                    if (header != null && UnicodeHelper.GetUtf8String(header.Magic).Equals(Constants.XGD_IMAGE_MAGIC) && UnicodeHelper.GetUtf8String(header.MagicTail).Equals(Constants.XGD_IMAGE_MAGIC))
                    {
                        baseSector = Constants.XGD_MAGIC_SECTOR_XDKI - Constants.XGD_ISO_BASE_SECTOR;
                        found = true;
                    }
                }
            }

            if (found == false && TotalSectors() >= Constants.XGD_MAGIC_SECTOR_XGD1)
            {
                if (TryReadSector(Constants.XGD_MAGIC_SECTOR_XGD1, out var sector) == true)
                {
                    header = Helpers.GetXgdHeaer(sector);
                    if (header != null && UnicodeHelper.GetUtf8String(header.Magic).Equals(Constants.XGD_IMAGE_MAGIC) && UnicodeHelper.GetUtf8String(header.MagicTail).Equals(Constants.XGD_IMAGE_MAGIC))
                    {
                        baseSector = Constants.XGD_MAGIC_SECTOR_XGD1 - Constants.XGD_ISO_BASE_SECTOR;
                        found = true;
                    }
                }
            }

            if (found == false && TotalSectors() >= Constants.XGD_MAGIC_SECTOR_XGD3)
            {
                if (TryReadSector(Constants.XGD_MAGIC_SECTOR_XGD3, out var sector) == true)
                {
                    header = Helpers.GetXgdHeaer(sector);
                    if (header != null && UnicodeHelper.GetUtf8String(header.Magic).Equals(Constants.XGD_IMAGE_MAGIC) && UnicodeHelper.GetUtf8String(header.MagicTail).Equals(Constants.XGD_IMAGE_MAGIC))
                    {
                        baseSector = Constants.XGD_MAGIC_SECTOR_XGD3 - Constants.XGD_ISO_BASE_SECTOR;
                        found = true;
                    }
                }
            }

            if (found == false && TotalSectors() >= Constants.XGD_MAGIC_SECTOR_XGD2)
            {
                if (TryReadSector(Constants.XGD_MAGIC_SECTOR_XGD2, out var sector) == true)
                {
                    header = Helpers.GetXgdHeaer(sector);
                    if (header != null && UnicodeHelper.GetUtf8String(header.Magic).Equals(Constants.XGD_IMAGE_MAGIC) && UnicodeHelper.GetUtf8String(header.MagicTail).Equals(Constants.XGD_IMAGE_MAGIC))
                    {
                        baseSector = Constants.XGD_MAGIC_SECTOR_XGD2 - Constants.XGD_ISO_BASE_SECTOR;
                        found = true;
                    }
                }
            }

            if (found == true && header != null)
            {
                mXgdInfo = new XgdInfo
                {
                    BaseSector = baseSector,
                    RootDirSector = header.RootDirSector,
                    RootDirSize = header.RootDirSize,
                    CreationDateTime = DateTime.FromFileTime(header.CreationFileTime)
                };
                return true;
            }

            return false;
        }

        private XgdInfo? mXgdInfo;
        public XgdInfo GetXgdInfo()
        {
            if (mXgdInfo == null)
            {
                throw new Exception("Sector decoder nor initialized.");
            }
            return mXgdInfo;
        }

        public abstract uint TotalSectors();

        public uint SectorSize()
        {
            return Constants.XGD_SECTOR_SIZE;
        }

        public abstract bool TryReadSector(long sector, out byte[] sectorData);

        public virtual void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
