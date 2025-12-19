using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using XboxToolkit.Internal.Models;
using XboxToolkit.Internal;

namespace XboxToolkit.Interface
{
    public abstract class ContainerReader : IContainerReader, IDisposable
    {
        public abstract SectorDecoder GetDecoder();

        public abstract bool TryMount();

        public abstract void Dismount();

        public abstract int GetMountCount();

        public bool TryExtractFiles(string destFilePath)
        {
            try
            {
                Directory.CreateDirectory(destFilePath);

                var decoder = GetDecoder();
                var xgdInfo = decoder.GetXgdInfo();

                var rootSectors = xgdInfo.RootDirSize / Constants.XGD_SECTOR_SIZE;
                var rootData = new byte[xgdInfo.RootDirSize];
                for (var i = 0; i < rootSectors; i++)
                {
                    var currentRootSector = xgdInfo.BaseSector + xgdInfo.RootDirSector + (uint)i;
                    if (decoder.TryReadSector(currentRootSector, out var sectorData) == false)
                    {
                        return false;
                    }
                    Array.Copy(sectorData, 0, rootData, i * Constants.XGD_SECTOR_SIZE, Constants.XGD_SECTOR_SIZE);
                }

                var treeNodes = new List<TreeNodeInfo>
            {
                new TreeNodeInfo
                {
                    DirectoryData = rootData,
                    Offset = 0,
                    Path = string.Empty
                }
            };

                while (treeNodes.Count > 0)
                {
                    var currentTreeNode = treeNodes[0];
                    treeNodes.RemoveAt(0);

                    using (var directoryDataStream = new MemoryStream(currentTreeNode.DirectoryData))
                    using (var directoryDataDataReader = new BinaryReader(directoryDataStream))
                    {

                        if (currentTreeNode.Offset * 4 >= directoryDataStream.Length)
                        {
                            continue;
                        }

                        directoryDataStream.Position = currentTreeNode.Offset * 4;

                        var left = directoryDataDataReader.ReadUInt16();
                        var right = directoryDataDataReader.ReadUInt16();
                        var sector = directoryDataDataReader.ReadUInt32();
                        var size = directoryDataDataReader.ReadUInt32();
                        var attribute = directoryDataDataReader.ReadByte();
                        var nameLength = directoryDataDataReader.ReadByte();
                        var filenameBytes = directoryDataDataReader.ReadBytes(nameLength);
                        var filename = Encoding.ASCII.GetString(filenameBytes);

                        var path = Path.Combine(destFilePath, currentTreeNode.Path, filename);
                        if ((attribute & 0x10) != 0)
                        {
                            Directory.CreateDirectory(path);
                        }
                        else
                        {
                            using (var fileStream = File.OpenWrite(path))
                            {
                                var readSector = sector + xgdInfo.BaseSector;
                                var result = new byte[size];
                                var processed = 0U;
                                if (size > 0)
                                {
                                    while (processed < size)
                                    {
                                        if (decoder.TryReadSector(readSector, out var buffer) == false)
                                        {
                                            return false;
                                        }
                                        var bytesToSave = Math.Min(size - processed, 2048);
                                        fileStream.Write(buffer, 0, (int)bytesToSave);
                                        readSector++;
                                        processed += bytesToSave;
                                    }
                                }
                            }
                        }

                        // Process left child in binary tree (if exists)
                        if (left != 0 && left != 0xFFFF)
                        {
                            treeNodes.Add(new TreeNodeInfo
                            {
                                DirectoryData = currentTreeNode.DirectoryData,
                                Offset = left,
                                Path = currentTreeNode.Path
                            });
                        }

                        // If this is a directory entry, read its directory data and add to treeNodes for traversal
                        // This must happen even if left == 0xFFFF, because left/right are for binary tree structure,
                        // not for indicating whether this directory has subdirectories
                        if ((attribute & 0x10) != 0 && size > 0)
                        {
                            var directorySectors = size / Constants.XGD_SECTOR_SIZE;
                            var directoryData = new byte[size];
                            for (var i = 0; i < directorySectors; i++)
                            {
                                var currentDirectorySector = xgdInfo.BaseSector + sector + (uint)i;
                                if (decoder.TryReadSector(currentDirectorySector, out var sectorData) == false)
                                {
                                    return false;
                                }
                                Array.Copy(sectorData, 0, directoryData, i * Constants.XGD_SECTOR_SIZE, Constants.XGD_SECTOR_SIZE);
                            }

                            treeNodes.Add(new TreeNodeInfo
                            {
                                DirectoryData = directoryData,
                                Offset = 0,
                                Path = Path.Combine(currentTreeNode.Path, filename)
                            });
                        }

                        // Process right child in binary tree (if exists)
                        if (right != 0 && right != 0xFFFF)
                        {
                            treeNodes.Add(new TreeNodeInfo
                            {
                                DirectoryData = currentTreeNode.DirectoryData,
                                Offset = right,
                                Path = currentTreeNode.Path
                            });
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.ToString());
                return false;
            }
        }

        public bool TryGetDataSectors(out HashSet<uint> dataSectors)
        {
            dataSectors = new HashSet<uint>();

            try
            {
                var decoder = GetDecoder();
                var xgdInfo = decoder.GetXgdInfo();

                dataSectors.Add(xgdInfo.BaseSector + Constants.XGD_ISO_BASE_SECTOR);
                dataSectors.Add(xgdInfo.BaseSector + Constants.XGD_ISO_BASE_SECTOR + 1);

                var rootSectors = xgdInfo.RootDirSize / Constants.XGD_SECTOR_SIZE;
                var rootData = new byte[xgdInfo.RootDirSize];
                for (var i = 0; i < rootSectors; i++)
                {
                    var currentRootSector = xgdInfo.BaseSector + xgdInfo.RootDirSector + (uint)i;
                    dataSectors.Add(currentRootSector);
                    if (decoder.TryReadSector(currentRootSector, out var sectorData) == false)
                    {
                        return false;
                    }
                    Array.Copy(sectorData, 0, rootData, i * Constants.XGD_SECTOR_SIZE, Constants.XGD_SECTOR_SIZE);
                }

                var treeNodes = new List<TreeNodeInfo>
            {
                new TreeNodeInfo
                {
                    DirectoryData = rootData,
                    Offset = 0,
                    Path = string.Empty
                }
            };

                while (treeNodes.Count > 0)
                {
                    var currentTreeNode = treeNodes[0];
                    treeNodes.RemoveAt(0);

                    using (var directoryDataStream = new MemoryStream(currentTreeNode.DirectoryData))
                    using (var directoryDataDataReader = new BinaryReader(directoryDataStream))
                    {

                        if (currentTreeNode.Offset * 4 >= directoryDataStream.Length)
                        {
                            continue;
                        }

                        directoryDataStream.Position = currentTreeNode.Offset * 4;

                        var left = directoryDataDataReader.ReadUInt16();
                        var right = directoryDataDataReader.ReadUInt16();
                        var sector = directoryDataDataReader.ReadUInt32();
                        var size = directoryDataDataReader.ReadUInt32();
                        var attribute = directoryDataDataReader.ReadByte();
                        var nameLength = directoryDataDataReader.ReadByte();
                        var filenameBytes = directoryDataDataReader.ReadBytes(nameLength);
                        var filename = Encoding.ASCII.GetString(filenameBytes);

                        if (left == 0xFFFF)
                        {
                            continue;
                        }

                        if (left != 0)
                        {
                            treeNodes.Add(new TreeNodeInfo
                            {
                                DirectoryData = currentTreeNode.DirectoryData,
                                Offset = left,
                                Path = currentTreeNode.Path
                            });
                        }

                        if (size > 0)
                        {

                            if ((attribute & 0x10) != 0)
                            {
                                var directorySectors = size / Constants.XGD_SECTOR_SIZE;
                                var directoryData = new byte[size];
                                for (var i = 0; i < directorySectors; i++)
                                {
                                    var currentDirectorySector = xgdInfo.BaseSector + sector + (uint)i;
                                    dataSectors.Add(currentDirectorySector);
                                    if (decoder.TryReadSector(currentDirectorySector, out var sectorData) == false)
                                    {
                                        return false;
                                    }
                                    Array.Copy(sectorData, 0, directoryData, i * Constants.XGD_SECTOR_SIZE, Constants.XGD_SECTOR_SIZE);
                                }

                                treeNodes.Add(new TreeNodeInfo
                                {
                                    DirectoryData = directoryData,
                                    Offset = 0,
                                    Path = Path.Combine(currentTreeNode.Path, filename)
                                });
                            }
                            else
                            {
                                var fileSectors = Helpers.RoundToMultiple(size, Constants.XGD_SECTOR_SIZE) / Constants.XGD_SECTOR_SIZE;
                                for (var i = 0; i < fileSectors; i++)
                                {
                                    var currentFileSector = xgdInfo.BaseSector + sector + (uint)i;
                                    dataSectors.Add(currentFileSector);
                                }
                            }
                        }

                        if (right != 0)
                        {
                            treeNodes.Add(new TreeNodeInfo
                            {
                                DirectoryData = currentTreeNode.DirectoryData,
                                Offset = right,
                                Path = currentTreeNode.Path
                            });
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.ToString());
                return false;
            }
        }

        public bool TryGetDefault(out byte[] defaultData, out ContainerType containerType)
        {
            defaultData = Array.Empty<byte>();
            containerType = ContainerType.Unknown;

            try
            {
                var decoder = GetDecoder();
                var xgdInfo = decoder.GetXgdInfo();

                var rootSectors = xgdInfo.RootDirSize / Constants.XGD_SECTOR_SIZE;
                var rootData = new byte[xgdInfo.RootDirSize];
                for (var i = 0; i < rootSectors; i++)
                {
                    if (decoder.TryReadSector(xgdInfo.BaseSector + xgdInfo.RootDirSector + (uint)i, out var sectorData) == false)
                    {
                        return false;
                    }
                    Array.Copy(sectorData, 0, rootData, i * Constants.XGD_SECTOR_SIZE, Constants.XGD_SECTOR_SIZE);
                }

                var treeNodes = new List<TreeNodeInfo>
                {
                    new TreeNodeInfo
                    {
                        DirectoryData = rootData,
                        Offset = 0,
                        Path = string.Empty
                    }
                };

                while (treeNodes.Count > 0)
                {
                    var currentTreeNode = treeNodes[0];
                    treeNodes.RemoveAt(0);

                    using (var directoryDataStream = new MemoryStream(currentTreeNode.DirectoryData))
                    using (var directoryDataDataReader = new BinaryReader(directoryDataStream))
                    {

                        if (currentTreeNode.Offset * 4 >= directoryDataStream.Length)
                        {
                            continue;
                        }

                        directoryDataStream.Position = currentTreeNode.Offset * 4;

                        var left = directoryDataDataReader.ReadUInt16();
                        var right = directoryDataDataReader.ReadUInt16();
                        var sector = directoryDataDataReader.ReadUInt32();
                        var size = directoryDataDataReader.ReadUInt32();
                        var attribute = directoryDataDataReader.ReadByte();
                        var nameLength = directoryDataDataReader.ReadByte();
                        var filenameBytes = directoryDataDataReader.ReadBytes(nameLength);

                        var filename = Encoding.ASCII.GetString(filenameBytes);
                        var isXbe = filename.Equals(Constants.XBE_FILE_NAME, StringComparison.CurrentCultureIgnoreCase);
                        var isXex = filename.Equals(Constants.XEX_FILE_NAME, StringComparison.CurrentCultureIgnoreCase);
                        if (isXbe || isXex)
                        {
                            containerType = isXbe ? ContainerType.XboxOriginal : ContainerType.Xbox360;

                            var readSector = sector + xgdInfo.BaseSector;
                            var result = new byte[size];
                            var processed = 0U;
                            if (size > 0)
                            {
                                while (processed < size)
                                {
                                    if (decoder.TryReadSector(readSector, out var buffer) == false)
                                    {
                                        return false;
                                    }
                                    var bytesToCopy = Math.Min(size - processed, 2048);
                                    Array.Copy(buffer, 0, result, processed, bytesToCopy);
                                    readSector++;
                                    processed += bytesToCopy;
                                }
                            }
                            defaultData = result;
                            return true;
                        }

                        if (left == 0xFFFF)
                        {
                            continue;
                        }

                        if (left != 0)
                        {
                            treeNodes.Add(new TreeNodeInfo
                            {
                                DirectoryData = currentTreeNode.DirectoryData,
                                Offset = left,
                                Path = currentTreeNode.Path
                            });
                        }

                        if (right != 0)
                        {
                            treeNodes.Add(new TreeNodeInfo
                            {
                                DirectoryData = currentTreeNode.DirectoryData,
                                Offset = right,
                                Path = currentTreeNode.Path
                            });
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.ToString());
                return false;
            }
        }

        public virtual void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
