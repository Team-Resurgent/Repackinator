using ICSharpCode.SharpZipLib;
using Resurgent.UtilityBelt.Library.Utilities.Xiso;
using System.Data;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

//0x18300000U video size
//0x1D26A8000 redump size 0x3A4D50 sectors
//0x1BA3A8000 iso size 0x374750 sectors

namespace Resurgent.UtilityBelt.Library.Utilities
{    
    public static class XisoUtility
    {
        public const long VideoSectors = 0x30600;

        public const long RedumpSectors = 0x3A4D50;

        public const long IsoSectors = 0x374750;

        private struct IndexInfo
        {
            public ulong Value { get; set; }

            public bool Compressed { get; set; }
        }

        private struct TreeNodeInfo
        {
            public uint DirectorySize { get; set; }
            public long DirectorySector { get; set; }
            public uint Offset { get; set; }
        };

        public static HashSet<uint> GetDataSectorsFromXiso(IImageInput input)
        {
            var dataSectors = new HashSet<uint>();

            var position = 20U;
            var sectorOffset = input.TotalSectors == RedumpSectors ? VideoSectors : 0U;
            var headerSector = (uint)sectorOffset + 0x20U;
            if (input.TotalSectors == RedumpSectors) { 
                dataSectors.Add(headerSector);
                dataSectors.Add(headerSector + 1);                
            }
            position += (headerSector << 11);

            var rootSector = input.ReadUint32(position);
            var rootSize = input.ReadUint32(position + 4);
            var rootOffset = (long)rootSector << 11;

            var treeNodes = new List<TreeNodeInfo>
            {
                new TreeNodeInfo
                {
                    DirectorySize = rootSize,
                    DirectorySector = rootOffset,
                    Offset = 0
                }
            };

            while (treeNodes.Count > 0)
            {
                var currentTreeNode = treeNodes[0];

                var currentOffset = (sectorOffset << 11) + currentTreeNode.DirectorySector + currentTreeNode.Offset * 4;

                for (var i = currentOffset >> 11; i < (currentOffset >> 11) + ((currentTreeNode.DirectorySize - (currentTreeNode.Offset * 4) + 2047) >> 11); i++)
                {
                    dataSectors.Add((uint)i);
                }

                if ((currentTreeNode.Offset * 4) >= currentTreeNode.DirectorySize)
                {
                    treeNodes.RemoveAt(0);
                    continue;
                }

                position = (uint)currentOffset;

                var left = input.ReadUint16(position);
                var right = input.ReadUint16(position + 2);
                var sector = input.ReadUint32(position + 4);
                var size = input.ReadUint32(position + 8);
                var attribute = input.ReadByte(position + 12);

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
                        DirectorySector = currentTreeNode.DirectorySector,
                        Offset = left
                    });
                }

                if ((attribute & 0x10) != 0)
                {
                    if (size > 0)
                    {
                        treeNodes.Add(new TreeNodeInfo
                        {
                            DirectorySize = size,
                            DirectorySector = sector << 11,
                            Offset = 0
                        });
                    }
                }
                else
                {
                    for (var i = (sectorOffset + sector); i < (sectorOffset + sector) + ((size + 2047) >> 11); i++)
                    {
                        dataSectors.Add((uint)i);
                    }
                }

                if (right != 0)
                {
                    treeNodes.Add(new TreeNodeInfo
                    {
                        DirectorySize = currentTreeNode.DirectorySize,
                        DirectorySector = currentTreeNode.DirectorySector,
                        Offset = right
                    });
                }

                treeNodes.RemoveAt(0);
            }

            return dataSectors;
        }

        public static HashSet<uint> GetSecuritySectorsFromXiso(IImageInput input, HashSet<uint> datasecs)
        {
            var securitySectors = new HashSet<uint>();            
            if (input.TotalSectors != RedumpSectors && input.TotalSectors != IsoSectors)
            {
                return datasecs;
            }
                        
            var flag = false;
            var start = 0U;

            var sectorOffset = input.TotalSectors == RedumpSectors ? 0x30600U : 0U;
            for (var sectorIndex = 0; sectorIndex <= 0x345B60; sectorIndex++)
            {
                var currentSector = (uint)(sectorOffset + sectorIndex);

                byte[] sectorBuffer = input.ReadSectors(currentSector, 1);

                var isEmptySector = true;
                for (var i = 0; i < sectorBuffer.Length; i++)
                {
                    if (sectorBuffer[i] != 0)
                    {
                        isEmptySector = false;
                        break;
                    }
                }

                var isDataSector = datasecs.Contains(currentSector);
                if (isEmptySector == true && flag == false && !isDataSector)
                {
                    start = currentSector;
                    flag = true;
                }
                else if (isEmptySector == false)
                {
                    var end = currentSector - 1;
                    flag = false;
                    if (end - start == 0xFFF)
                    {
                        for (var i = start; i <= end; i++)
                        {
                            securitySectors.Add(i);
                        }
                    }
                }
            }

            return securitySectors;
        }

        public static bool TryGetDefaultXbeFromXiso(IImageInput input, ref byte[] xbeData)
        {
            var position = 20U;
            var sectorOffset = input.TotalSectors == RedumpSectors ? VideoSectors : 0U;
            var headerSector = (uint)sectorOffset + 0x20U;
            position += (headerSector << 11);

            var rootSector = input.ReadUint32(position);
            var rootSize = input.ReadUint32(position + 4);
            var rootOffset = (long)rootSector << 11;

            var treeNodes = new List<TreeNodeInfo>
            {
                new TreeNodeInfo
                {
                    DirectorySize = rootSize,
                    DirectorySector = rootOffset,
                    Offset = 0
                }
            };

            while (treeNodes.Count > 0)
            {
                var currentTreeNode = treeNodes[0];

                var currentOffset = (sectorOffset << 11) + currentTreeNode.DirectorySector + currentTreeNode.Offset * 4;

                if ((currentTreeNode.Offset * 4) >= currentTreeNode.DirectorySize)
                {
                    treeNodes.RemoveAt(0);
                    continue;
                }

                position = (uint)currentOffset;

                var left = input.ReadUint16(position);
                var right = input.ReadUint16(position + 2);
                var sector = input.ReadUint32(position + 4);
                var size = input.ReadUint32(position + 8);
                var attribute = input.ReadByte(position + 12);

                var nameLength = input.ReadByte(position + 13);
                var filenameBytes = input.ReadBytes(position + 14, nameLength);
                var filename = Encoding.ASCII.GetString(filenameBytes);

                if ((attribute & 0x10) == 0 && filename.Equals("default.xbe", StringComparison.CurrentCultureIgnoreCase))
                {
                    var result = new byte[size];
                    var processed = 0U;
                    while (processed < size)
                    {
                        var buffer = input.ReadSectors(sector, 1);
                        var bytesToCopy = (uint)Math.Min(size - processed, 2048);
                        Array.Copy(buffer, 0, result, processed, bytesToCopy);
                        sector++;
                        processed += bytesToCopy;
                    }
                    xbeData = result;
                    return true;
                }

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
                        DirectorySector = currentTreeNode.DirectorySector,
                        Offset = left
                    });
                }

                if (right != 0)
                {
                    treeNodes.Add(new TreeNodeInfo
                    {
                        DirectorySize = currentTreeNode.DirectorySize,
                        DirectorySector = currentTreeNode.DirectorySector,
                        Offset = right
                    });
                }

                treeNodes.RemoveAt(0);
            }

            return false;
        }

        //https://github.com/Qubits01/xbox_shrinker

        public static bool CompareXISO(IImageInput input1, IImageInput input2)
        {

            if (input1.TotalSectors != input2.TotalSectors)
            {
                System.Diagnostics.Debug.WriteLine($"Sector count is different.");
                return false;
            }

            var sectorOffset = input1.TotalSectors == RedumpSectors ? 0x30600U : 0U;

            for (var i = 0; i < input1.TotalSectors; i++)
            {
                var buffer1 = input1.ReadSectors(i, 1);
                var buffer2 = input2.ReadSectors(i, 1);

                var same = true;
                for (var j = 0; j < 2048; j++)
                {
                    if (buffer1[j] != buffer2[j])
                    {
                        same = false;
                        break;
                    }
                }

                if (i <= sectorOffset && !same)
                {
                    System.Diagnostics.Debug.WriteLine($"Ignoring sector {i} as different but doesnt matter.");
                }
                else if (!same)
                {
                    System.Diagnostics.Debug.WriteLine($"Sector {i} is different.");
                    return false;
                }
            }

            return true;
        }

        public static bool Split(IImageInput input, string outputPath, string name, string extension, bool scrub, Action<float>? progress, CancellationToken cancellationToken)
        {
            if (progress != null)
            {
                progress(0);
            }

            var dataSectors = new HashSet<uint>();
            if (scrub)
            {
                dataSectors = GetDataSectorsFromXiso(input);
                var securitySectors = GetSecuritySectorsFromXiso(input, dataSectors);
                for (var i = 0; i < securitySectors.Count; i++)
                {
                    dataSectors.Add(securitySectors.ElementAt(i));
                }
            }

            var sectorOffset = input.TotalSectors == RedumpSectors ? 0x30600U : 0U;
            var sectorSplit = (uint)(input.TotalSectors - sectorOffset) / 2;

            using var partStream1 = new FileStream(Path.Combine(outputPath, $"{name}.1{extension}"), FileMode.Create, FileAccess.Write);
            using var partWriter1 = new BinaryWriter(partStream1);

            using var partStream2 = new FileStream(Path.Combine(outputPath, $"{name}.2{extension}"), FileMode.Create, FileAccess.Write);
            using var partWriter2 = new BinaryWriter(partStream2);

            var emptySector = new byte[2048];

            for (var i = sectorOffset; i < input.TotalSectors; i++)
            {
                var currentWriter = i - sectorOffset >= sectorSplit ? partWriter2 : partWriter1;
               
                var writeSector = true;
                if (scrub)
                {
                    writeSector = dataSectors.Contains(i);
                }
                if (writeSector == true)
                {
                    var sectorBuffer = input.ReadSectors(i, 1);
                    currentWriter.Write(sectorBuffer);
                }
                else
                {
                    currentWriter.Write(emptySector);
                }

                if (progress != null)
                {
                    progress(i / (float)(input.TotalSectors - sectorOffset));
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            return true;
        }

        public static bool CreateCCI(IImageInput input, string outputPath, string name, string extension, bool scrub, Action<float>? progress, CancellationToken cancellationToken)
        {
            if (progress != null)
            {
                progress(0);
            }

            var dataSectors = new HashSet<uint>();
            if (scrub)
            {
                dataSectors = GetDataSectorsFromXiso(input);
                var securitySectors = GetSecuritySectorsFromXiso(input, dataSectors);
                for (var i = 0; i < securitySectors.Count; i++)
                {
                    dataSectors.Add(securitySectors.ElementAt(i));
                }
            }

            var sectorOffset = input.TotalSectors == RedumpSectors ? 0x30600U : 0U;

            var splitMargin = 0xFF000000L;
            var emptySector = new byte[2048];
            var compressedData = new byte[2048];
            var sectorsWritten = sectorOffset;
            var iteration = 0;

            while (sectorsWritten < input.TotalSectors)
            {
                var indexInfos = new List<IndexInfo>();

                var outputFile = Path.Combine(outputPath, iteration > 0 ? $"{name}.{iteration + 1}{extension}" : $"{name}{extension}");
                var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
                var outputWriter = new BinaryWriter(outputStream);

                uint header = 0x4D494343U;
                outputWriter.Write(header);

                uint headerSize = 32;
                outputWriter.Write(headerSize);

                ulong uncompressedSize = (ulong)0;
                outputWriter.Write(uncompressedSize);

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

                var splitting = false;
                var sectorCount = 0U;
                while (sectorsWritten < input.TotalSectors)
                {
                    var writeSector = true;
                    if (scrub)
                    {
                        writeSector = dataSectors.Contains(sectorsWritten);
                    }

                    var sectorToWrite = writeSector == true ? input.ReadSectors(sectorsWritten, 1) :  emptySector;              

                    var compressedSize = K4os.Compression.LZ4.LZ4Codec.Encode(sectorToWrite, compressedData, K4os.Compression.LZ4.LZ4Level.L12_MAX);
                    if (compressedSize > 0 && compressedSize < (2048 - (4 + (1 << indexAlignment))))
                    {
                        var multiple = (1 << indexAlignment);
                        var padding = ((compressedSize + 1 + multiple - 1) / multiple * multiple) - (compressedSize + 1);
                        outputWriter.Write((byte)padding);
                        outputWriter.Write(compressedData, 0, compressedSize);
                        if (padding != 0)
                        {
                            outputWriter.Write(new byte[padding]);
                        }
                        indexInfos.Add(new IndexInfo { Value = (ushort)(compressedSize + 1 + padding), Compressed = true });
                    }
                    else
                    {
                        outputWriter.Write(sectorToWrite);
                        indexInfos.Add(new IndexInfo { Value = 2048, Compressed = false });
                    }

                    uncompressedSize += 2048;
                    sectorsWritten++;
                    sectorCount++;

                    if (outputStream.Position > splitMargin)
                    {
                        splitting = true;
                        break;
                    }

                    if (progress != null)
                    {
                        progress(sectorsWritten / (float)(input.TotalSectors - sectorOffset));
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

                outputStream.Position = 8;
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

            uint headerSize = inputReader.ReadUInt32();
            if (headerSize != 32)
            {
                return false;
            }

            ulong uncompressedSize = inputReader.ReadUInt64();

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
                    var padding = inputReader.ReadByte();
                    var buffer = inputReader.ReadBytes(size);
                    var compressedSize = K4os.Compression.LZ4.LZ4Codec.Decode(buffer, 0, size - (padding + 1), decodeBuffer, 0, 2048);
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
    }
}

