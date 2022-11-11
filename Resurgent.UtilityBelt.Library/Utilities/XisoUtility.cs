using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Resurgent.UtilityBelt.Library.Utilities
{
    public struct IndexInfo
    {
        public ulong Value { get; set; }

        public bool Compressed { get; set; }
    }
     
    public static class RepackinatorImports
    {
        [DllImport("RepackinatorLz4.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr CreateCabinetProcessor();

        [DllImport("RepackinatorLz4.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int DisposeCabinetProcessor(IntPtr cabinetProcessor);

        [DllImport("RepackinatorLz4.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int PackSector(IntPtr cabinetProcessor, IntPtr inputBuffer, int inputBufferSize, IntPtr outputBuffer, int outputBufferSize, out int compressedSize, out int result);

        [DllImport("RepackinatorLz4.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int UnpackSector(IntPtr cabinetProcessor, IntPtr inputBuffer, int inputBufferSize, IntPtr outputBuffer, int outputBufferSize, out int uncompressedSize, out int result);

        public static bool PackSector(byte[] inputBuffer, byte[] outputBuffer, out int compressedSize)
        {
            var HadesCabinetHandle = CreateCabinetProcessor();

            var inputPinnedArray = GCHandle.Alloc(inputBuffer, GCHandleType.Pinned);
            var inputPtr = inputPinnedArray.AddrOfPinnedObject();
            var outputPinnedArray = GCHandle.Alloc(outputBuffer, GCHandleType.Pinned);
            var outputPtr = outputPinnedArray.AddrOfPinnedObject();
            PackSector(HadesCabinetHandle, inputPtr, inputBuffer.Length, outputPtr, outputBuffer.Length, out compressedSize, out var result);
            inputPinnedArray.Free();
            outputPinnedArray.Free();

            DisposeCabinetProcessor(HadesCabinetHandle);

            return result == 1;
        }

        public static bool UnpackSector(byte[] inputBuffer, byte[] outputBuffer, out int uncompressedSize)
        {
            var HadesCabinetHandle = CreateCabinetProcessor();

            var inputPinnedArray = GCHandle.Alloc(inputBuffer, GCHandleType.Pinned);
            var inputPtr = inputPinnedArray.AddrOfPinnedObject();
            var outputPinnedArray = GCHandle.Alloc(outputBuffer, GCHandleType.Pinned);
            var outputPtr = outputPinnedArray.AddrOfPinnedObject();
            UnpackSector(HadesCabinetHandle, inputPtr, inputBuffer.Length, outputPtr, outputBuffer.Length, out uncompressedSize, out var result);
            inputPinnedArray.Free();
            outputPinnedArray.Free();

            DisposeCabinetProcessor(HadesCabinetHandle);

            return result == 1;
        }
    }

    public static class XisoUtility
    {
        private class CsoIndexInfo
        {
            public uint Size { get; set; }
            public bool Compressed { get; set; }
        }

        private class CsoIndexInfosPart
        {
            public List<CsoIndexInfo> CsoIndexInfos { get; } = new List<CsoIndexInfo>();

            public ulong UncompressedSize { get; set; }
        }

        private static bool PatternMatch(byte[] buffer, int offset, byte[] compare)
        {
            for (var i = 0; i < compare.Length; i++)
            {
                if (buffer[offset + i] != compare[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static string GetUtf8String(byte[] buffer, int offset, int length)
        {
            var result = string.Empty;
            for (var i = 0; i < length; i++)
            {
                var value = buffer[i + offset];
                result += (char)value;
            }
            return result;
        }

        private struct TreeNodeInfo
        {
            public uint DirectorySize { get; set; }
            public long DirectoryOffset { get; set; }
            public uint Offset { get; set; }
            public long StartOffset { get; set; }
        };

        public static HashSet<uint> GetDataSectorsFropmXiso(Stream inputStream)
        {
            using var binaryReader = new BinaryReader(inputStream, Encoding.Default, true);

            var dataSectors = new HashSet<uint>();
            var headerSector = (0x18300000U + 0x10000U) / 2048;
            dataSectors.Add(headerSector);
            dataSectors.Add(headerSector + 1);
            binaryReader.BaseStream.Position = 0x18300000 + 0x10000 + 20;

            var rootSector = binaryReader.ReadUInt32();
            var rootSize = binaryReader.ReadUInt32();
            var rootOffset = (long)rootSector * 2048;

            var treeNodes = new List<TreeNodeInfo>
            {
                new TreeNodeInfo
                {
                    DirectorySize = rootSize,
                    DirectoryOffset = rootOffset,
                    Offset = 0,
                    StartOffset = 0x18300000
                }
            };

            while (treeNodes.Count > 0)
            {
                var currentTreeNode = treeNodes[0];

                var currentOffset = currentTreeNode.StartOffset + currentTreeNode.DirectoryOffset + currentTreeNode.Offset * 4;

                for (var i = currentOffset / 2048; i < (currentOffset / 2048) + ((currentTreeNode.DirectorySize - (currentTreeNode.Offset * 4) + 2048 - 1) / 2048); i++)
                {
                    dataSectors.Add((uint)i);
                }

                if ((currentTreeNode.Offset * 4) >= currentTreeNode.DirectorySize)
                {
                    treeNodes.RemoveAt(0);
                    continue;
                }

                binaryReader.BaseStream.Position = currentOffset;

                var left = binaryReader.ReadUInt16();
                var right = binaryReader.ReadUInt16();
                var sector = binaryReader.ReadUInt32();
                var size = binaryReader.ReadUInt32();
                var attribute = binaryReader.ReadByte();
                var dataOffset = (long)sector * 2048;

                if (left == 0xFFFF)
                {
                    treeNodes.RemoveAt(0);
                    continue;
                }

                if (left != 0)
                {
                    treeNodes.Add(new TreeNodeInfo
                    {
                        DirectorySize = currentTreeNode.DirectorySize,
                        DirectoryOffset = currentTreeNode.DirectoryOffset,
                        Offset = left,
                        StartOffset = currentTreeNode.StartOffset
                    });
                }

                if ((attribute & 0x10) != 0)
                {
                    if (size > 0)
                    {
                        treeNodes.Add(new TreeNodeInfo
                        {
                            DirectorySize = size,
                            DirectoryOffset = dataOffset,
                            Offset = 0,
                            StartOffset = currentTreeNode.StartOffset
                        });
                    }
                }
                else
                {
                    for (var i = (currentTreeNode.StartOffset + dataOffset) / 2048; i < (currentTreeNode.StartOffset + dataOffset) / 2048 + ((size + 2048 - 1) / 2048); i++)
                    {
                        dataSectors.Add((uint)i);
                    }
                }

                if (right != 0)
                {
                    treeNodes.Add(new TreeNodeInfo
                    {
                        DirectorySize = currentTreeNode.DirectorySize,
                        DirectoryOffset = currentTreeNode.DirectoryOffset,
                        Offset = right,
                        StartOffset = currentTreeNode.StartOffset
                    });
                }

                treeNodes.RemoveAt(0);
            }

            return dataSectors;
        }

        public static bool TryExtractDefaultFromXiso(Stream inputStream, Stream outputStream, ref string error)
        {
            const long XGD1_LSEEK_OFFSET = 0x18300000;
            // const long XGD1_LSEEK_OFFSET = 0x100000000;
            const long SectorSize = 2048;
            const long VolumeSector = 32;

            byte[] Magic = Encoding.ASCII.GetBytes("MICROSOFT*XBOX*MEDIA");

            try
            {
                using var binaryReader = new BinaryReader(inputStream);

                long headerOffset = 0;
                inputStream.Position = VolumeSector * SectorSize;
                var volumeDescriptor = binaryReader.ReadBytes((int)SectorSize);
                if (!PatternMatch(volumeDescriptor, 0, Magic))
                {
                    headerOffset = XGD1_LSEEK_OFFSET;
                    inputStream.Position = headerOffset + VolumeSector * SectorSize;
                    volumeDescriptor = binaryReader.ReadBytes((int)SectorSize);
                    if (!PatternMatch(volumeDescriptor, 0, Magic))
                    {
                        error = "Invalid volume descriptor detected.";
                        return false;
                    }
                }

                var rootDirectoryTableSector = BitConverter.ToUInt32(volumeDescriptor, 0x14);
                var rootDirectoryTableSize = BitConverter.ToInt32(volumeDescriptor, 0x18);
                var fileTime = BitConverter.ToUInt64(volumeDescriptor, 0x1c);

                if (!PatternMatch(volumeDescriptor, volumeDescriptor.Length - 20, Magic))
                {
                    error = "Invalid volume descriptor detected.";
                    return false;
                }

                inputStream.Position = headerOffset + rootDirectoryTableSector * SectorSize;
                var directoryTable = binaryReader.ReadBytes(rootDirectoryTableSize);

                var entryOffset = 0;
                while (entryOffset < directoryTable.Length)
                {
                    var leftOffset = BitConverter.ToUInt16(directoryTable, 0x0 + entryOffset);
                    var rightOffset = BitConverter.ToUInt16(directoryTable, 0x2 + entryOffset);
                    if (leftOffset == 0xffff && rightOffset == 0xffff)
                    {
                        entryOffset += 4;
                        continue;
                    }


                    var fileSector = BitConverter.ToUInt32(directoryTable, 0x4 + entryOffset);
                    var fileSize = BitConverter.ToUInt32(directoryTable, 0x8 + entryOffset);
                    var fileAttributes = directoryTable[0xc + entryOffset];

                    //bool isReadOnly = (fileAttributes & 0x01) != 0;
                    //bool isHidden = (fileAttributes & 0x02) != 0;
                    //bool isSystem = (fileAttributes & 0x04) != 0;
                    //bool isDirectory = (fileAttributes & 0x10) != 0;
                    //bool isArchive = (fileAttributes & 0x20) != 0;
                    //bool isNormal = (fileAttributes & 0x80) != 0;

                    var filenameLength = (int)directoryTable[0xd + entryOffset];
                    var filename = GetUtf8String(directoryTable, 0xe + entryOffset, filenameLength);

                    filenameLength = 0xe + filenameLength;
                    filenameLength += (4 - filenameLength % 4) % 4;

                    entryOffset += filenameLength;

                    if (filename.Equals("default.xbe", StringComparison.CurrentCultureIgnoreCase))
                    {
                        long offset = headerOffset + fileSector * SectorSize;
                        inputStream.Seek(offset, SeekOrigin.Begin);

                        inputStream.Position = offset;
                        var fileData = binaryReader.ReadBytes((int)fileSize);
                        outputStream.Write(fileData);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.ToString();
                return false;
            }

            error = "Default.xbe not detected.";
            return false;
        }

        private static byte[] ReadBytesFromSplitIso(BinaryReader binaryReader1, BinaryReader binaryReader2, long offset, int count)
        {
            var binaryReaders = new[] { binaryReader1, binaryReader2 };

            var isoPart = offset < binaryReader1.BaseStream.Length ? 0 : 1;

            // Check if goes over boundary
            if (isoPart == 0 && offset + count > binaryReaders[0].BaseStream.Length)
            {
                using (var memoryStream = new MemoryStream())
                {
                    var remainder = (offset + count) - binaryReaders[0].BaseStream.Length;
                    binaryReaders[0].BaseStream.Position = offset;
                    var result1 = binaryReaders[isoPart].ReadBytes(count - (int)remainder);
                    memoryStream.Write(result1);
                    binaryReaders[1].BaseStream.Position = 0;
                    var result2 = binaryReaders[isoPart].ReadBytes((int)remainder);
                    memoryStream.Write(result2);
                    return memoryStream.ToArray();
                }
            }

            binaryReaders[isoPart].BaseStream.Position = (isoPart == 0 ? offset : offset - binaryReaders[0].BaseStream.Length);
            return binaryReaders[isoPart].ReadBytes(count);
        }

        public static bool TryExtractDefaultFromSplitXiso(Stream inputStream1, Stream inputStream2, Stream outputStream, ref string error)
        {
            const long XGD1_LSEEK_OFFSET = 0x18300000;
            // const long XGD1_LSEEK_OFFSET = 0x100000000;
            const long SectorSize = 2048;
            const long VolumeSector = 32;

            byte[] Magic = Encoding.ASCII.GetBytes("MICROSOFT*XBOX*MEDIA");

            using var binaryReader1 = new BinaryReader(inputStream1, Encoding.Default, true);
            using var binaryReader2 = new BinaryReader(inputStream2, Encoding.Default, true);

            try
            {
                long headerOffset = 0;

                var volumeDescriptor = ReadBytesFromSplitIso(binaryReader1, binaryReader2, VolumeSector * SectorSize, (int)SectorSize);
                if (!PatternMatch(volumeDescriptor, 0, Magic))
                {
                    headerOffset = XGD1_LSEEK_OFFSET;
                    volumeDescriptor = ReadBytesFromSplitIso(binaryReader1, binaryReader2, headerOffset + VolumeSector * SectorSize, (int)SectorSize);
                    if (!PatternMatch(volumeDescriptor, 0, Magic))
                    {
                        error = "Invalid volume descriptor detected.";
                        return false;
                    }
                }

                var rootDirectoryTableSector = BitConverter.ToUInt32(volumeDescriptor, 0x14);
                var rootDirectoryTableSize = BitConverter.ToInt32(volumeDescriptor, 0x18);
                var fileTime = BitConverter.ToUInt64(volumeDescriptor, 0x1c);

                if (!PatternMatch(volumeDescriptor, volumeDescriptor.Length - 20, Magic))
                {
                    error = "Invalid volume descriptor detected.";
                    return false;
                }

                var directoryTable = ReadBytesFromSplitIso(binaryReader1, binaryReader2, headerOffset + rootDirectoryTableSector * SectorSize, rootDirectoryTableSize);
                var entryOffset = 0;
                while (entryOffset < directoryTable.Length)
                {
                    var leftOffset = BitConverter.ToUInt16(directoryTable, 0x0 + entryOffset);
                    var rightOffset = BitConverter.ToUInt16(directoryTable, 0x2 + entryOffset);
                    if (leftOffset == 0xffff && rightOffset == 0xffff)
                    {
                        entryOffset += 4;
                        continue;
                    }

                    var fileSector = BitConverter.ToUInt32(directoryTable, 0x4 + entryOffset);
                    var fileSize = BitConverter.ToUInt32(directoryTable, 0x8 + entryOffset);
                    var fileAttributes = directoryTable[0xc + entryOffset];

                    //bool isReadOnly = (fileAttributes & 0x01) != 0;
                    //bool isHidden = (fileAttributes & 0x02) != 0;
                    //bool isSystem = (fileAttributes & 0x04) != 0;
                    //bool isDirectory = (fileAttributes & 0x10) != 0;
                    //bool isArchive = (fileAttributes & 0x20) != 0;
                    //bool isNormal = (fileAttributes & 0x80) != 0;

                    var filenameLength = (int)directoryTable[0xd + entryOffset];
                    var filename = GetUtf8String(directoryTable, 0xe + entryOffset, filenameLength);

                    filenameLength = 0xe + filenameLength;
                    filenameLength += (4 - filenameLength % 4) % 4;

                    entryOffset += filenameLength;

                    if (filename.Equals("default.xbe", StringComparison.CurrentCultureIgnoreCase))
                    {
                        long offset = headerOffset + fileSector * SectorSize;
                        var fileData = ReadBytesFromSplitIso(binaryReader1, binaryReader2, offset, (int)fileSize);
                        outputStream.Write(fileData);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.ToString();
                return false;
            }

            error = "Default.xbe not detected.";
            return false;
        }

        public static bool Split(string input, string outputPath, string name, string extension, bool scrub, Action<float>? progress, CancellationToken cancellationToken)
        {
            if (progress != null)
            {
                progress(0);
            }

            using var fileStream = new FileStream(input, FileMode.Open, FileAccess.Read);
            using var binaryReader = new BinaryReader(fileStream);

            var dataSectors = new HashSet<uint>();
            if (scrub)
            {
                dataSectors = GetDataSectorsFropmXiso(fileStream);
            }

            fileStream.Seek(0, SeekOrigin.End);

            var fileLength = fileStream.Position;
            if (fileLength % 2048 > 0)
            {
                return false;
            }

            var redumpSize = 0x1D26A8000L;
            var videoSize =  0x18300000U;
            var fileSectors = (uint)(fileLength / 2048);
            var skipSize = fileLength == redumpSize ? videoSize : 0;
            var skipSectors = skipSize / 2048;
            var sectorSplit = (uint)((fileLength - skipSize) / 4096);

            fileStream.Seek(skipSize, SeekOrigin.Begin);

            using var partStream1 = new FileStream(Path.Combine(outputPath, $"{name}.1{extension}"), FileMode.Create, FileAccess.Write);
            using var partWriter1 = new BinaryWriter(partStream1);

            using var partStream2 = new FileStream(Path.Combine(outputPath, $"{name}.2{extension}"), FileMode.Create, FileAccess.Write);
            using var partWriter2 = new BinaryWriter(partStream2);

            var emptySector = new byte[2048];

            for (var i = 0U; i < sectorSplit; i++)
            {
                var writeSector = true;
                if (scrub)
                {
                    writeSector = dataSectors.Contains(i - skipSectors);                    
                }
                if (writeSector == true)
                {
                    var sectorBuffer = binaryReader.ReadBytes(2048);
                    partWriter1.Write(sectorBuffer);
                }
                else
                {
                    binaryReader.BaseStream.Position += 2048;
                    partWriter1.Write(emptySector);
                }

                if (progress != null)
                {
                    progress(i / (float)fileSectors);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return true;
            }

            for (var i = sectorSplit; i < (fileSectors - skipSectors); i++)
            {
                var writeSector = true;
                if (scrub)
                {
                    writeSector = dataSectors.Contains(i - skipSectors);
                }
                if (writeSector == true)
                {
                    var sectorBuffer = binaryReader.ReadBytes(2048);
                    partWriter2.Write(sectorBuffer);
                }
                else
                {
                    binaryReader.BaseStream.Position += 2048;
                    partWriter2.Write(emptySector);
                }

                if (progress != null)
                {
                    progress(i / (float)fileSectors);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

            }

            return true;
        }

        //https://github.com/laffer1/ciso-maker

        public static void CompareCompression()
        {
            var buffer = new byte[2048];
            for (var i = 0; i < 2048; i++)
            {
                buffer[i] = (byte)(i % 15);
            }

            var compressedBuffer1 = new byte[2048];
            var result1 = RepackinatorImports.PackSector(buffer, compressedBuffer1, out var compressedSize1);
            if (result1 == false) 
            {
                System.Diagnostics.Debug.WriteLine("Fail");
                return;
            }


            var compressedBuffer2 = new byte[2048];
            var compressedSize2 = K4os.Compression.LZ4.LZ4Codec.Encode(buffer, compressedBuffer2, K4os.Compression.LZ4.LZ4Level.L12_MAX);



            if (compressedSize1 != compressedSize2)
            {
                System.Diagnostics.Debug.WriteLine("Fail");
                return;
            }

            for (var i = 0; i < 2048; i++)
            {
                if (compressedBuffer1[i] != compressedBuffer2[i])
                {

                    System.Diagnostics.Debug.WriteLine("Fail");
                    return;
                }
            }

            System.Diagnostics.Debug.WriteLine("OK");
        }

        public static bool CreateCCI(string inputFile, string outputPath, string name, string extension, bool scrub, Action<float>? progress, CancellationToken cancellationToken)
        {
            if (progress != null)
            {
                progress(0);
            }

            var indexInfos = new List<IndexInfo>();

            using var inputStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
            using var inputReader = new BinaryReader(inputStream);

            var dataSectors = new HashSet<uint>();
            if (scrub)
            {
                dataSectors = GetDataSectorsFropmXiso(inputStream);
            }

            inputStream.Seek(0, SeekOrigin.End);

            var fileLength = inputStream.Position;
            if (fileLength % 2048 > 0)
            {
                return false;
            }
            inputStream.Position = 0;

            var redumpSize = 0x1D26A8000L;
            var videoSize = 0x18300000U;
            var fileSectors = (uint)(fileLength / 2048);
            var skipSize = fileLength == redumpSize ? videoSize : 0;
            var skipSectors = skipSize / 2048;
            var splitMargin = 0x100000000L - ((3 * 2048) + (3 * 4) + 36);

            inputStream.Position = skipSize;

            var emptySector = new byte[2048];
            var compressedData = new byte[2048];
            var sectorsWritten = 0U;
            var iteration = 0;

            while (sectorsWritten < fileSectors - skipSectors)
            {
                var outputFile = Path.Combine(outputPath, iteration > 0 ? $"{name}.{iteration + 1}{extension}" : $"{name}{extension}");
                var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
                var outputWriter = new BinaryWriter(outputStream);

                uint header = 0x4D494343U;
                outputWriter.Write(header);

                ulong uncompressedSize = (ulong)0;
                outputWriter.Write(uncompressedSize);

                uint headerSize = 32;
                outputWriter.Write(headerSize);

                ulong indexOffset = (ulong)0;
                outputWriter.Write(indexOffset);

                uint blockSize = 2048;
                outputWriter.Write(blockSize);

                byte version = 1;
                outputWriter.Write(version);

                byte indexAlignment = 2;
                outputWriter.Write(indexAlignment);

                ushort unused = 0;
                outputWriter.Write(unused);

                headerSize = (uint)outputStream.Position;

                var splitting = false;
                var sectorCount = 0U;
                while (sectorsWritten < fileSectors - skipSectors)
                {
                    var writeSector = true;
                    if (scrub)
                    {
                        writeSector = dataSectors.Contains(sectorsWritten + skipSectors);
                    }

                    var sectorToWrite = emptySector;
                    if (writeSector == true)
                    {
                        sectorToWrite = inputReader.ReadBytes(2048);
                    }
                    else
                    {
                        inputReader.BaseStream.Position += 2048;
                    }

                    var compressedSize = K4os.Compression.LZ4.LZ4Codec.Encode(sectorToWrite, compressedData, K4os.Compression.LZ4.LZ4Level.L12_MAX);
                    if (compressedSize > 0 && compressedSize < (2048 - (1 << indexAlignment)))
                    {
                        outputWriter.Write(compressedData, 0, compressedSize);
                        var padding = compressedSize % (1 << indexAlignment);
                        if (padding != 0)
                        {
                            outputWriter.Write(new byte[padding]);
                        }
                        indexInfos.Add(new IndexInfo { Value = (ushort)(compressedSize + padding), Compressed = true });
                    }
                    else
                    {
                        outputWriter.Write(sectorToWrite);
                        indexInfos.Add(new IndexInfo { Value = 2048, Compressed = false });
                    }

                    uncompressedSize += 2048;
                    sectorsWritten++;
                    sectorCount++;

                    indexOffset = (ulong)outputStream.Position;

                    if (outputStream.Position > (splitMargin - (sectorCount * 4)))
                    {
                        splitting = true;
                        break;
                    }

                    if (progress != null)
                    {
                        progress(sectorsWritten / (float)(fileSectors - skipSectors));
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    outputStream.Dispose();
                    outputWriter.Dispose();
                    return true;
                }

                indexOffset = (ulong)outputStream.Position;

                var position = (ulong)headerSize;
                for (var i = 0; i < indexInfos.Count; i++)
                {
                    var index = (uint)(position >> indexAlignment) | (indexInfos[i].Compressed ? 0x80000000U : 0U);
                    outputWriter.Write(index);
                    position += indexInfos[i].Value;
                }
                var indexEnd = (uint)(position >> indexAlignment);
                outputWriter.Write(indexEnd);

                outputStream.Position = 4;
                outputWriter.Write(uncompressedSize);
                outputWriter.Write(indexOffset);

                outputStream.Dispose();
                outputWriter.Dispose();

                if (splitting)
                {
                    File.Move(outputFile, Path.Combine(outputPath, $"{name}.{iteration + 1}{extension}"));
                }

                iteration++;
            }

            return true;
        }

        public static bool ConvertCCItoISO(string inputFile, string outputFile)
        {
            using var inputStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read);
            using var inputReader = new BinaryReader(inputStream);

            var header = inputReader.ReadUInt32();
            if (header != 0x4D494343)
            {
                return false;
            }

            ulong uncompressedSize = inputReader.ReadUInt64();

            uint headerSize = inputReader.ReadUInt32(); 
            if (headerSize != 32)
            {
                return false;
            }

            ulong indexOffset = inputReader.ReadUInt64();

            uint blockSize = inputReader.ReadUInt32();
            if (blockSize != 2048)
            {
                return false;
            }

            byte version = inputReader.ReadByte();
            if (version != 1)
            {
                return false;
            }

            byte indexAlignment = inputReader.ReadByte();
            if (indexAlignment != 2)
            {
                return false;
            }

            ushort padding = inputReader.ReadUInt16();

            using var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
            using var outputWriter = new BinaryWriter(outputStream);

            var entries = (int)(uncompressedSize / (ulong)blockSize);

            inputStream.Position = (long)indexOffset;

            var indexInfos = new List<IndexInfo>();
            for (var i = 0; i <= entries; i++)
            {
                var index = inputReader.ReadUInt32();
                indexInfos.Add(new IndexInfo
                {
                    Value = (index & 0x7FFFFFFF) << indexAlignment,
                    Compressed = (index & 0x80000000) > 0
                });
            }

            var decodeBuffer = new byte[2048];
            for (var i = 0; i < entries; i++)
            {
                inputStream.Position = (long)indexInfos[i].Value;
                
                var size = (int)(indexInfos[i + 1].Value - indexInfos[i].Value);
                if (size < 2048 || indexInfos[i].Compressed)
                {
                    var buffer = inputReader.ReadBytes(size);
                    var compressedSize = K4os.Compression.LZ4.LZ4Codec.Decode(buffer, decodeBuffer);
                    if (compressedSize < 0)
                    {
                        return false;
                    }
                    outputWriter.Write(decodeBuffer);
                }
                else
                {
                    var buffer = inputReader.ReadBytes(2048);
                    outputWriter.Write(buffer);
                }
            }

            return true;
        }

        //public static bool SplitCSO(string input, string outputPath, string isoname, Action<float>? progress, CancellationToken cancellationToken)
        //{
        //    const long maxFileSize = (4 * 1024L * 1024L * 1024L) - 4096L; // 4096 is to allow for XBMC bug

        //    using var inputStream = new FileStream(input, FileMode.Open, FileAccess.Read);
        //    using var inputReader = new BinaryReader(inputStream);

        //    var header = inputReader.ReadInt32();
        //    if (header != 0x4F534943)
        //    {
        //        return false;
        //    }

        //    var headerSize = inputReader.ReadInt32();
        //    if (headerSize != 24)
        //    {
        //        return false;
        //    }

        //    var uncompressedSize = inputReader.ReadUInt64();

        //    var blockSize = inputReader.ReadInt32();
        //    if (blockSize != 2048)
        //    {
        //        return false;
        //    }

        //    var version = inputReader.ReadByte();
        //    if (version != 1)
        //    {
        //        return false;
        //    }

        //    var indexAlignment = inputReader.ReadByte();
        //    if (indexAlignment != 2)
        //    {
        //        return false;
        //    }
            
        //    var unused = inputReader.ReadUInt16();

        //    var entries = (int)(uncompressedSize / (ulong)blockSize) + 1;
        //    var indexTableSize = 4 * entries;

        //    var indexTableOffset = (long)headerSize;

        //    var totalSize = 0L;
        //    var headerIndexSize = (long)headerSize;
        //    var csoIndexInfosParts = new List<CsoIndexInfosPart>
        //    {
        //        new CsoIndexInfosPart()
        //    };

        //    // Calculate best place to split and cache index info

        //    var currentPart = 0;
        //    for (var i = 0; i < entries - 1; i++)
        //    {
        //        inputStream.Position = indexTableOffset + (i * 4);

        //        var index1 = inputReader.ReadUInt32();
        //        var position1 = (index1 & 0x7FFFFFFF) << indexAlignment;
        //        var compressed = (index1 & 0x80000000) >> 31;
        //        var index2 = inputReader.ReadUInt32();
        //        var position2 = (index2 & 0x7FFFFFFF) << indexAlignment;
 
        //        var size = position2 - position1;

        //        if (totalSize + headerIndexSize <= maxFileSize && totalSize + headerIndexSize + 4 + size > maxFileSize)
        //        {
        //            currentPart++;
        //            csoIndexInfosParts.Add(new CsoIndexInfosPart());
        //        }

        //        var csoIndexInfo = new CsoIndexInfo
        //        {
        //            Size = size,
        //            Compressed = compressed == 1
        //        };
        //        csoIndexInfosParts[currentPart].CsoIndexInfos.Add(csoIndexInfo);
        //        csoIndexInfosParts[currentPart].UncompressedSize += (ulong)blockSize;

        //        headerIndexSize += 4;
        //        totalSize += size;
        //    }

        //    for (var i = 0; i < csoIndexInfosParts.Count; i++)
        //    {
        //        var filename = Path.Combine(outputPath, csoIndexInfosParts.Count == 1 ? $"{isoname}.cso" : $"{isoname}.{i + 1}.cso");
        //        using var outputStream = new FileStream(filename, FileMode.Create, FileAccess.Write);
        //        using var outputWriter = new BinaryWriter(outputStream);

        //        var currentCsoIndexInfosPart = csoIndexInfosParts[i];

        //        outputWriter.Write(header);
        //        outputWriter.Write(headerSize);
        //        outputWriter.Write(currentCsoIndexInfosPart.UncompressedSize);
        //        outputWriter.Write(blockSize);
        //        outputWriter.Write(version);
        //        outputWriter.Write(indexAlignment);
        //        outputWriter.Write(unused);

        //        var startPosition = (long)headerSize + ((currentCsoIndexInfosPart.CsoIndexInfos.Count + 1) * 4);
        //        var position = startPosition;
        //        for (var j = 0; j < currentCsoIndexInfosPart.CsoIndexInfos.Count; j++)
        //        {
        //            var currentCsoIndexInfo = currentCsoIndexInfosPart.CsoIndexInfos[j];
        //            var index = (position >> indexAlignment) | (currentCsoIndexInfo.Compressed ? 0x80000000 : 0);
        //            outputWriter.Write((int)index);
        //            position += currentCsoIndexInfo.Size;
        //        }
        //        outputWriter.Write((int)(position >> indexAlignment));

        //        inputStream.Position = startPosition;
        //        for (var j = 0; j < currentCsoIndexInfosPart.CsoIndexInfos.Count; j++)
        //        {
        //            var currentCsoIndexInfo = currentCsoIndexInfosPart.CsoIndexInfos[j];
        //            var buffer = inputReader.ReadBytes((int)currentCsoIndexInfo.Size);
        //            outputWriter.Write(buffer);

        //            if (buffer.Length != 2048)
        //            {
        //                var output = new byte[2048];

        //                //https://create.stephan-brumme.com/smallz4/#git1
        //            }
        //            //if (buffer.Length != 2048)
        //            //{
        //            //    Joveler.Compression.ZLib.ZLibInit.GlobalInit(@"C:\Users\equin\.nuget\packages\joveler.compression.zlib\4.1.0\runtimes\win-x64\native\zlibwapi.dll");
        //            //    ZLibDecompressOptions decompOpts = new ZLibDecompressOptions();
        //            //    decompOpts.WindowBits = ZLibWindowBits.Bits15;

        //            //    using (MemoryStream decompMs = new MemoryStream())
        //            //    using (MemoryStream compFs = new MemoryStream(buffer))
        //            //    {
        //            //        using (var zs = new Joveler.Compression.ZLib.ZLibStream(compFs, decompOpts))
        //            //        {

        //            //            if (true)
        //            //            {
        //            //                byte[] buffer2 = new byte[64 * 1024];
        //            //                int bytesRead;
        //            //                do
        //            //                {
        //            //                    bytesRead = zs.Read(buffer2.AsSpan());
        //            //                    decompMs.Write(buffer2.AsSpan(0, bytesRead));
        //            //                } while (0 < bytesRead);
        //            //            }
        //            //            else
        //            //            {
        //            //                zs.CopyTo(decompMs);
        //            //            }
        //            //        }

        //            //    }

        //            //        //InflateInit2
        //            //        var outputStream2 = new MemoryStream();
        //            //        var inf = new Inflater(true);
        //            //        using (var compressedStream = new MemoryStream(buffer))
        //            //        using (var inputStream2 = new InflaterInputStream(compressedStream, inf))
        //            //        {
        //            //            inputStream.CopyTo(outputStream);


        //            //        }
        //            //        var a = outputStream2.ToArray();


        //            //        byte[] inflated = new byte[4096];



        //            //        //using ZngInflater inflater = new();
        //            //        //var q = inflater.Inflate(buffer, inflated);


        //            //        var buffout = new byte[4096];

        //            //        //var res = z.inflateInit(-15);

        //            //        //z.next_in = buffer;
        //            //        //z.avail_in = buffer.Length;
        //            //        //z.next_out = buffout;
        //            //        //z.avail_out = buffout.Length;

        //            //        //var x = z.inflate(zlibConst.Z_FULL_FLUSH);



        //            //        var xx = 1;
        //            //    }
        //        }

                
        //    }

        //    return true;
        //}
    }
}

