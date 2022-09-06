using System.Text;

namespace Resurgent.UtilityBelt.Library.Utilities
{
    public static class XisoUtility
    {
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

        public static bool Split(string input, string outputPath, string isoname, bool removeVideoPartition, Action<float>? progress, CancellationToken cancellationToken)
        {
            using var fs = new FileStream(input, FileMode.Open, FileAccess.Read);
            fs.Seek(0, SeekOrigin.End);

            var fileLength = fs.Position;
            if (fileLength % 2048 > 0)
            {
                return false;
            }
            fs.Position = 0;

            const long dvdSize = 7825162240;
            long videoSize = 387 * 1024 * 1024;
            long skipSize = (removeVideoPartition == true && fileLength == dvdSize) ? videoSize : 0;
            long sectorSplit = ((fileLength - skipSize) / 4096) * 2048;

            var parts = (fileLength - skipSize) / sectorSplit;
            if ((fileLength - skipSize) % sectorSplit > 0)
            {
                parts++;
            }

            var fileParts = new List<FileStream>();
            for (var i = 0; i < parts; i++)
            {
                var filename = parts == 1 ? $"{isoname}.iso" : $"{isoname}.{i + 1}.iso";
                fileParts.Add(new FileStream(Path.Combine(outputPath, filename), FileMode.Create, FileAccess.Write));
            }

            fs.Position = skipSize;

            byte[] buffer = new byte[32768];

            if (progress != null)
            {
                progress(0);
            }

            for (var i = 0; i < parts; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var bytesRead = 0L;

                while (bytesRead < sectorSplit)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    var bytesToRead = (int)Math.Min(buffer.Length, sectorSplit - bytesRead); 
                    var chunkRead = fs.Read(buffer, 0, bytesToRead);
                    bytesRead += chunkRead;
                    fileParts[i].Write(buffer, 0, chunkRead);

                    if (progress != null)
                    {
                        progress((bytesRead + (i * sectorSplit)) / (float)(fileLength - skipSize));
                    }
                }

            }

            for (var i = 0; i < parts; i++)
            {
                fileParts[i].Dispose();
            }

            return true; 
        }
    }
}
