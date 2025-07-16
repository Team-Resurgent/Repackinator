using Resurgent.UtilityBelt.Library.Utilities.ImageInput;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO.Hashing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Resurgent.UtilityBelt.Library.Utilities
{
    public static class XisoUtility
    {
        private struct IndexInfo
        {
            public ulong Value { get; set; }

            public bool Compressed { get; set; }
        }

        private struct TreeNodeInfo
        {
            public uint DirectorySize { get; set; }
            public long DirectoryPos { get; set; }
            public uint Offset { get; set; }
            public string Path { get; set; }
        };

        public struct FileInfo
        {
            public bool IsFile { get; set; }
            public string Path { get; set; }
            public string Filename { get; set; }
            public long Size { get; set; }
            public int StartSector { get; set; }
            public int EndSector { get; set; }
            public string InSlices { get; set; }
        };

        public static HashSet<uint> GetDataSectorsFromXiso(IImageInput input, Action<float>? progress, CancellationToken cancellationToken)
        {
            if (progress != null)
            {
                progress(0);
            }

            var sectorOffset = input.TotalSectors == Constants.RedumpSectors ? Constants.VideoSectors : 0U;

            var dataSectors = new HashSet<uint>();

            var position = 20U;
            var headerSector = (uint)sectorOffset + 0x20U;
            dataSectors.Add(headerSector);
            dataSectors.Add(headerSector + 1);
            position += headerSector << 11;

            var rootSector = input.ReadUint32(position);
            var rootSize = input.ReadUint32(position + 4);
            var rootOffset = (long)rootSector << 11;

            var treeNodes = new List<TreeNodeInfo>
            {
                new TreeNodeInfo
                {
                    DirectorySize = rootSize,
                    DirectoryPos = rootOffset,
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

                var currentPosition = (sectorOffset << 11) + currentTreeNode.DirectoryPos + currentTreeNode.Offset * 4;

                for (var i = currentPosition >> 11; i < (currentPosition >> 11) + ((currentTreeNode.DirectorySize - (currentTreeNode.Offset * 4) + 2047) >> 11); i++)
                {
                    dataSectors.Add((uint)i);
                }

                if ((currentTreeNode.Offset * 4) >= currentTreeNode.DirectorySize)
                {
                    continue;
                }

                var left = input.ReadUint16(currentPosition);
                var right = input.ReadUint16(currentPosition + 2);
                var sector = (long)input.ReadUint32(currentPosition + 4);
                var size = input.ReadUint32(currentPosition + 8);
                var attribute = input.ReadByte(currentPosition + 12);

                var nameLength = input.ReadByte(currentPosition + 13);
                var filenameBytes = input.ReadBytes(currentPosition + 14, nameLength);

                var filename = Encoding.ASCII.GetString(filenameBytes);
                var encoding = CodePagesEncodingProvider.Instance.GetEncoding(1252);
                if (encoding != null)
                {
                    filename = encoding.GetString(filenameBytes);
                }

                if (left == 0xFFFF)
                {
                    continue;
                }

                if (left != 0)
                {
                    treeNodes.Add(new TreeNodeInfo
                    {
                        DirectorySize = currentTreeNode.DirectorySize,
                        DirectoryPos = currentTreeNode.DirectoryPos,
                        Offset = left,
                        Path = currentTreeNode.Path
                    });
                    totalNodes++;
                }

                if ((attribute & 0x10) != 0)
                {
                    if (size > 0)
                    {
                        treeNodes.Add(new TreeNodeInfo
                        {
                            DirectorySize = size,
                            DirectoryPos = sector << 11,
                            Offset = 0,
                            Path = Path.Combine(currentTreeNode.Path, filename)
                        });
                        totalNodes++;
                    }
                }
                else
                {
                    if (size > 0)
                    {
                        for (var i = (sectorOffset + sector); i < (sectorOffset + sector) + ((size + 2047) >> 11); i++)
                        {
                            dataSectors.Add((uint)i);
                        }
                    }
                }

                if (right != 0)
                {
                    treeNodes.Add(new TreeNodeInfo
                    {
                        DirectorySize = currentTreeNode.DirectorySize,
                        DirectoryPos = currentTreeNode.DirectoryPos,
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

            return dataSectors;
        }

        public static void GetFileInfoFromXiso(IImageInput input, Action<FileInfo> info, Action<float>? progress, CancellationToken cancellationToken)
        {
            if (progress != null)
            {
                progress(0);
            }

            var sectorOffset = input.TotalSectors == Constants.RedumpSectors ? Constants.VideoSectors : 0U;

            var position = 20U;
            var headerSector = (uint)sectorOffset + 0x20U;
            position += headerSector << 11;

            var rootSector = input.ReadUint32(position);
            var rootSize = input.ReadUint32(position + 4);
            var rootOffset = (long)rootSector << 11;

            var treeNodes = new List<TreeNodeInfo>
            {
                new TreeNodeInfo
                {
                    DirectorySize = rootSize,
                    DirectoryPos = rootOffset,
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

                var currentPosition = (sectorOffset << 11) + currentTreeNode.DirectoryPos + currentTreeNode.Offset * 4;

                if ((currentTreeNode.Offset * 4) >= currentTreeNode.DirectorySize)
                {
                    continue;
                }

                var left = input.ReadUint16(currentPosition);
                var right = input.ReadUint16(currentPosition + 2);
                var sector = (long)input.ReadUint32(currentPosition + 4);
                var size = input.ReadUint32(currentPosition + 8);
                var attribute = input.ReadByte(currentPosition + 12);

                var nameLength = input.ReadByte(currentPosition + 13);
                var filenameBytes = input.ReadBytes(currentPosition + 14, nameLength);

                var filename = Encoding.ASCII.GetString(filenameBytes);
                var encoding = CodePagesEncodingProvider.Instance.GetEncoding(1252);
                if (encoding != null)
                {
                    filename = encoding.GetString(filenameBytes);
                }

                if (left == 0xFFFF)
                {
                    continue;
                }

                if (left != 0)
                {
                    treeNodes.Add(new TreeNodeInfo
                    {
                        DirectorySize = currentTreeNode.DirectorySize,
                        DirectoryPos = currentTreeNode.DirectoryPos,
                        Offset = left,
                        Path = currentTreeNode.Path
                    });
                    totalNodes++;
                }

                if ((attribute & 0x10) != 0)
                {
                    if (size > 0)
                    {
                        treeNodes.Add(new TreeNodeInfo
                        {
                            DirectorySize = size,
                            DirectoryPos = sector << 11,
                            Offset = 0,
                            Path = Path.Combine(currentTreeNode.Path, filename)
                        });
                        totalNodes++;
                        info(new FileInfo
                        {
                            IsFile = false,
                            Path = Path.Combine(currentTreeNode.Path, filename),
                            Filename = filename,
                            Size = size,
                            StartSector = (int)(sectorOffset + sector),
                            EndSector = (int)((sectorOffset + sector) + ((size + 2047) >> 11) - 1),
                            InSlices = "N/A"
                        });
                    }
                }
                else
                {
                    if (size > 0)
                    {
                        var startSector = (int)(sectorOffset + sector);
                        var endSector = (int)((sectorOffset + sector) + ((size + 2047) >> 11) - 1);
                        var stringBuilder = new StringBuilder();
                        var slices = new List<int>();
                        if (size > 0)
                        {
                            slices.Add(input.SectorInSlice(startSector));
                            slices.Add(input.SectorInSlice(endSector));
                            for (var i = 0; i < slices.Count; i++)
                            {
                                if (i > 0)
                                {
                                    stringBuilder.Append("-");
                                }
                                stringBuilder.Append(slices[i].ToString());
                            }
                        }
                        else
                        {
                            stringBuilder.Append("N/A");
                        }
                        info(new FileInfo
                        {
                            IsFile = true,
                            Path = currentTreeNode.Path,
                            Filename = filename,
                            Size = size,
                            StartSector = size > 0 ? startSector : -1,
                            EndSector = size > 0 ? endSector : -1,
                            InSlices = stringBuilder.ToString()
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
                        DirectorySize = currentTreeNode.DirectorySize,
                        DirectoryPos = currentTreeNode.DirectoryPos,
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

        public static HashSet<uint> GetSecuritySectorsFromXiso(IImageInput input, HashSet<uint> datasecs, bool compareMode, Action<float>? progress, CancellationToken cancellationToken)
        {
            var sectorOffset = input.TotalSectors == Constants.RedumpSectors ? Constants.VideoSectors : 0U;

            var securitySectors = new HashSet<uint>();            
            if (input.TotalSectors != Constants.RedumpSectors && input.TotalSectors != Constants.IsoSectors)
            {
                return securitySectors;
            }

            if (progress != null)
            {
                progress(0);
            }

            var flag = false;
            var start = 0U;

            const uint endSector = 0x345B60;

            for (uint sectorIndex = 0; sectorIndex <= endSector; sectorIndex++)
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
                else if (isEmptySector == false && flag == true)
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
                    else if (compareMode && end - start > 0xFFF)      // if more than 0xFFF, we "guess" this image is scrubbed so we stop
                    {
                        if (progress != null)
                        {
                            progress(100);
                        }
                        return securitySectors;
                    }
                }

                if (progress != null)
                {
                    progress(sectorIndex / (float)endSector);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            return securitySectors;
        }

        public static string GetChecksumFromXiso(IImageInput input, Action<float>? progress, CancellationToken cancellationToken)
        {
            if (progress != null)
            {
                progress(0);
            }

            using var hash = SHA256.Create();
            for (var i = 0; i < input.TotalSectors; i++)
            {
                var buffer = input.ReadSectors(i, 1);
                hash.TransformBlock(buffer, 0, buffer.Length, null, 0);
                if (progress != null)
                {
                    progress(i / (float)input.TotalSectors);
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

        public static bool TryGetDefaultXbeFromXiso(IImageInput input, ref byte[] xbeData)
        {
            var sectorOffset = input.TotalSectors == Constants.RedumpSectors ? Constants.VideoSectors : 0U;

            var position = 20U;
            var headerSector = (uint)sectorOffset + 0x20U;
            position += headerSector << 11;

            var rootSector = input.ReadUint32(position);
            var rootSize = input.ReadUint32(position + 4);
            var rootOffset = (long)rootSector << 11;

            var treeNodes = new List<TreeNodeInfo>
            {
                new TreeNodeInfo
                {
                    DirectorySize = rootSize,
                    DirectoryPos = rootOffset,
                    Offset = 0,
                    Path = string.Empty
                }
            };

            while (treeNodes.Count > 0)
            {
                var currentTreeNode = treeNodes[0];
                treeNodes.RemoveAt(0);

                var currentPosition = (sectorOffset << 11) + currentTreeNode.DirectoryPos + currentTreeNode.Offset * 4;

                if ((currentTreeNode.Offset * 4) >= currentTreeNode.DirectorySize)
                {
                    continue;
                }

                var left = input.ReadUint16(currentPosition);
                var right = input.ReadUint16(currentPosition + 2);
                var sector = input.ReadUint32(currentPosition + 4);
                var size = input.ReadUint32(currentPosition + 8);
                var attribute = input.ReadByte(currentPosition + 12);

                var nameLength = input.ReadByte(currentPosition + 13);
                var filenameBytes = input.ReadBytes(currentPosition + 14, nameLength);
   
                var filename = Encoding.ASCII.GetString(filenameBytes);
                var encoding = CodePagesEncodingProvider.Instance.GetEncoding(1252);
                if (encoding != null)
                {
                    filename = encoding.GetString(filenameBytes);
                }

                if ((attribute & 0x10) == 0 && filename.Equals("default.xbe", StringComparison.CurrentCultureIgnoreCase))
                {
                    var result = new byte[size];
                    var processed = 0U;
                    while (processed < size)
                    {
                        var buffer = input.ReadSectors(sector + sectorOffset, 1);
                        var bytesToCopy = Math.Min(size - processed, 2048);
                        Array.Copy(buffer, 0, result, processed, bytesToCopy);
                        sector++;
                        processed += bytesToCopy;
                    }
                    xbeData = result;
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
                        DirectorySize = currentTreeNode.DirectorySize,
                        DirectoryPos = currentTreeNode.DirectoryPos,
                        Offset = left,
                        Path = currentTreeNode.Path
                    });
                }

                if (right != 0)
                {
                    treeNodes.Add(new TreeNodeInfo
                    {
                        DirectorySize = currentTreeNode.DirectorySize,
                        DirectoryPos = currentTreeNode.DirectoryPos,
                        Offset = right,
                        Path = currentTreeNode.Path
                    });
                }
            }

            return false;
        }

        //https://github.com/Qubits01/xbox_shrinker

        public static void CompareXISO(IImageInput input1, IImageInput input2, Action<string> log, Action<float>? progress)
        {
            var sectorOffset1 = input1.TotalSectors == Constants.RedumpSectors ? Constants.VideoSectors : 0U;
            if (sectorOffset1 > 0)
            {
                log("First contains a video partition, compare will ignore those sectors.");
            }

            var sectorOffset2 = input2.TotalSectors == Constants.RedumpSectors ? Constants.VideoSectors : 0U;
            if (sectorOffset2 > 0)
            {
                log("Second contains a video partition, compare will ignore those sectors.");
            }

            if (input1.TotalSectors - sectorOffset1 != input2.TotalSectors - sectorOffset2)
            {
                log("Expected sector counts do not match, assuming image could be trimmed.");
            }

            //var flag = false;
            //var startRange = 0L;
            //var endRange = 0L;
            //for (var i = 0; i < input1.TotalSectors - input1.SectorOffset; i++)
            //{
            //    var buffer1 = new byte[2048];
            //    var buffer2 = new byte[2048];

            //    if (i < input1.TotalSectors)
            //    {
            //        buffer1 = input1.ReadSectors(i + input1.SectorOffset, 1);
            //    }

            //    if (i < input2.TotalSectors) 
            //    { 
            //        buffer2 = input2.ReadSectors(i + input2.SectorOffset, 1);
            //    }

            //    var same = true;
            //    for (var j = 0; j < 2048; j++)
            //    {
            //        if (buffer1[j] != buffer2[j])
            //        {
            //            same = false;
            //            break;
            //        }
            //    }

            //    endRange = i;
            //    if (!same)
            //    {
            //        if (!flag)
            //        {
            //            startRange = i;
            //            flag = true;
            //        }
            //    }
            //    else if (flag)
            //    {
            //        log($"Game partition sectors in range {startRange}-{endRange} (Redump range {startRange + (Constants.VideoSectors - input1.SectorOffset)}-{endRange + (Constants.VideoSectors - input1.SectorOffset)}) are different.");
            //        flag = false;
            //    }

            //    if (progress != null)
            //    {
            //        progress(i / (float)(input1.TotalSectors - input1.SectorOffset));
            //    }
            //}

            //if (flag)
            //{
            //    log($"Game partition sectors in range {startRange}-{endRange} (Redump range {startRange + (Constants.VideoSectors - input1.SectorOffset)}-{endRange + (Constants.VideoSectors - input1.SectorOffset)}) are different.");
            //}

            log("");

            log("Getting data sectors hash for first...");
            var dataSectors1 = GetDataSectorsFromXiso(input1, progress, default);
            var dataSectors1Array = dataSectors1.ToArray();
            Array.Sort(dataSectors1Array);

            log("Calculating data sector hashes for first...");
            var dataSectorsHash1 = new XxHash64();
            for (var i = 0; i < dataSectors1Array.Length; i++)
            {
                var dataSector1 = dataSectors1Array[i];
                var buffer = input1.ReadSectors(dataSector1, 1);
                dataSectorsHash1.Append(buffer);
                if (progress != null)
                {
                    progress(i / (float)dataSectors1Array.Length);
                }
            }
            var dataChecksum1 = dataSectorsHash1.GetCurrentHash();
            if (dataChecksum1 == null)
            {
                throw new ArgumentOutOfRangeException();
            }
            var dataSectorsHash1Result = BitConverter.ToString(dataChecksum1).Replace("-", string.Empty);

            log("Getting data sectors hash for second...");
            var dataSectors2 = GetDataSectorsFromXiso(input2, progress, default);
            var dataSectors2Array = dataSectors2.ToArray();
            Array.Sort(dataSectors2Array);

            log("Calculating data sector hash for second...");
            var dataSectorsHash2 = new XxHash64();
            for (var i = 0; i < dataSectors2Array.Length; i++)
            {
                var dataSector2 = dataSectors2Array[i];
                var buffer = input2.ReadSectors(dataSector2, 1);
                dataSectorsHash2.Append(buffer);
                if (progress != null)
                {
                    progress(i / (float)dataSectors2Array.Length);
                }
            }
            var dataChecksum2 = dataSectorsHash2.GetCurrentHash();
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
            log("");

            log("Getting security sectors for first...");
            var securitySectors1 = GetSecuritySectorsFromXiso(input1, dataSectors1, true, progress, default).ToArray();
            Array.Sort(securitySectors1);

            log("Getting security sectors for second...");
            var securitySectors2 = GetSecuritySectorsFromXiso(input2, dataSectors2, true, progress, default).ToArray();
            Array.Sort(securitySectors2);

            if (securitySectors1.Length > 0 && securitySectors2.Length > 0)
            {
                // TO-DO: do array compare without reading sectors since we know they are empty sectors already, taking into account redump video offset eventually
            }

            log("");
            log("");

            var securitySectorsCompare = securitySectors1.Length > securitySectors2.Length ? securitySectors1 : securitySectors2;   // we use the array with the most security sectors to check against
            var minTotalSectors = Math.Min(input1.TotalSectors, input2.TotalSectors);
            log($"Minimum total sectors to compare = {minTotalSectors}");

            log("");

            log("Calculating security sector hashes for first...");
            var securitySectorsHash1 = new XxHash64();
            long firstSecuritySector = -1;
            long lastSecuritySector = -1;
            int securitySectorsCount = 0;
            var hashedSectorsCount1 = 0;
            for (var i = 0; i < securitySectorsCompare.Length; i++)
            {
                long securitySector1;
                if (securitySectors1 == securitySectorsCompare)
                {
                    securitySector1 = securitySectors1[i];
                    lastSecuritySector = i > 0 ? securitySectors1[i - 1] : lastSecuritySector;
                }
                else if (input1.TotalSectors != Constants.RedumpSectors)
                {
                    securitySector1 = securitySectorsCompare[i] - (uint)sectorOffset2;
                    lastSecuritySector = i > 0 ? securitySectorsCompare[i - 1] - (uint)sectorOffset2 : lastSecuritySector;
                }
                else if (input2.TotalSectors != Constants.RedumpSectors)    
                {
                    securitySector1 = securitySectorsCompare[i] + (uint)sectorOffset1;
                    lastSecuritySector = i > 0 ? securitySectorsCompare[i - 1] - (uint)sectorOffset1 : lastSecuritySector;
                }
                else
                {
                    securitySector1 = securitySectorsCompare[i];
                    lastSecuritySector = i > 0 ? securitySectorsCompare[i - 1] : lastSecuritySector;
                }

                if (i == 0)
                {
                    firstSecuritySector = securitySector1;
                }

                if (securitySector1 >= input1.TotalSectors)     // stop the cycle when reached the end of input1 file
                {
                    securitySectorsCount = i;
                    if (progress != null)
                    {
                        progress(100);
                    }
                    break;
                }

                if (securitySector1 < minTotalSectors + sectorOffset1)        // only hash sectors that are available to hash in the input that has lower file size
                {
                    var buffer = input1.ReadSectors(securitySector1, 1);
                    securitySectorsHash1.Append(buffer);
                    if (progress != null)
                    {
                        progress(i / (float)securitySectorsCompare.Length);
                    }
                    hashedSectorsCount1++;
                }

                if (i == securitySectorsCompare.Length - 1)
                {
                    lastSecuritySector = securitySector1;
                    securitySectorsCount = i + 1;
                }
            }
            var secutityChecksum1 = securitySectorsHash1.GetCurrentHash();
            if (secutityChecksum1 == null)
            {
                throw new ArgumentOutOfRangeException();
            }
            var securitySectorsHash1Result = BitConverter.ToString(secutityChecksum1).Replace("-", string.Empty);

            log($"First image (first - last) Security Sector: {(firstSecuritySector < 0 ? "N/A" : firstSecuritySector)} - {(lastSecuritySector < 0 ? "N/A" : lastSecuritySector)}");
            log($"First image Security Sectors count: {(securitySectorsCount < 0 ? "N/A" : securitySectorsCount)}");
            log($"First image number of hashed sectors: {hashedSectorsCount1}");

            log("");

            log("Calculating security sector hashes for second...");
            var securitySectorsHash2 = new XxHash64();
            firstSecuritySector = -1;
            lastSecuritySector = -1;
            securitySectorsCount = 0;
            var hashedSectorsCount2 = 0;
            for (var i = 0; i < securitySectorsCompare.Length; i++)
            {
                long securitySector2;
                if (securitySectors2 == securitySectorsCompare)
                {
                    securitySector2 = securitySectors2[i];
                    lastSecuritySector = i > 0 ? securitySectors2[i - 1] : lastSecuritySector;
                }
                else if (input2.TotalSectors != Constants.RedumpSectors)
                {
                    securitySector2 = securitySectorsCompare[i] - (uint)sectorOffset1;
                    lastSecuritySector = i > 0 ? securitySectorsCompare[i - 1] - (uint)sectorOffset1 : lastSecuritySector;
                }
                else if (input1.TotalSectors != Constants.RedumpSectors)
                {
                    securitySector2 = securitySectorsCompare[i] + (uint)sectorOffset2;
                    lastSecuritySector = i > 0 ? securitySectorsCompare[i - 1] - (uint)sectorOffset2 : lastSecuritySector;
                }
                else
                {
                    securitySector2 = securitySectorsCompare[i];
                    lastSecuritySector = i > 0 ? securitySectorsCompare[i - 1] : lastSecuritySector;
                }

                if (i == 0)
                {
                    firstSecuritySector = securitySector2;
                }

                if (securitySector2 >= input2.TotalSectors)
                {
                    securitySectorsCount = i;
                    if (progress != null)
                    {
                        progress(100);
                    }
                    break;
                }

                if (securitySector2 < minTotalSectors + sectorOffset2)
                {
                    var buffer = input2.ReadSectors(securitySector2, 1);
                    securitySectorsHash2.Append(buffer);
                    if (progress != null)
                    {
                        progress(i / (float)securitySectorsCompare.Length);
                    }
                    hashedSectorsCount2++;
                }

                if (i == securitySectorsCompare.Length - 1)
                {
                    lastSecuritySector = securitySector2;
                    securitySectorsCount = i + 1;
                }
            }
            var secutityChecksum2 = securitySectorsHash2.GetCurrentHash();
            if (secutityChecksum2 == null)
            {
                throw new ArgumentOutOfRangeException();
            }
            var securitySectorsHash2Result = BitConverter.ToString(secutityChecksum2).Replace("-", string.Empty);

            log($"Second image (first - last) Security Sector: {(firstSecuritySector < 0 ? "N/A" : firstSecuritySector)} - {(lastSecuritySector < 0 ? "N/A" : lastSecuritySector)}");
            log($"Second image Security Sectors count: {(securitySectorsCount < 0 ? "N/A" : securitySectorsCount)}");
            log($"Second image number of hashed sectors: {hashedSectorsCount2}");

            log("");

            if (hashedSectorsCount1 == 0 || hashedSectorsCount2 == 0)
            {
                log("Couldn't hash one or both images, possible reason: impossible to determine presence of security sectors.");
            }
            else if (securitySectorsHash1Result == securitySectorsHash2Result)
            {
                log("Hashed Security sectors match.");
            }
            else
            {
                log("Hashed Security sectors do not match.");
            }

            log("");

            log($"First image data sectors range: {dataSectors1Array.First()} - {dataSectors1Array.Last()}");
            log($"Second image data sectors range: {dataSectors2Array.First()} - {dataSectors2Array.Last()}");
            log("");
        }

        public static bool Split(IImageInput input, string outputPath, string name, string extension, bool scrub, bool trimmedScrub, bool noSplit, Action<int, float>? progress, CancellationToken cancellationToken)
        {
            if (progress != null)
            {
                progress(0, 0);
            }

            var sectorOffset = input.TotalSectors == Constants.RedumpSectors ? Constants.VideoSectors : 0U;

            Action<float> progress1 = (percent) => {
                if (progress != null)
                {
                    progress(0, percent);
                }
            };

            Action<float> progress2 = (percent) => {
                if (progress != null)
                {
                    progress(1, percent);
                }
            };

            var endSector = input.TotalSectors;
            var dataSectors = new HashSet<uint>();
            if (scrub)
            {
                dataSectors = GetDataSectorsFromXiso(input, progress1, cancellationToken);

                if (trimmedScrub)
                {
                    endSector = Math.Min(dataSectors.Max() + 1, input.TotalSectors);
                }

                var securitySectors = GetSecuritySectorsFromXiso(input, dataSectors, false, progress2, cancellationToken).ToArray();
                for (var i = 0; i < securitySectors.Length; i++)
                {
                    dataSectors.Add(securitySectors[i]);
                }
            }

            var sectorSplit = (uint)(endSector - sectorOffset) / 2;

            var partStream = new FileStream(Path.Combine(outputPath, $"{name}.1{extension}"), FileMode.Create, FileAccess.Write, FileShare.None, 2048 * 4096);
            var partWriter = new BinaryWriter(partStream);

            var emptySector = new byte[2048];
            var hasSplit = false;

            try
            {

                for (var i = (uint)sectorOffset; i < endSector; i++)
                {
                    if (noSplit == false && hasSplit == false && i - sectorOffset >= sectorSplit)
                    {
                        hasSplit = true;
                        partWriter.Dispose();
                        partStream.Dispose();
                        partStream = new FileStream(Path.Combine(outputPath, $"{name}.2{extension}"), FileMode.Create, FileAccess.Write, FileShare.None, 2048 * 4096);
                        partWriter = new BinaryWriter(partStream);
                    }

                    var writeSector = true;
                    if (scrub)
                    {
                        writeSector = dataSectors.Contains(i);
                    }
                    if (writeSector == true)
                    {
                        var sectorBuffer = input.ReadSectors(i, 1);
                        partWriter.Write(sectorBuffer);
                    }
                    else
                    {
                        partWriter.Write(emptySector);
                    }

                    if (progress != null)
                    {
                        progress(2, i / (float)(endSector - sectorOffset));
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
            finally
            {
                partWriter.Dispose();
                partStream.Dispose();
            }

            return true;
        }

        public static bool CreateCCI(IImageInput input, string outputPath, string name, string extension, bool scrub, bool trimmedScrub, Action<int, float>? progress, CancellationToken cancellationToken)
        {
            if (progress != null)
            {
                progress(0, 0);
            }

            Action<float> progress1 = (percent) => {
                if (progress != null)
                {
                    progress(0, percent);
                }
            };

            Action<float> progress2 = (percent) => {
                if (progress != null)
                {
                    progress(1, percent);
                }
            };

            var endSector = input.TotalSectors;
            var dataSectors = new HashSet<uint>();
            if (scrub)
            {
                dataSectors = GetDataSectorsFromXiso(input, progress1, cancellationToken);

                if (trimmedScrub)
                {
                    endSector = Math.Min(dataSectors.Max() + 1, input.TotalSectors);
                }

                var securitySectors = GetSecuritySectorsFromXiso(input, dataSectors, false, progress2, cancellationToken).ToArray();
                for (var i = 0; i < securitySectors.Length; i++)
                {
                    dataSectors.Add(securitySectors[i]);
                }
            }

            var sectorOffset = input.TotalSectors == Constants.RedumpSectors ? Constants.VideoSectors : 0U;

            var splitMargin = 0xFF000000L;
            var emptySector = new byte[2048];
            var compressedData = new byte[2048];
            var sectorsWritten = (uint)sectorOffset;
            var iteration = 0;

            while (sectorsWritten < endSector)
            {
                var indexInfos = new List<IndexInfo>();

                var outputFile = Path.Combine(outputPath, iteration > 0 ? $"{name}.{iteration + 1}{extension}" : $"{name}{extension}");
                var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None, 2048 * 4096);
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
                while (sectorsWritten < endSector)
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
                        progress(2, (sectorsWritten - sectorOffset) / (float)(endSector - sectorOffset));
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

            using var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None, 2048 * 4096);
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

