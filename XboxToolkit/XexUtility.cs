using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using XboxToolkit.Internal;
using XboxToolkit.Internal.Models;
using XboxToolkit.Models;

namespace XboxToolkit
{
    public static partial class XexUtility
    {
        private static readonly byte[] XexDevkitKey = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private static readonly byte[] XexRetailKey = { 0x20, 0xB1, 0x85, 0xA5, 0x9D, 0x28, 0xFD, 0xC3, 0x40, 0x58, 0x3F, 0xBB, 0x08, 0x96, 0xBF, 0x91 };

        private static readonly uint XexExecutionId = 0x400;
        private static readonly uint XexHeaderSectionTableId = 0x2;
        private static readonly uint XexFileDataDescriptorId = 0x3;
        private static readonly uint XexDataFlagEncrypted = 0x1;
        private static readonly uint XexDataFormatRaw = 0x1;
        private static readonly uint XexDataFormatCompressed = 0x2;
        private static readonly uint XexDataFormatDeltaCompressed = 0x3;

        private static readonly uint XSRC = 0x58535243;

        private static bool SearchField<T>(BinaryReader binaryReader, XexHeader header, uint searchId, out T result) where T : struct
        {
            result = default;

            binaryReader.BaseStream.Position = Helpers.SizeOf<XexHeader>();
            var headerDirectoryEntryCount = Helpers.ConvertEndian(header.HeaderDirectoryEntryCount);

            for (var i = 0; i < headerDirectoryEntryCount; i++)
            {
                var value = Helpers.ConvertEndian(binaryReader.ReadUInt32());
                var offset = Helpers.ConvertEndian(binaryReader.ReadUInt32());
                if (value != searchId)
                {
                    continue;
                }
                binaryReader.BaseStream.Position = offset;
                result = Helpers.ByteToType<T>(binaryReader);
                return true;
            }
            return false;
        }

        private static byte[] ExtractXsrc(byte[] xsrcData)
        {
            using (var xsrcStream = new MemoryStream(xsrcData))
            using (var xsrcReader = new BinaryReader(xsrcStream))
            {

                var xsrcHeader = Helpers.ByteToType<XsrcHeader>(xsrcReader);

                var magic = UnicodeHelper.GetUtf8String(xsrcHeader.Magic);
                if (magic.Equals("XSRC") == false)
                {
                    return Array.Empty<byte>();
                }

                var fileNameLen = Helpers.ConvertEndian(xsrcHeader.FileNameLen);
                var fileNameData = xsrcReader.ReadBytes((int)fileNameLen);
                var fileName = UnicodeHelper.GetUtf8String(fileNameData);

                var xsrcBody = Helpers.ByteToType<XsrcBody>(xsrcReader);
                var decompressedSize = Helpers.ConvertEndian(xsrcBody.DecompressedSize);
                var compressedSize = Helpers.ConvertEndian(xsrcBody.CompressedSize);

                var compData = xsrcReader.ReadBytes((int)compressedSize);
                var xmlData = new byte[decompressedSize];

                using (var decompressor = new LibDeflate.GzipDecompressor())
                {
                    if (decompressor.Decompress(compData, xmlData, out var bytesWritten) != System.Buffers.OperationStatus.Done && bytesWritten != decompressedSize)
                    {
                        return Array.Empty<byte>();
                    }
                    return xmlData.ToArray();
                }
            }
        }

        private static string GetLocalizedElementString(XDocument document, XmlNamespaceManager namespaceManager, string id, string defaultValue)
        {
            var elements = document.XPathSelectElements($"/xlast:XboxLiveSubmissionProject/xlast:GameConfigProject/xlast:LocalizedStrings/xlast:LocalizedString[@id='{id}']/xlast:Translation", namespaceManager).ToArray();
            if (elements == null || elements.Length == 0)
            {
                return defaultValue;
            }

            const string desiredLanguage = "en-US";

            var result = elements[0].Value;
            for (int i = 0; i < elements.Length; i++)
            {
                var locale = elements[i]?.FirstAttribute?.Value;
                if (string.Equals(locale, desiredLanguage) == true) 
                {
                    result = elements[i].Value;
                    break;
                }
            }

            return result; 
        }

        private static bool TryDecrypt(XexContext context, byte[] xexKey, byte[] input, out byte[] result)
        {
            result = Array.Empty<byte>();
            try
            {
                using (var aes1 = Aes.Create())
                {
                    aes1.Padding = PaddingMode.None;
                    aes1.Key = xexKey;
                    aes1.IV = new byte[16];
                    using (var aes1Decryptor = aes1.CreateDecryptor())
                    {
                        var imageKey = context.SecurityInfo.ImageInfo.ImageKey;
                        var decryptedKey = aes1Decryptor.TransformFinalBlock(imageKey, 0, imageKey.Length);
                        if (decryptedKey == null)
                        {
                            return false;
                        }

                        var blockMultiple = Helpers.RoundToMultiple((uint)input.Length, 16);
                        var paddingNeeded = blockMultiple - input.Length;
                        var decryptBuffer = paddingNeeded > 0 ? new byte[input.Length + paddingNeeded] : input;
                        if (paddingNeeded > 0)
                        {
                            Array.Copy(input, decryptBuffer, input.Length);
                        }

                        using (var aes2 = Aes.Create())
                        {
                            aes2.Padding = PaddingMode.None;
                            aes2.Key = decryptedKey;
                            aes2.IV = new byte[16];
                            using (var aes2Decryptor = aes2.CreateDecryptor())
                            {
                                var tempResult = aes2Decryptor.TransformFinalBlock(decryptBuffer, 0, decryptBuffer.Length);
                                if (paddingNeeded > 0)
                                {
                                    result = new byte[input.Length];
                                    Array.Copy(tempResult, result, result.Length);
                                }
                                else
                                {
                                    result = tempResult;
                                }
                            }
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        private static bool TryProcessRawData(XexContext context, BinaryReader reader, byte[] data, out byte[] result)
        {
            result = Array.Empty<byte>();
            try
            {
                var fileDataSize = Helpers.ConvertEndian(context.FileDataDescriptor.Size);
                var fileDataCount = (fileDataSize - Helpers.SizeOf<XexRawDescriptor>()) / Helpers.SizeOf<XexRawDescriptor>();

                var imageSize = Helpers.ConvertEndian(context.SecurityInfo.ImageSize);
                var rawData = new byte[imageSize];

                var rawOffset = 0;
                var dataOffset = 0;
                for (var i = 0; i < fileDataCount; i++)
                {
                    var rawDescriptor = Helpers.ByteToType<XexRawDescriptor>(reader);
                    var rawDataSize = Helpers.ConvertEndian(rawDescriptor.DataSize);
                    var rawZeroSize = Helpers.ConvertEndian(rawDescriptor.ZeroSize);
                    if (rawDataSize > 0)
                    {
                        Array.Copy(data, dataOffset, rawData, rawOffset, (int)rawDataSize);
                        dataOffset += (int)rawDataSize;
                        rawOffset += (int)rawDataSize;
                    }
                    if (rawZeroSize > 0)
                    {
                        rawOffset += (int)rawZeroSize;
                    }
                }

                result = rawData;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryDecompressData(XexContext context, BinaryReader reader, byte[] data, out byte[] result)
        {
            result = Array.Empty<byte>();
            try
            {
                var compressedDescriptor = Helpers.ByteToType<XexCompressedDescriptor>(reader);
                var windowSize = Helpers.ConvertEndian(compressedDescriptor.WindowSize);
                var firstSize = Helpers.ConvertEndian(compressedDescriptor.Size);
                var imageSize = Helpers.ConvertEndian(context.SecurityInfo.ImageSize);

                if (XexUnpack.UnpackXexData(data, imageSize, windowSize, firstSize, out var unpacked) == false)
                {
                    return false;
                }

                result = unpacked;
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static bool TryCalculateChecksum(byte[] data, out string hash)
        {
            hash = string.Empty;
            using (var sha = SHA256.Create())
            {
                sha.TransformFinalBlock(data, 0, data.Length);
                var sha256Hash = sha.Hash;
                if (sha256Hash == null)
                {
                    return false;
                }
                hash = BitConverter.ToString(sha256Hash).Replace("-", string.Empty);
                return true;
            }
        }

        private struct XexContext
        {
            public XexHeader Header;
            public XexSecurityInfo SecurityInfo;
            public XexExecution Execution;
            public XexFileDataDescriptor FileDataDescriptor;
        }

        public static bool TryExtractXexMetaData(byte[] xexData, out XexMetaData metaData, string? checksum = null)
        {
            metaData = new XexMetaData
            {
                GameRegion = 0,
                TitleId = 0,
                MediaId = 0,
                Version = 0,
                BaseVersion = 0,
                DiscNum = 0,
                DiscTotal = 0,
                Thumbnail = Array.Empty<byte>(),
                TitleName = string.Empty,
                Description = string.Empty,
                Publisher = string.Empty,
                Developer = string.Empty,
                Genre = string.Empty
            };

            try
            {
                XexContext xexContext = new XexContext();

                using (var xexStream = new MemoryStream(xexData))
                using (var xexReader = new BinaryReader(xexStream))
                {

                    if (xexData.Length < Helpers.SizeOf<XexHeader>())
                    {
                        System.Diagnostics.Debug.Print("Invalid file length for XexHeader structure.");
                        return false;
                    }

                    xexContext.Header = Helpers.ByteToType<XexHeader>(xexReader);

                    var magic = UnicodeHelper.GetUtf8String(xexContext.Header.Magic);
                    if (magic.Equals("XEX2") == false)
                    {
                        System.Diagnostics.Debug.Print("Invalid XEX header magic.");
                        return false;
                    }

                    var securityInfoPos = Helpers.ConvertEndian(xexContext.Header.SecurityInfo);
                    if (securityInfoPos > xexData.Length - Helpers.SizeOf<XexSecurityInfo>())
                    {
                        System.Diagnostics.Debug.Print("Invalid file length for XexSecurityInfo structure.");
                        return false;
                    }

                    xexReader.BaseStream.Position = securityInfoPos;

                    xexContext.SecurityInfo = Helpers.ByteToType<XexSecurityInfo>(xexReader);

                    var regions = Helpers.ConvertEndian(xexContext.SecurityInfo.ImageInfo.GameRegion);
                    if ((regions & 0x000000FF) == 0x000000FF)
                    {
                        metaData.GameRegion |= XexRegion.USA;
                    }
                    if ((regions & 0x0000FD00) == 0x0000FD00)
                    {
                        metaData.GameRegion |= XexRegion.Japan;
                    }
                    if ((regions & 0x00FF0000) == 0x00FF0000)
                    {
                        metaData.GameRegion |= XexRegion.Europe;
                    }

                    var xexExecutionSearchId = (XexExecutionId << 8) | (uint)(Helpers.SizeOf<XexExecution>() >> 2);
                    if (SearchField(xexReader, xexContext.Header, xexExecutionSearchId, out xexContext.Execution) == false)
                    {
                        System.Diagnostics.Debug.Print("Unable to find XexExecution structure.");
                        return false;
                    }

                    metaData.TitleId = Helpers.ConvertEndian(xexContext.Execution.TitleId);
                    metaData.MediaId = Helpers.ConvertEndian(xexContext.Execution.MediaId);
                    metaData.Version = Helpers.ConvertEndian(xexContext.Execution.Version);
                    metaData.BaseVersion = Helpers.ConvertEndian(xexContext.Execution.BaseVersion);
                    metaData.DiscNum = xexContext.Execution.DiscNum;
                    metaData.DiscTotal = xexContext.Execution.DiscTotal;

                    if (checksum != null)
                    {
                        metaData.Checksum = checksum;
                    }
                    else if (TryCalculateChecksum(xexData, out var checksumValue) == true)
                    {
                        metaData.Checksum = checksumValue;
                    }

                    var xexFileDataDescriptorSearchId = (XexFileDataDescriptorId << 8) | 0xff;
                    if (SearchField(xexReader, xexContext.Header, xexFileDataDescriptorSearchId, out xexContext.FileDataDescriptor) == false)
                    {
                        System.Diagnostics.Debug.Print("Skipping detailed xex info due to being unable to find XexFileDataDescriptor structure.");
                        return true;
                    }

                    var fileDataDescriptorPos = xexReader.BaseStream.Position;
                    var dataPos = Helpers.ConvertEndian(xexContext.Header.SizeOfHeaders);
                    var dataLen = xexData.Length - Helpers.ConvertEndian(xexContext.Header.SizeOfHeaders);
                    xexReader.BaseStream.Position = dataPos;
                    var data = xexReader.ReadBytes((int)dataLen);

                    var processed = false;
                    var decryptionKeys = new byte[][] { XexRetailKey, XexDevkitKey };

                    foreach (var decryptionKey in decryptionKeys)
                    {
                        xexReader.BaseStream.Position = fileDataDescriptorPos;

                        var processedData = data;

                        var flags = Helpers.ConvertEndian(xexContext.FileDataDescriptor.Flags);

                        if ((flags & XexDataFlagEncrypted) == XexDataFlagEncrypted)
                        {
                            if (TryDecrypt(xexContext, decryptionKey, processedData, out processedData) == false)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            processedData = data;
                        }

                        var format = Helpers.ConvertEndian(xexContext.FileDataDescriptor.Format);
                        if (format == XexDataFormatRaw)
                        {
                            if (TryProcessRawData(xexContext, xexReader, processedData, out processedData) == false)
                            {
                                continue;
                            }
                        }
                        else if (format == XexDataFormatCompressed)
                        {
                            if (TryDecompressData(xexContext, xexReader, processedData, out processedData) == false)
                            {
                                continue;
                            }

                        }
                        else if (format == XexDataFormatDeltaCompressed)
                        {
                            System.Diagnostics.Debug.Print("Unsupported format 'XexDataFormatDeltaCompressed'.");
                            return false;
                        }
                        else
                        {
                            System.Diagnostics.Debug.Print($"Unrecognized format value {format}.");
                            return false;
                        }

                        processed = processedData[0] == 'M' && processedData[1] == 'Z';
                        if (processed)
                        {
                            data = processedData;
                            break;
                        }
                    }

                    if (processed == false)
                    {
                        System.Diagnostics.Debug.Print("Unable to process xex data.");
                        return false;
                    }

                    xexReader.BaseStream.Position = fileDataDescriptorPos;

                    var headerSectionTableSearchId = (XexHeaderSectionTableId << 8) | 0xff;
                    if (SearchField<XexHeaderSectionTable>(xexReader, xexContext.Header, headerSectionTableSearchId, out var headerSectionTable) == false)
                    {
                        System.Diagnostics.Debug.Print("Skipping detailed xex info due to being unable to find XexHeaderSectionTable structure.");
                        return true;
                    }

                    var headerSectionSize = Helpers.ConvertEndian(headerSectionTable.Size);
                    var headerSectionCount = headerSectionSize / Helpers.SizeOf<XexHeaderSectionEntry>();
                    for (var i = 0; i < headerSectionCount; i++)
                    {
                        var headerSectionEntry = Helpers.ByteToType<XexHeaderSectionEntry>(xexReader);
                        var headerSectionName = UnicodeHelper.GetUtf8String(headerSectionEntry.SectionName);
                        var headerSearchTitle = $"{Helpers.ConvertEndian(xexContext.Execution.TitleId):X}";
                        if (headerSectionName.Equals(headerSearchTitle))
                        {
                            var virtualSize = Helpers.ConvertEndian(headerSectionEntry.VirtualSize);
                            var virtualAddress = Helpers.ConvertEndian(headerSectionEntry.VirtualAddress);

                            using (var dataStream = new MemoryStream(data))
                            using (var dataReader = new BinaryReader(dataStream))
                            {

                                var xdbfPosition = virtualAddress - Helpers.ConvertEndian(xexContext.SecurityInfo.ImageInfo.LoadAddress);

                                dataStream.Position = xdbfPosition;

                                var xdbfHeader = Helpers.ByteToType<XdbfHeader>(dataReader);
                                var xdbfMagic = UnicodeHelper.GetUtf8String(xdbfHeader.Magic);
                                if (xdbfMagic.Equals("XDBF") == false)
                                {
                                    System.Diagnostics.Debug.Print("Invalid XDBF header magic.");
                                    return false;
                                }

                                var entryCount = Helpers.ConvertEndian(xdbfHeader.EntryCount);
                                var entrySize = entryCount * Helpers.SizeOf<XdbfEntry>();
                                if (xdbfPosition + entrySize >= data.Length)
                                {
                                    System.Diagnostics.Debug.Print("Invalid XDBF length for XDBF entries.");
                                    return false;
                                }

                                var baseOffset = xdbfPosition + (Helpers.ConvertEndian(xdbfHeader.EntryTableLen) * Helpers.SizeOf<XdbfEntry>()) + (Helpers.ConvertEndian(xdbfHeader.freeMemTablLen) * 8) + Helpers.SizeOf<XdbfHeader>();
                                if (baseOffset >= data.Length)
                                {
                                    System.Diagnostics.Debug.Print("Invalid XDBF length for XDBF entries.");
                                    return false;
                                }


                                for (var j = 0; j < entryCount; j++)
                                {
                                    var xdbfEntry = Helpers.ByteToType<XdbfEntry>(dataReader);
                                    var entryType = Helpers.ConvertEndian(xdbfEntry.Type);
                                    var entryOffset = baseOffset + Helpers.ConvertEndian(xdbfEntry.Offset);
                                    var entryLength = Helpers.ConvertEndian(xdbfEntry.Length);
                                    var entryIdentifier1 = Helpers.ConvertEndian(xdbfEntry.Identifier1);
                                    var entryIdentifier2 = Helpers.ConvertEndian(xdbfEntry.Identifier2);

                                    if (entryType == 3 && entryIdentifier1 == 0 && entryIdentifier2 == 1)
                                    {
                                        var tempPosition = dataStream.Position;
                                        dataStream.Position = entryOffset;

                                        var xstrHeader = Helpers.ByteToType<XstrHeader>(dataReader);
                                        if (UnicodeHelper.GetUtf8String(xstrHeader.Magic) == "XSTR")
                                        {
                                            var xstrSize = Helpers.ConvertEndian(xstrHeader.Size);
                                            var xstrEntryCount = Helpers.ConvertEndian(xstrHeader.EntryCount);

                                            for (i = 0; i < xstrEntryCount; i++)
                                            {
                                                var xstrType = Helpers.ConvertEndian(dataReader.ReadUInt16());
                                                var xstrLen = Helpers.ConvertEndian(dataReader.ReadUInt16());
                                                var xstrrValue = UnicodeHelper.GetUtf8String(dataReader.ReadBytes(xstrLen));
                                                if (xstrType == 32768)
                                                {
                                                    metaData.TitleName = xstrrValue;
                                                }
                                            }
                                        }
                                    }
                                    else if (entryType == 2 && entryIdentifier1 == 0 && entryIdentifier2 == 0x8000)
                                    {
                                        var tempPosition = dataStream.Position;
                                        dataStream.Position = entryOffset;
                                        metaData.Thumbnail = dataReader.ReadBytes((int)entryLength);
                                        dataStream.Position = tempPosition;
                                    }
                                    else if (entryType == 1 && entryIdentifier1 == 0 && entryIdentifier2 == XSRC)
                                    {
                                        var tempPosition = dataStream.Position;
                                        dataStream.Position = entryOffset;
                                        var xsrcData = ExtractXsrc(dataReader.ReadBytes((int)entryLength));

                                        dataStream.Position = tempPosition;

                                        var namespaceManager = new XmlNamespaceManager(new NameTable());
                                        namespaceManager.AddNamespace("xlast", "http://www.xboxlive.com/xlast");

                                        var documentUnicode = UnicodeHelper.GetUnicodeString(xsrcData);
                                        var xboxLiveSubmissionDocument = XDocument.Parse(documentUnicode);
                                        var gameConfigProjectElement = xboxLiveSubmissionDocument.XPathSelectElement("/xlast:XboxLiveSubmissionProject/xlast:GameConfigProject", namespaceManager);
                                        if (gameConfigProjectElement != null)
                                        {
                                            var titleNameAttribue = gameConfigProjectElement.Attribute(XName.Get("titleName"));
                                            if (titleNameAttribue != null)
                                            {
                                                metaData.TitleName = GetLocalizedElementString(xboxLiveSubmissionDocument, namespaceManager, "32768", metaData.TitleName);
                                            }
                                        }

                                        var productInformationElement = xboxLiveSubmissionDocument.XPathSelectElement("/xlast:XboxLiveSubmissionProject/xlast:GameConfigProject/xlast:ProductInformation", namespaceManager);
                                        if (productInformationElement != null)
                                        {
                                            var sellTextStringIdAttribue = productInformationElement.Attribute(XName.Get("sellTextStringId"));
                                            if (sellTextStringIdAttribue != null)
                                            {
                                                //&lt;Translated text&gt;
                                                metaData.Description = GetLocalizedElementString(xboxLiveSubmissionDocument, namespaceManager, sellTextStringIdAttribue.Value, sellTextStringIdAttribue.Value);
                                            }

                                            var publisherStringIdAttribue = productInformationElement.Attribute(XName.Get("publisherStringId"));
                                            if (publisherStringIdAttribue != null)
                                            {
                                                metaData.Publisher = GetLocalizedElementString(xboxLiveSubmissionDocument, namespaceManager, publisherStringIdAttribue.Value, publisherStringIdAttribue.Value);
                                            }

                                            var developerStringIdAttribue = productInformationElement.Attribute(XName.Get("developerStringId"));
                                            if (developerStringIdAttribue != null)
                                            {
                                                metaData.Developer = GetLocalizedElementString(xboxLiveSubmissionDocument, namespaceManager, developerStringIdAttribue.Value, developerStringIdAttribue.Value);
                                            }

                                            var genreTextStringIdAttribue = productInformationElement.Attribute(XName.Get("genreTextStringId"));
                                            if (genreTextStringIdAttribue != null)
                                            {
                                                metaData.Genre = GetLocalizedElementString(xboxLiveSubmissionDocument, namespaceManager, genreTextStringIdAttribue.Value, genreTextStringIdAttribue.Value);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"Exception occurred: {ex}");
                return false;
            }
        }
    }
}
