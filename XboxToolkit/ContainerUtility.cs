using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using XboxToolkit.Interface;
using XboxToolkit.Internal;
using XboxToolkit.Internal.Models;
using XboxToolkit.Internal.ContainerBuilder;

namespace XboxToolkit
{
    public static class ContainerUtility
    {
        public static string[] GetSlicesFromFile(string filename)
        {
            var slices = new List<string>();
            var extension = Path.GetExtension(filename);
            var fileWithoutExtension = Path.GetFileNameWithoutExtension(filename);
            var subExtension = Path.GetExtension(fileWithoutExtension);
            if (subExtension?.Length == 2 && char.IsNumber(subExtension[1]))
            {
                var fileWithoutSubExtension = Path.GetFileNameWithoutExtension(fileWithoutExtension);
                return Directory.GetFiles(Path.GetDirectoryName(filename) ?? "", $"{fileWithoutSubExtension}.?{extension}").OrderBy(s => s).ToArray();
            }
            return new string[] { filename };
        }

        public static bool TryAutoDetectContainerType(string filePath, out ContainerReader? containerReader)
        {
            if (ISOContainerReader.IsISO(filePath))
            {
                containerReader = new ISOContainerReader(filePath);
                return true;
            }
            else if (CCIContainerReader.IsCCI(filePath))
            {
                containerReader = new CCIContainerReader(filePath);
                return true;
            }
            else if (GODContainerReader.IsGOD(filePath))
            {
                containerReader = new GODContainerReader(filePath);
                return true;
            }
 
            containerReader = null;
            return false;
        }

        public static bool ExtractFilesFromContainer(ContainerReader containerReader, string destFilePath)
        {
            if (containerReader.GetMountCount() == 0)
            {
                return false;
            }

            if (containerReader.TryExtractFiles(destFilePath) == false)
            {
                return false;
            }

            return true;
        }

        public static bool ConvertFolderToISO(string inputFolder, ISOFormat format, string outputFile, long splitPoint, Action<float>? progress)
        {
            try
            {
                var isoFormat = format;

                if (Directory.Exists(inputFolder) == false)
                {
                    return false;
                }

                progress?.Invoke(0.0f);

                // Determine magic sector based on format
                uint magicSector;
                uint baseSector;
                if (isoFormat == ISOFormat.XboxOriginal)
                {
                    magicSector = Constants.XGD_ISO_BASE_SECTOR;
                    baseSector = 0;
                }
                else // Xbox360
                {
                    // Use XGD3 as default (most common)
                    magicSector = Constants.XGD_MAGIC_SECTOR_XGD3;
                    baseSector = magicSector - Constants.XGD_ISO_BASE_SECTOR;
                }

                // Scan folder structure
                var fileEntries = new List<FileEntry>();
                var directoryEntries = new List<DirectoryEntry>();
                ContainerBuilderHelper.ScanFolder(inputFolder, string.Empty, fileEntries, directoryEntries);

                progress?.Invoke(0.1f);

                // Build directory tree structure
                var rootDirectory = ContainerBuilderHelper.BuildDirectoryTree(directoryEntries, fileEntries, string.Empty);
                
                // Calculate directory sizes
                var directorySizes = new Dictionary<string, uint>();
                ContainerBuilderHelper.CalculateDirectorySizes(rootDirectory, directorySizes, Constants.XGD_SECTOR_SIZE);

                progress?.Invoke(0.2f);

                // Allocate sectors efficiently - maximize usage between magic sector and directory tables
                var sectorAllocator = new SectorAllocator(magicSector, baseSector);
                
                // Allocate directories FIRST (they need to be in a known location after magic sector)
                var rootDirSize = directorySizes[string.Empty];
                var rootDirSectors = Helpers.RoundToMultiple(rootDirSize, Constants.XGD_SECTOR_SIZE) / Constants.XGD_SECTOR_SIZE;
                var rootDirSector = sectorAllocator.AllocateDirectorySectors(rootDirSectors);
                
                // Set root directory sector (relative to baseSector)
                rootDirectory.Sector = rootDirSector - baseSector;

                // Allocate sectors for all subdirectories
                ContainerBuilderHelper.AllocateDirectorySectors(rootDirectory, directorySizes, sectorAllocator, baseSector);
                
                // Allocate sectors for files - try to fill space between base and magic sector first, then after directories
                // Sort files by size (largest first) to better fill sectors
                var sortedFiles = fileEntries.OrderByDescending(f => f.Size).ToList();
                uint totalFileSectorsAllocated = 0;
                foreach (var fileEntry in sortedFiles)
                {
                    var fileSectors = Helpers.RoundToMultiple(fileEntry.Size, Constants.XGD_SECTOR_SIZE) / Constants.XGD_SECTOR_SIZE;
                    var allocatedSector = sectorAllocator.AllocateFileSectors(fileSectors);
                    fileEntry.Sector = allocatedSector - baseSector;
                    totalFileSectorsAllocated += fileSectors;
                }

                progress?.Invoke(0.4f);

                // Build directory data
                var directoryData = new Dictionary<string, byte[]>();
                ContainerBuilderHelper.BuildDirectoryData(rootDirectory, fileEntries, directorySizes, directoryData, baseSector);

                progress?.Invoke(0.6f);

                // Write ISO
                // Calculate total sectors needed - must be at least magicSector + 1, and include all allocated sectors
                var allocatedSectors = sectorAllocator.GetTotalSectors();
                var totalSectors = Math.Max(allocatedSectors, magicSector + 1);
                
                
                var iteration = 0;
                var currentSector = 0u;

                while (currentSector < totalSectors)
                {
                    var splitting = false;
                    using (var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                    using (var outputWriter = new BinaryWriter(outputStream))
                    {
                        // Build sector map for directory tables and files
                        var sectorMap = new Dictionary<uint, byte[]>();
                        
                        // Add directory sectors to map
                        foreach (var dir in directoryData)
                        {
                            var dirSectors = Helpers.RoundToMultiple((uint)dir.Value.Length, Constants.XGD_SECTOR_SIZE) / Constants.XGD_SECTOR_SIZE;
                            var dirSector = dir.Key == string.Empty ? rootDirSector : ContainerBuilderHelper.GetDirectorySector(dir.Key, rootDirectory, baseSector);
                            
                            for (var i = 0u; i < dirSectors; i++)
                            {
                                var sectorIndex = dirSector + i;
                                var offset = (int)(i * Constants.XGD_SECTOR_SIZE);
                                var length = Math.Min(Constants.XGD_SECTOR_SIZE, dir.Value.Length - offset);
                                var sectorData = new byte[Constants.XGD_SECTOR_SIZE];
                                if (length > 0)
                                {
                                    Array.Copy(dir.Value, offset, sectorData, 0, length);
                                }
                                sectorMap[sectorIndex] = sectorData;
                            }
                        }

                        // Add file sectors to map
                        foreach (var fileEntry in fileEntries)
                        {
                            var fileSector = fileEntry.Sector + baseSector;
                            var fileSectors = Helpers.RoundToMultiple(fileEntry.Size, Constants.XGD_SECTOR_SIZE) / Constants.XGD_SECTOR_SIZE;
                            
                            using (var fileStream = File.OpenRead(fileEntry.FullPath))
                            {
                                for (var i = 0u; i < fileSectors; i++)
                                {
                                    var sectorIndex = fileSector + i;
                                    var sectorData = new byte[Constants.XGD_SECTOR_SIZE];
                                    var bytesRead = fileStream.Read(sectorData, 0, (int)Constants.XGD_SECTOR_SIZE);
                                    if (bytesRead < Constants.XGD_SECTOR_SIZE)
                                    {
                                        Helpers.FillArray(sectorData, (byte)0, bytesRead, (int)(Constants.XGD_SECTOR_SIZE - bytesRead));
                                    }
                                    sectorMap[sectorIndex] = sectorData;
                                }
                            }
                        }

                        // Write all sectors in order from 0 to totalSectors
                        uint sectorsWritten = 0;
                        uint sectorsWithData = 0;
                        while (currentSector < totalSectors)
                        {
                            var estimatedSize = outputStream.Position + Constants.XGD_SECTOR_SIZE;
                            if (splitPoint > 0 && estimatedSize > splitPoint)
                            {
                                splitting = true;
                                break;
                            }

                            byte[] sectorToWrite;
                            if (currentSector == magicSector)
                            {
                                // Write magic sector with XGD header
                                sectorToWrite = new byte[Constants.XGD_SECTOR_SIZE];
                                ContainerBuilderHelper.WriteXgdHeader(sectorToWrite, rootDirSector - baseSector, rootDirSize);
                                sectorsWithData++;
                            }
                            else if (currentSector < baseSector)
                            {
                                // Scrubbed sector before base (0xFF filled)
                                sectorToWrite = new byte[Constants.XGD_SECTOR_SIZE];
                                Helpers.FillArray(sectorToWrite, (byte)0xff);
                            }
                            else if (sectorMap.ContainsKey(currentSector))
                            {
                                // Sector with file or directory data
                                sectorToWrite = sectorMap[currentSector];
                                sectorsWithData++;
                            }
                            else
                            {
                                // Scrubbed sector (0xFF filled)
                                sectorToWrite = new byte[Constants.XGD_SECTOR_SIZE];
                                Helpers.FillArray(sectorToWrite, (byte)0xff);
                            }

                            outputWriter.Write(sectorToWrite);
                            currentSector++;
                            sectorsWritten++;

                            var currentProgress = 0.6f + 0.4f * (currentSector / (float)totalSectors);
                            progress?.Invoke(currentProgress);
                        }
                    }

                    if (splitting || iteration > 0)
                    {
                        var destFile = Path.Combine(Path.GetDirectoryName(outputFile) ?? "", Path.GetFileNameWithoutExtension(outputFile) + $".{iteration + 1}{Path.GetExtension(outputFile)}");
                        if (File.Exists(destFile))
                        {
                            File.Delete(destFile);
                        }
                        File.Move(outputFile, destFile);
                    }

                    iteration++;
                }

                progress?.Invoke(1.0f);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.ToString());
                return false;
            }
        }

        public static bool ConvertFolderToCCI(string inputFolder, ISOFormat format, string outputFile, long splitPoint, Action<float>? progress)
        {
            try
            {
                var isoFormat = format;

                if (Directory.Exists(inputFolder) == false)
                {
                    return false;
                }

                progress?.Invoke(0.0f);

                // Determine magic sector based on format
                uint magicSector;
                uint baseSector;
                if (isoFormat == ISOFormat.XboxOriginal)
                {
                    magicSector = Constants.XGD_ISO_BASE_SECTOR;
                    baseSector = 0;
                }
                else // Xbox360
                {
                    // Use XGD3 as default (most common)
                    magicSector = Constants.XGD_MAGIC_SECTOR_XGD3;
                    baseSector = magicSector - Constants.XGD_ISO_BASE_SECTOR;
                }

                // Scan folder structure
                var fileEntries = new List<FileEntry>();
                var directoryEntries = new List<DirectoryEntry>();
                ContainerBuilderHelper.ScanFolder(inputFolder, string.Empty, fileEntries, directoryEntries);

                progress?.Invoke(0.1f);

                // Build directory tree structure
                var rootDirectory = ContainerBuilderHelper.BuildDirectoryTree(directoryEntries, fileEntries, string.Empty);
                
                // Calculate directory sizes
                var directorySizes = new Dictionary<string, uint>();
                ContainerBuilderHelper.CalculateDirectorySizes(rootDirectory, directorySizes, Constants.XGD_SECTOR_SIZE);

                progress?.Invoke(0.2f);

                // Allocate sectors efficiently - maximize usage between magic sector and directory tables
                var sectorAllocator = new SectorAllocator(magicSector, baseSector);
                
                // Allocate directories FIRST (they need to be in a known location after magic sector)
                var rootDirSize = directorySizes[string.Empty];
                var rootDirSectors = Helpers.RoundToMultiple(rootDirSize, Constants.XGD_SECTOR_SIZE) / Constants.XGD_SECTOR_SIZE;
                var rootDirSector = sectorAllocator.AllocateDirectorySectors(rootDirSectors);
                
                // Set root directory sector (relative to baseSector)
                rootDirectory.Sector = rootDirSector - baseSector;

                // Allocate sectors for all subdirectories
                ContainerBuilderHelper.AllocateDirectorySectors(rootDirectory, directorySizes, sectorAllocator, baseSector);
                
                // Allocate sectors for files - try to fill space between base and magic sector first, then after directories
                // Sort files by size (largest first) to better fill sectors
                var sortedFiles = fileEntries.OrderByDescending(f => f.Size).ToList();
                uint totalFileSectorsAllocated = 0;
                foreach (var fileEntry in sortedFiles)
                {
                    var fileSectors = Helpers.RoundToMultiple(fileEntry.Size, Constants.XGD_SECTOR_SIZE) / Constants.XGD_SECTOR_SIZE;
                    var allocatedSector = sectorAllocator.AllocateFileSectors(fileSectors);
                    fileEntry.Sector = allocatedSector - baseSector;
                    totalFileSectorsAllocated += fileSectors;
                }

                progress?.Invoke(0.4f);

                // Build directory data
                var directoryData = new Dictionary<string, byte[]>();
                ContainerBuilderHelper.BuildDirectoryData(rootDirectory, fileEntries, directorySizes, directoryData, baseSector);

                progress?.Invoke(0.6f);

                // Write CCI
                var totalSectors = sectorAllocator.GetTotalSectors();
                var iteration = 0;
                var currentSector = 0u;

                while (currentSector < totalSectors)
                {
                    var cciIndices = new List<CCIIndex>();
                    var splitting = false;
                    var compressedData = new byte[Constants.XGD_SECTOR_SIZE];
                    var indexAlignment = 2;
                    var multiple = (1 << indexAlignment);

                    using (var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                    using (var outputWriter = new BinaryWriter(outputStream))
                    {
                        // Build sector map for directory tables and files
                        var sectorMap = new Dictionary<uint, byte[]>();
                        
                        // Add directory sectors to map
                        foreach (var dir in directoryData)
                        {
                            var dirSectors = Helpers.RoundToMultiple((uint)dir.Value.Length, Constants.XGD_SECTOR_SIZE) / Constants.XGD_SECTOR_SIZE;
                            var dirSector = dir.Key == string.Empty ? rootDirSector : ContainerBuilderHelper.GetDirectorySector(dir.Key, rootDirectory, baseSector);
                            
                            for (var i = 0u; i < dirSectors; i++)
                            {
                                var sectorIndex = dirSector + i;
                                var offset = (int)(i * Constants.XGD_SECTOR_SIZE);
                                var length = Math.Min(Constants.XGD_SECTOR_SIZE, dir.Value.Length - offset);
                                var sectorData = new byte[Constants.XGD_SECTOR_SIZE];
                                if (length > 0)
                                {
                                    Array.Copy(dir.Value, offset, sectorData, 0, length);
                                }
                                sectorMap[sectorIndex] = sectorData;
                            }
                        }

                        // Add file sectors to map
                        foreach (var fileEntry in fileEntries)
                        {
                            var fileSector = fileEntry.Sector + baseSector;
                            var fileSectors = Helpers.RoundToMultiple(fileEntry.Size, Constants.XGD_SECTOR_SIZE) / Constants.XGD_SECTOR_SIZE;
                            
                            using (var fileStream = File.OpenRead(fileEntry.FullPath))
                            {
                                for (var i = 0u; i < fileSectors; i++)
                                {
                                    var sectorIndex = fileSector + i;
                                    var sectorData = new byte[Constants.XGD_SECTOR_SIZE];
                                    var bytesRead = fileStream.Read(sectorData, 0, (int)Constants.XGD_SECTOR_SIZE);
                                    if (bytesRead < Constants.XGD_SECTOR_SIZE)
                                    {
                                        Helpers.FillArray(sectorData, (byte)0, bytesRead, (int)(Constants.XGD_SECTOR_SIZE - bytesRead));
                                    }
                                    sectorMap[sectorIndex] = sectorData;
                                }
                            }
                        }

                        // Write CCI header (placeholder values, will update later)
                        uint header = 0x4D494343U;
                        outputWriter.Write(header);

                        uint headerSize = 32;
                        outputWriter.Write(headerSize);

                        ulong uncompressedSize = 0UL;
                        outputWriter.Write(uncompressedSize);

                        ulong indexOffset = 0UL;
                        outputWriter.Write(indexOffset);

                        uint blockSize = 2048;
                        outputWriter.Write(blockSize);

                        byte version = 1;
                        outputWriter.Write(version);

                        outputWriter.Write((byte)indexAlignment);

                        ushort unused = 0;
                        outputWriter.Write(unused);

                        // Write all sectors in order with compression
                        while (currentSector < totalSectors)
                        {
                            var currentIndexSize = cciIndices.Count * 4;
                            var estimatedSize = outputStream.Position + currentIndexSize + Constants.XGD_SECTOR_SIZE;
                            if (splitPoint > 0 && estimatedSize > splitPoint)
                            {
                                splitting = true;
                                break;
                            }

                            byte[] sectorToWrite;
                            if (currentSector == magicSector)
                            {
                                // Write magic sector with XGD header
                                sectorToWrite = new byte[Constants.XGD_SECTOR_SIZE];
                                ContainerBuilderHelper.WriteXgdHeader(sectorToWrite, rootDirSector - baseSector, rootDirSize);
                            }
                            else if (currentSector < baseSector)
                            {
                                // Scrubbed sector before base (0xFF filled)
                                sectorToWrite = new byte[Constants.XGD_SECTOR_SIZE];
                                Helpers.FillArray(sectorToWrite, (byte)0xff);
                            }
                            else if (sectorMap.ContainsKey(currentSector))
                            {
                                // Sector with file or directory data
                                sectorToWrite = sectorMap[currentSector];
                            }
                            else
                            {
                                // Scrubbed sector (0xFF filled)
                                sectorToWrite = new byte[Constants.XGD_SECTOR_SIZE];
                                Helpers.FillArray(sectorToWrite, (byte)0xff);
                            }

                            // Try to compress the sector
                            using (var memoryStream = new MemoryStream())
                            {
                                var compressed = false;
                                var compressedSize = K4os.Compression.LZ4.LZ4Codec.Encode(sectorToWrite, compressedData, K4os.Compression.LZ4.LZ4Level.L12_MAX);
                                if (compressedSize > 0 && compressedSize < (Constants.XGD_SECTOR_SIZE - (4 + multiple)))
                                {
                                    var padding = ((compressedSize + 1 + multiple - 1) / multiple * multiple) - (compressedSize + 1);

                                    memoryStream.WriteByte((byte)padding);
                                    memoryStream.Write(compressedData, 0, compressedSize);
                                    if (padding != 0)
                                    {
                                        memoryStream.Write(new byte[padding], 0, padding);
                                    }
                                    compressed = true;
                                }
                                else
                                {
                                    memoryStream.Write(sectorToWrite, 0, sectorToWrite.Length);
                                }

                                var blockData = memoryStream.ToArray();
                                outputWriter.Write(blockData, 0, blockData.Length);
                                cciIndices.Add(new CCIIndex { Value = (ulong)blockData.Length, LZ4Compressed = compressed });
                            }

                            uncompressedSize += Constants.XGD_SECTOR_SIZE;
                            currentSector++;

                            var currentProgress = 0.6f + 0.4f * (currentSector / (float)totalSectors);
                            progress?.Invoke(currentProgress);
                        }

                        // Write index table
                        indexOffset = (ulong)outputStream.Position;

                        var position = (ulong)headerSize;
                        for (var i = 0; i < cciIndices.Count; i++)
                        {
                            var index = (uint)(position >> indexAlignment) | (cciIndices[i].LZ4Compressed ? 0x80000000U : 0U);
                            outputWriter.Write(index);
                            position += cciIndices[i].Value;
                        }
                        var indexEnd = (uint)(position >> indexAlignment);
                        outputWriter.Write(indexEnd);

                        // Update header with actual values
                        outputStream.Position = 8;
                        outputWriter.Write(uncompressedSize);
                        outputWriter.Write(indexOffset);
                    }

                    if (splitting || iteration > 0)
                    {
                        var destFile = Path.Combine(Path.GetDirectoryName(outputFile) ?? "", Path.GetFileNameWithoutExtension(outputFile) + $".{iteration + 1}{Path.GetExtension(outputFile)}");
                        if (File.Exists(destFile))
                        {
                            File.Delete(destFile);
                        }
                        File.Move(outputFile, destFile);
                    }

                    iteration++;
                }

                progress?.Invoke(1.0f);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.ToString());
                return false;
            }
        }

        public static bool ConvertContainerToISO(ContainerReader containerReader, ProcessingOptions processingOptions, string outputFile, long splitPoint, Action<float>? progress)
        {
            var progressPercent = 0.0f;
            progress?.Invoke(progressPercent);

            if (containerReader.GetMountCount() == 0)
            {
                return false;
            }

            if (containerReader.TryGetDataSectors(out var dataSectors) == false)
            {
                return false;
            }

            var scrubbedSector = new byte[2048];
            for (var i = 0; i < scrubbedSector.Length; i++)
            {
                scrubbedSector[i] = 0xff;
            }

            var decoder = containerReader.GetDecoder();
            var xgdInfo = decoder.GetXgdInfo();

            var startSector = 0u;
            if (processingOptions.HasFlag(ProcessingOptions.RemoveVideoPartition) == false)
            {
                for (var i = 0u; i < xgdInfo.BaseSector; i++)
                {
                    dataSectors.Add(i);
                }
            }
            else
            {
                startSector = xgdInfo.BaseSector;
            }

            var totalSectors = processingOptions.HasFlag(ProcessingOptions.TrimSectors) == true ? Math.Min(Helpers.RoundToMultiple(dataSectors.Max() + 1, 2), decoder.TotalSectors()) : decoder.TotalSectors();

            var currentSector = startSector;
            var iteration = 0;

            while (currentSector < totalSectors)
            {
                var splitting = false;
                using (var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                using (var outputWriter = new BinaryWriter(outputStream))
                {
                    while (currentSector < totalSectors)
                    {
                        var estimatedSize = outputStream.Position + 2048;
                        if (splitPoint > 0 && estimatedSize > splitPoint)
                        {
                            splitting = true;
                            break;
                        }

                        var sectorToWrite = scrubbedSector;
                        var writeSector = (processingOptions.HasFlag(ProcessingOptions.ScrubSectors) == false) || dataSectors.Contains(currentSector);
                        if (writeSector == true)
                        {
                            if (decoder.TryReadSector(currentSector, out sectorToWrite) == false)
                            {
                                return false;
                            }
                        }
                        outputStream.Write(sectorToWrite, 0, sectorToWrite.Length);

                        var currentProgressPercent = (float)Math.Round((currentSector - startSector) / (float)totalSectors, 4);
                        if (Helpers.IsEqualTo(currentProgressPercent, progressPercent) == false)
                        {
                            progress?.Invoke(currentProgressPercent);
                            Interlocked.Exchange(ref progressPercent, currentProgressPercent);
                        }

                        currentSector++;
                    }
                }

                if (splitting || iteration > 0)
                {
                    var destFile = Path.Combine(Path.GetDirectoryName(outputFile) ?? "", Path.GetFileNameWithoutExtension(outputFile) + $".{iteration + 1}{Path.GetExtension(outputFile)}");
                    if (File.Exists(destFile))
                    {
                        File.Delete(destFile);
                    }
                    File.Move(outputFile, destFile);
                }

                iteration++;
            }

            progress?.Invoke(1);
            return true;
        }

        public static bool ConvertContainerToCCI(ContainerReader containerReader, ProcessingOptions processingOptions, string outputFile, long splitPoint, Action<float>? progress)
        {
            var progressPercent = 0.0f;
            progress?.Invoke(progressPercent);

            if (containerReader.GetMountCount() == 0)
            {
                return false;
            }

            if (containerReader.TryGetDataSectors(out var dataSectors) == false)
            {
                return false;
            }

            var scrubbedSector = new byte[Constants.XGD_SECTOR_SIZE];
            for (var i = 0; i < scrubbedSector.Length; i++)
            {
                scrubbedSector[i] = 0xff;
            }

            var decoder = containerReader.GetDecoder();
            var xgdInfo = decoder.GetXgdInfo();

            var startSector = 0u;
            if (processingOptions.HasFlag(ProcessingOptions.RemoveVideoPartition) == false)
            {
                for (var i = 0u; i < xgdInfo.BaseSector; i++)
                {
                    dataSectors.Add(i);
                }
            }
            else
            {
                startSector = xgdInfo.BaseSector;
            }

            var totalSectors = processingOptions.HasFlag(ProcessingOptions.TrimSectors) == true ? Math.Min(Helpers.RoundToMultiple(dataSectors.Max() + 1, 2), decoder.TotalSectors()) : decoder.TotalSectors();

            var compressedData = new byte[2048];
            var currentSector = startSector;
            var iteration = 0;

            while (currentSector < totalSectors)
            {
                var cciIndices = new List<CCIIndex>();

                var splitting = false;
                using (var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                using (var outputWriter = new BinaryWriter(outputStream))
                {
                    uint header = 0x4D494343U;
                    outputWriter.Write(header);

                    uint headerSize = 32;
                    outputWriter.Write(headerSize);

                    ulong uncompressedSize = 0UL;
                    outputWriter.Write(uncompressedSize);

                    ulong indexOffset = 0UL;
                    outputWriter.Write(indexOffset);

                    uint blockSize = 2048;
                    outputWriter.Write(blockSize);

                    byte version = 1;
                    outputWriter.Write(version);

                    byte indexAlignment = 2;
                    outputWriter.Write(indexAlignment);

                    ushort unused = 0;
                    outputWriter.Write(unused);

                    while (currentSector < totalSectors)
                    {
                        var sectorToWrite = scrubbedSector;
                        var writeSector = (processingOptions.HasFlag(ProcessingOptions.ScrubSectors) == false) || dataSectors.Contains(currentSector);
                        if (writeSector == true)
                        {
                            if (decoder.TryReadSector(currentSector, out sectorToWrite) == false)
                            {
                                return false;
                            }
                        }

                        using (var memoryStream = new MemoryStream())
                        {
                            var compressed = false;
                            var compressedSize = K4os.Compression.LZ4.LZ4Codec.Encode(sectorToWrite, compressedData, K4os.Compression.LZ4.LZ4Level.L12_MAX);
                            if (compressedSize > 0 && compressedSize < (2048 - (4 + (1 << indexAlignment))))
                            {
                                var multiple = (1 << indexAlignment);
                                var padding = ((compressedSize + 1 + multiple - 1) / multiple * multiple) - (compressedSize + 1);

                                memoryStream.WriteByte((byte)padding);
                                memoryStream.Write(compressedData, 0, compressedSize);
                                if (padding != 0)
                                {
                                    memoryStream.Write(new byte[padding], 0, padding);
                                }
                                compressed = true;
                            }
                            else
                            {
                                memoryStream.Write(sectorToWrite, 0, sectorToWrite.Length);
                            }

                            var currentIndexSize = cciIndices.Count * 8;
                            var estimatedSize = outputStream.Position + currentIndexSize + memoryStream.Length;
                            if (splitPoint > 0 && estimatedSize > splitPoint)
                            {
                                splitting = true;
                                break;
                            }

                            outputWriter.Write(memoryStream.ToArray(), 0, (int)memoryStream.Length);
                            cciIndices.Add(new CCIIndex { Value = (ulong)memoryStream.Length, LZ4Compressed = compressed });
                        }

                        uncompressedSize += 2048;
                        currentSector++;
                       
                        if (progress != null)
                        {
                            progress((currentSector - startSector) / (float)(totalSectors - startSector));
                        }
                    }

                    indexOffset = (ulong)outputStream.Position;

                    var position = (ulong)headerSize;
                    for (var i = 0; i < cciIndices.Count; i++)
                    {
                        var index = (uint)(position >> indexAlignment) | (cciIndices[i].LZ4Compressed ? 0x80000000U : 0U);
                        outputWriter.Write(index); 
                        position += cciIndices[i].Value;
                    }
                    var indexEnd = (uint)(position >> indexAlignment);
                    outputWriter.Write(indexEnd);

                    outputStream.Position = 8;
                    outputWriter.Write(uncompressedSize);
                    outputWriter.Write(indexOffset);
                }

                if (splitting || iteration > 0)
                {
                    var destFile = Path.Combine(Path.GetDirectoryName(outputFile) ?? "", Path.GetFileNameWithoutExtension(outputFile) + $".{iteration + 1}{Path.GetExtension(outputFile)}");
                    if (File.Exists(destFile))
                    {
                        File.Delete(destFile);
                    }
                    File.Move(outputFile, destFile);
                }

                iteration++;
            }

            progress?.Invoke(1);
            return true;
        }

        public struct FileInfo
        {
            public bool IsFile { get; set; }
            public string Path { get; set; }
            public string Filename { get; set; }
            public long Size { get; set; }
            public int StartSector { get; set; }
            public int EndSector { get; set; }
            public string InSlices { get; set; }
        }

        public static void GetFileInfoFromContainer(ContainerReader containerReader, Action<FileInfo> info, Action<float>? progress, CancellationToken cancellationToken)
        {
            if (progress != null)
            {
                progress(0);
            }

            if (containerReader.GetMountCount() == 0)
            {
                throw new Exception("Container not mounted.");
            }

            try
            {
                var decoder = containerReader.GetDecoder();
                var xgdInfo = decoder.GetXgdInfo();

                var rootSectors = xgdInfo.RootDirSize / Constants.XGD_SECTOR_SIZE;
                var rootData = new byte[xgdInfo.RootDirSize];
                for (var i = 0; i < rootSectors; i++)
                {
                    var currentRootSector = xgdInfo.BaseSector + xgdInfo.RootDirSector + (uint)i;
                    if (decoder.TryReadSector(currentRootSector, out var sectorData) == false)
                    {
                        return;
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

                var totalNodes = 1;
                var processedNodes = 0;

                while (treeNodes.Count > 0)
                {
                    var currentTreeNode = treeNodes[0];
                    treeNodes.RemoveAt(0);
                    processedNodes++;

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
                            totalNodes++;
                        }

                        if ((attribute & 0x10) != 0)
                        {
                            if (size > 0)
                            {
                                var directorySectors = size / Constants.XGD_SECTOR_SIZE;
                                var directoryData = new byte[size];
                                for (var i = 0; i < directorySectors; i++)
                                {
                                    var currentDirectorySector = xgdInfo.BaseSector + sector + (uint)i;
                                    if (decoder.TryReadSector(currentDirectorySector, out var sectorData) == false)
                                    {
                                        return;
                                    }
                                    Array.Copy(sectorData, 0, directoryData, i * Constants.XGD_SECTOR_SIZE, Constants.XGD_SECTOR_SIZE);
                                }

                                treeNodes.Add(new TreeNodeInfo
                                {
                                    DirectoryData = directoryData,
                                    Offset = 0,
                                    Path = System.IO.Path.Combine(currentTreeNode.Path, filename)
                                });
                                totalNodes++;
                                info(new FileInfo
                                {
                                    IsFile = false,
                                    Path = System.IO.Path.Combine(currentTreeNode.Path, filename),
                                    Filename = filename,
                                    Size = size,
                                    StartSector = (int)(xgdInfo.BaseSector + sector),
                                    EndSector = (int)(xgdInfo.BaseSector + sector + ((size + Constants.XGD_SECTOR_SIZE - 1) / Constants.XGD_SECTOR_SIZE) - 1),
                                    InSlices = "N/A"
                                });
                            }
                        }
                        else
                        {
                            if (size > 0)
                            {
                                var startSector = (int)(xgdInfo.BaseSector + sector);
                                var endSector = (int)(xgdInfo.BaseSector + sector + ((size + Constants.XGD_SECTOR_SIZE - 1) / Constants.XGD_SECTOR_SIZE) - 1);
                                info(new FileInfo
                                {
                                    IsFile = true,
                                    Path = currentTreeNode.Path,
                                    Filename = filename,
                                    Size = size,
                                    StartSector = startSector,
                                    EndSector = endSector,
                                    InSlices = "N/A"
                                });
                            }
                            else
                            {
                                info(new FileInfo
                                {
                                    IsFile = true,
                                    Path = currentTreeNode.Path,
                                    Filename = filename,
                                    Size = size,
                                    StartSector = -1,
                                    EndSector = -1,
                                    InSlices = "N/A"
                                });
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
                            totalNodes++;
                        }

                        if (progress != null)
                        {
                            progress(processedNodes / (float)totalNodes);
                        }

                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.ToString());
                throw;
            }
        }

        public static string GetChecksumFromContainer(ContainerReader containerReader, Action<float>? progress, CancellationToken cancellationToken)
        {
            if (progress != null)
            {
                progress(0);
            }

            if (containerReader.GetMountCount() == 0)
            {
                throw new Exception("Container not mounted.");
            }

            try
            {
                var decoder = containerReader.GetDecoder();
                using var hash = SHA256.Create();
                
                var totalSectors = decoder.TotalSectors();
                for (var i = 0u; i < totalSectors; i++)
                {
                    if (decoder.TryReadSector(i, out var buffer))
                    {
                        hash.TransformBlock(buffer, 0, buffer.Length, null, 0);
                    }
                    
                    if (progress != null)
                    {
                        progress(i / (float)totalSectors);
                    }
                    
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
                
                hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                var sha256Hash = hash.Hash;
                if (sha256Hash == null)
                {
                    throw new ArgumentOutOfRangeException();
                }
                return BitConverter.ToString(sha256Hash).Replace("-", string.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print(ex.ToString());
                throw;
            }
        }

        public static void CompareContainers(ContainerReader containerReader1, ContainerReader containerReader2, Action<string> log, Action<float>? progress)
        {
            if (containerReader1.GetMountCount() == 0 || containerReader2.GetMountCount() == 0)
            {
                log("One or both containers are not mounted.");
                return;
            }

            try
            {
                var decoder1 = containerReader1.GetDecoder();
                var decoder2 = containerReader2.GetDecoder();
                var xgdInfo1 = decoder1.GetXgdInfo();
                var xgdInfo2 = decoder2.GetXgdInfo();

                if (xgdInfo1.BaseSector > 0)
                {
                    log("First contains a video partition, compare will ignore those sectors.");
                }

                if (xgdInfo2.BaseSector > 0)
                {
                    log("Second contains a video partition, compare will ignore those sectors.");
                }

                var totalSectors1 = decoder1.TotalSectors();
                var totalSectors2 = decoder2.TotalSectors();

                if (totalSectors1 - xgdInfo1.BaseSector != totalSectors2 - xgdInfo2.BaseSector)
                {
                    log("Expected sector counts do not match, assuming image could be trimmed.");
                }

                log("");
                log("Getting data sectors hash for first...");
                if (!containerReader1.TryGetDataSectors(out var dataSectors1))
                {
                    log("Failed to get data sectors from first container.");
                    return;
                }
                var dataSectors1Array = dataSectors1.ToArray();
                Array.Sort(dataSectors1Array);

                log("Calculating data sector hashes for first...");
                using var dataSectorsHash1 = SHA256.Create();
                for (var i = 0; i < dataSectors1Array.Length; i++)
                {
                    var dataSector1 = dataSectors1Array[i];
                    if (decoder1.TryReadSector(dataSector1, out var buffer))
                    {
                        dataSectorsHash1.TransformBlock(buffer, 0, buffer.Length, null, 0);
                    }
                    if (progress != null)
                    {
                        progress(i / (float)dataSectors1Array.Length * 0.5f);
                    }
                }
                dataSectorsHash1.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                var dataChecksum1 = dataSectorsHash1.Hash;
                if (dataChecksum1 == null)
                {
                    throw new ArgumentOutOfRangeException();
                }
                var dataSectorsHash1Result = BitConverter.ToString(dataChecksum1).Replace("-", string.Empty);

                log("Getting data sectors hash for second...");
                if (!containerReader2.TryGetDataSectors(out var dataSectors2))
                {
                    log("Failed to get data sectors from second container.");
                    return;
                }
                var dataSectors2Array = dataSectors2.ToArray();
                Array.Sort(dataSectors2Array);

                log("Calculating data sector hash for second...");
                using var dataSectorsHash2 = SHA256.Create();
                for (var i = 0; i < dataSectors2Array.Length; i++)
                {
                    var dataSector2 = dataSectors2Array[i];
                    if (decoder2.TryReadSector(dataSector2, out var buffer))
                    {
                        dataSectorsHash2.TransformBlock(buffer, 0, buffer.Length, null, 0);
                    }
                    if (progress != null)
                    {
                        progress(0.5f + i / (float)dataSectors2Array.Length * 0.5f);
                    }
                }
                dataSectorsHash2.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                var dataChecksum2 = dataSectorsHash2.Hash;
                if (dataChecksum2 == null)
                {
                    throw new ArgumentOutOfRangeException();
                }
                var dataSectorsHash2Result = BitConverter.ToString(dataChecksum2).Replace("-", string.Empty);

                if (dataSectorsHash1Result == dataSectorsHash2Result)
                {
                    log("Data sectors match.");
                }
                else
                {
                    log("Data sectors do not match.");
                }

                log("");
                log($"First image data sectors range: {dataSectors1Array.First()} - {dataSectors1Array.Last()}");
                log($"Second image data sectors range: {dataSectors2Array.First()} - {dataSectors2Array.Last()}");
                log("");
            }
            catch (Exception ex)
            {
                log($"Error during comparison: {ex.Message}");
                System.Diagnostics.Debug.Print(ex.ToString());
            }
        }
    }
}
