using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace Resurgent.UtilityBelt.Library.Utilities
{
    public static class XexUtility
    {
        private static byte[] Xex1Key = { 0xA2, 0x6C, 0x10, 0xF7, 0x1F, 0xD9, 0x35, 0xE9, 0x8B, 0x99, 0x92, 0x2C, 0xE9, 0x32, 0x15, 0x72 };
        private static byte[] Xex2Key = { 0x20, 0xB1, 0x85, 0xA5, 0x9D, 0x28, 0xFD, 0xC3, 0x40, 0x58, 0x3F, 0xBB, 0x08, 0x96, 0xBF, 0x91 };

        private static readonly uint XexExecutionId = 0x400;
        private static readonly uint XexHeaderSectionTableId = 0x2;
        private static readonly uint XexFileDataDescriptorId = 0x3;
        private static readonly uint XexSecurityFlagMfgSupport = 0x4;
        private static readonly uint XexDataFlagEncrypted = 0x1;
        private static readonly uint XexDataFormatRaw = 0x1;
        private static readonly uint XexDataFormatCompressed = 0x2;
        private static readonly uint XexDataFormatDeltaCompressed = 0x3;

        private static readonly uint XSRC = 0x58535243;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct XexHeader
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] Magic = Array.Empty<byte>();

            public uint ModuleFlags = 0;

            public uint SizeOfHeaders = 0;

            public uint SizeOfDiscardableHeaders = 0;

            public uint SecurityInfo = 0;

            public uint HeaderDirectoryEntryCount = 0; 
            
            public XexHeader()
            {

            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct HvImageInfo
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x100)]
            public byte[] Signature = Array.Empty<byte>();

            public uint InfoSize = 0; 

            public uint ImageFlags = 0;

            public uint LoadAddress = 0;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
            public byte[] ImageHash = Array.Empty<byte>();

            public uint ImportTableCoun = 0; 

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
            public byte[] ImportDigest = Array.Empty<byte>();

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
            public byte[] MediaID = Array.Empty<byte>();

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
            public byte[] ImageKey = Array.Empty<byte>();

            public uint ExportTableAddress = 0;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
            public byte[] HeaderHash = Array.Empty<byte>();

            public uint GameRegion = 0;

            public HvImageInfo()
            {
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct XexSecurityInfo
        {
            public uint Size = 0;

            public uint ImageSize = 0;

            public HvImageInfo ImageInfo = new();

            public uint AllowedMediaTypes = 0;

            public uint PageDescriptorCount = 0;

            public XexSecurityInfo()
            {

            }
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        private struct XexExecution
        {
            [FieldOffset(0)]
            public uint MediaId = 0;

            [FieldOffset(4)]
            public uint Version = 0;

            [FieldOffset(8)]
            public uint BaseVersion = 0;

            [FieldOffset(12)]
            public uint TitleId = 0;

            [FieldOffset(12)]
            public ushort PublisherId = 0;

            [FieldOffset(14)]
            public ushort GameId = 0;

            [FieldOffset(16)]
            public byte Platform = 0;

            [FieldOffset(17)]
            public byte ExecutableType = 0;

            [FieldOffset(18)]
            public byte DiscNum = 0;

            [FieldOffset(19)]
            public byte DiscTotal = 0;

            [FieldOffset(20)]
            public uint SaveGameID = 0;

            public XexExecution()
            {

            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct XexFileDataDescriptor
        {
            public uint Size = 0;

            public ushort Flags = 0;

            public ushort Format = 0;

            public XexFileDataDescriptor()
            {

            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct XexDataDescriptor
        {
            public uint Size = 0;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
            public byte[] DataDigest = Array.Empty<byte>();

            public XexDataDescriptor()
            {

            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct XexCompressedDescriptor
        {
            public uint WindowSize = 0;

            public uint Size = 0;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
            public byte[] DataDigest = Array.Empty<byte>();

            public XexCompressedDescriptor()
            {

            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct XexRawDescriptor
        {
            public uint DataSize = 0;

            public uint ZeroSize = 0;

            public XexRawDescriptor()
            {

            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct XexHeaderSectionTable
        {
            public uint Size = 0;

            public XexHeaderSectionTable()
            {

            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct XexHeaderSectionEntry
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x8)]
            public byte[] SectionName = Array.Empty<byte>();

            public uint VirtualAddress = 0;

            public uint VirtualSize = 0;

            public XexHeaderSectionEntry()
            {

            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct XdbfHeader
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x4)]
            public byte[] Magic = Array.Empty<byte>();

            public uint Version = 0;

            public uint EntryTableLen = 0;

            public uint EntryCount = 0;

            public uint freeMemTablLen = 0;

            public uint freeMemTablEntryCount = 0;

            public XdbfHeader()
            {

            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct XdbfEntry
        {
            public ushort Type = 0;

            public uint Identifier1 = 0;

            public uint Identifier2 = 0;

            public uint Offset = 0;

            public uint Length = 0;

            public XdbfEntry()
            {

            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct XsrcHeader
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x4)]
            public byte[] Magic = Array.Empty<byte>();

            public uint Version = 0;

            public uint Size = 0;

            public uint FileNameLen = 0;

            public XsrcHeader()
            {

            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct XsrcBody
        {
            public uint DecompressedSize = 0;

            public uint CompressedSize = 0;

            public XsrcBody()
            {

            }
        }

        public struct XexMetaData
        {
            public uint GameRegion;
            public string TitleId;
            public string MediaId;
            public uint Version;
            public uint BaseVersion;
            public uint DiscNum;
            public uint DiscTotal;
            public byte[] Thumbnail;
            public string TitleName;
            public string Description;
            public string Publisher;
            public string Developer;
            public string Genre;
        }

        private static uint ConvertEndian(uint value)
        {
            return
                ((value & 0x000000ff) << 24) |
                ((value & 0x0000ff00) << 8) |
                ((value & 0x00ff0000) >> 8) |
                ((value & 0xff000000) >> 24);
        }

        private static ushort ConvertEndian(ushort value)
        {
            return (ushort)(
                ((value & 0x000ff) << 8) | 
                ((value & 0xff00) >> 8)
            );
        }

        private static bool SearchField<T>(BinaryReader binaryReader, XexHeader header, uint searchId, out T? result)
        {
            result = default;

            binaryReader.BaseStream.Position = StructUtility.SizeOf<XexHeader>();
            var headerDirectoryEntryCount = ConvertEndian(header.HeaderDirectoryEntryCount);

            for (var i = 0; i < headerDirectoryEntryCount; i++)
            {
                var value = ConvertEndian(binaryReader.ReadUInt32());
                var offset = ConvertEndian(binaryReader.ReadUInt32());
                if (value != searchId)
                {
                    continue;
                }
                binaryReader.BaseStream.Position = offset;
                result = StructUtility.ByteToType<T>(binaryReader);
                return true;
            }
            return false;
        }

        private static byte[] ExtractXsrc(byte[] xsrcData)
        {
            using var xsrcStream = new MemoryStream(xsrcData);
            using var xsrcReader = new BinaryReader(xsrcStream);

            var xsrcHeader = StructUtility.ByteToType<XsrcHeader>(xsrcReader);

            var magic = StringHelper.GetUtf8String(xsrcHeader.Magic);
            if (magic.Equals("XSRC") == false)
            {
                return Array.Empty<byte>();
            }

            var fileNameLen = ConvertEndian(xsrcHeader.FileNameLen);
            var fileNameData = xsrcReader.ReadBytes((int)fileNameLen);
            var fileName = StringHelper.GetUtf8String(fileNameData);

            var xsrcBody = StructUtility.ByteToType<XsrcBody>(xsrcReader);
            var decompressedSize = ConvertEndian(xsrcBody.DecompressedSize);
            var compressedSize = ConvertEndian(xsrcBody.CompressedSize);

            var compData = xsrcReader.ReadBytes((int)compressedSize);
            var xmlData = new byte[decompressedSize];

            using var decompressor = new LibDeflate.GzipDecompressor();
            if (decompressor.Decompress(compData, xmlData, out var bytesWritten) != System.Buffers.OperationStatus.Done && bytesWritten != decompressedSize)
            {
                return Array.Empty<byte>();
            }

            return xmlData.ToArray();
        }

        public static string GetLocalizedElementString(XDocument document, XmlNamespaceManager namespaceManager, string id, string defaultValue)
        {
            var element = document.XPathSelectElement($"/xlast:XboxLiveSubmissionProject/xlast:GameConfigProject/xlast:LocalizedStrings/xlast:LocalizedString[@id='{id}']/xlast:Translation", namespaceManager);
            if (element == null)
            {
                return defaultValue;
            }

            return element.FirstNode?.ToString() ?? defaultValue;
        }

        public static XexMetaData? ExtractXex(byte[] xexData)
        {
            var metaData = new XexMetaData
            {
                GameRegion = 0,
                TitleId = string.Empty,
                MediaId = string.Empty,
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

            using var xexStream = new MemoryStream(xexData);
            using var xexReader = new BinaryReader(xexStream);

            if (xexData.Length < StructUtility.SizeOf<XexHeader>())
            {
                System.Diagnostics.Debug.Print("Invalid file length for XexHeader structure.");
                return null;
            }

            var header = StructUtility.ByteToType<XexHeader>(xexReader);

            var magic = StringHelper.GetUtf8String(header.Magic);
            if (magic.Equals("XEX2") == false)
            {
                System.Diagnostics.Debug.Print("Invalid XEX header magic.");
                return null;
            }

            var securityInfoPos = ConvertEndian(header.SecurityInfo);
            if (securityInfoPos > xexData.Length - StructUtility.SizeOf<XexSecurityInfo>())
            {
                System.Diagnostics.Debug.Print("Invalid file length for XexSecurityInfo structure.");
                return null;
            }

            xexReader.BaseStream.Position = securityInfoPos;

            var securityInfo = StructUtility.ByteToType<XexSecurityInfo>(xexReader);

            metaData.GameRegion = ConvertEndian(securityInfo.ImageInfo.GameRegion);

            var xexExecutionSearchId = (XexExecutionId << 8) | (uint)(StructUtility.SizeOf<XexExecution>() >> 2);
            if (SearchField<XexExecution>(xexReader, header, xexExecutionSearchId, out var xexExecution) == false)
            {
                System.Diagnostics.Debug.Print("Unable to find XexExecution structure.");
                return null;
            }

            metaData.TitleId = $"{ConvertEndian(xexExecution.TitleId):X8}";
            metaData.MediaId = $"{ConvertEndian(xexExecution.MediaId):X8}";
            metaData.Version = ConvertEndian(xexExecution.Version);
            metaData.BaseVersion = ConvertEndian(xexExecution.BaseVersion);
            metaData.DiscNum = xexExecution.DiscNum;
            metaData.DiscTotal = xexExecution.DiscTotal;

            var xexFileDataDescriptorSearchId = (XexFileDataDescriptorId << 8) | 0xff;
            if (SearchField<XexFileDataDescriptor>(xexReader, header, xexFileDataDescriptorSearchId, out var xexFileDataDescriptor) == false)
            {
                System.Diagnostics.Debug.Print("Unable to find XexFileDataDescriptor structure.");
                return null;
            }

            var xexFileDataDescriptorPos = xexReader.BaseStream.Position;

            var dataPos = ConvertEndian(header.SizeOfHeaders);
            var dataLen = xexData.Length - ConvertEndian(header.SizeOfHeaders);
            xexReader.BaseStream.Position = dataPos;
            var data = xexReader.ReadBytes((int)dataLen);

            var imageSize = ConvertEndian(securityInfo.ImageSize);
            var outBuf = new byte[imageSize];

            var flags = ConvertEndian(xexFileDataDescriptor.Flags);
            if ((flags & XexDataFlagEncrypted) == XexDataFlagEncrypted)
            {
                var imageFlasgs = ConvertEndian(securityInfo.ImageInfo.ImageFlags);
                var sizeOfHeaders = ConvertEndian(header.SizeOfHeaders);
                var xexKey = ((imageFlasgs & XexSecurityFlagMfgSupport) == XexSecurityFlagMfgSupport) ? Xex1Key : Xex2Key;
                using var aes1 = Aes.Create();
                aes1.Key = xexKey;
                aes1.IV = new byte[16];
                var decryptedKey = aes1.DecryptCbc(securityInfo.ImageInfo.ImageKey, aes1.IV, PaddingMode.None);
                if (decryptedKey == null)
                {
                    System.Diagnostics.Debug.Print("Failed to decrypt xex data.");
                    return null;
                }
                using var aes2 = Aes.Create();
                aes2.Key = decryptedKey;
                aes2.IV = new byte[16];
                data = aes2.DecryptCbc(data, aes2.IV, PaddingMode.None);
            }

            var format = ConvertEndian(xexFileDataDescriptor.Format);
            if (format == XexDataFormatRaw)
            {
                var fileDataSize = ConvertEndian(xexFileDataDescriptor.Size);
                var fileDataCount = fileDataSize / StructUtility.SizeOf<XexRawDescriptor>();

                xexReader.BaseStream.Position = xexFileDataDescriptorPos;

                var rawData = new byte[imageSize];

                var rawOffset = 0;
                var dataOffset = 0;
                for (var i = 0; i < fileDataCount; i++)
                {
                    var rawDescriptor = StructUtility.ByteToType<XexRawDescriptor>(xexReader);
                    var rawDataSize = ConvertEndian(rawDescriptor.DataSize);
                    var rawZeroSize = ConvertEndian(rawDescriptor.ZeroSize);
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

                data = rawData;

            }
            else if (format == XexDataFormatCompressed)
            {
                xexReader.BaseStream.Position = xexFileDataDescriptorPos;
                var compressedDescriptor = StructUtility.ByteToType<XexCompressedDescriptor>(xexReader);

                uint windowSize = ConvertEndian(compressedDescriptor.WindowSize);
                uint firstSize = ConvertEndian(compressedDescriptor.Size);

                if (Unpacker.UnpackXexData(data, imageSize, windowSize, firstSize, out var unpacked) == false)
                {
                    System.Diagnostics.Debug.Print("Failed to extract xex data.");
                    return null;
                }

                data = unpacked;

            }
            else if (format == XexDataFormatDeltaCompressed)
            {
                System.Diagnostics.Debug.Print("Unsupported format 'XexDataFormatDeltaCompressed'.");
                return null;
            }
            else
            {
                System.Diagnostics.Debug.Print($"Unrecognized format value {format}.");
                return null;
            }

            var headerSectionTableSearchId = (XexHeaderSectionTableId << 8) | 0xff;
            if (SearchField<XexHeaderSectionTable>(xexReader, header, headerSectionTableSearchId, out var headerSectionTable) == false)
            {
                System.Diagnostics.Debug.Print("Unable to find XexFileDataDescriptor structure.");
                return null;
            }

            var headerSectionSize = ConvertEndian(headerSectionTable.Size);
            var headerSectionCount = headerSectionSize / StructUtility.SizeOf<XexHeaderSectionEntry>();
            for (var i = 0; i < headerSectionCount; i++)
            {
                var headerSectionEntry = StructUtility.ByteToType<XexHeaderSectionEntry>(xexReader);
                var headerSectionName = StringHelper.GetUtf8String(headerSectionEntry.SectionName);
                var headerSearchTitle = $"{ConvertEndian(xexExecution.TitleId):X}";
                if (headerSectionName.Equals(headerSearchTitle))
                {
                    var virtualSize = ConvertEndian(headerSectionEntry.VirtualSize);
                    var virtualAddress = ConvertEndian(headerSectionEntry.VirtualAddress);
            
                    using var dataStream = new MemoryStream(data);
                    using var dataReader = new BinaryReader(dataStream);

                    var xdbfPosition = virtualAddress - ConvertEndian(securityInfo.ImageInfo.LoadAddress);

                    dataStream.Position = xdbfPosition;

                    var xdbfHeader = StructUtility.ByteToType<XdbfHeader>(dataReader);
                    var xdbfMagic = StringHelper.GetUtf8String(xdbfHeader.Magic);
                    if (xdbfMagic.Equals("XDBF") == false)
                    {
                        System.Diagnostics.Debug.Print("Invalid XDBF header magic.");
                        return null;
                    }

                    var entryCount = ConvertEndian(xdbfHeader.EntryCount);
                    var entrySize = entryCount * StructUtility.SizeOf<XdbfEntry>();
                    if (xdbfPosition + entrySize >= data.Length)
                    {
                        System.Diagnostics.Debug.Print("Invalid XDBF length for XDBF entries.");
                        return null;
                    }

                    var baseOffset = xdbfPosition + (ConvertEndian(xdbfHeader.EntryTableLen) * StructUtility.SizeOf<XdbfEntry>()) + (ConvertEndian(xdbfHeader.freeMemTablLen) * 8) + StructUtility.SizeOf<XdbfHeader>();
                    if (baseOffset >= data.Length)
                    {
                        System.Diagnostics.Debug.Print("Invalid XDBF length for XDBF entries.");
                        return null;
                    }

                    for (var j = 0; j < entryCount; j++)
                    {
                        var xdbfEntry = StructUtility.ByteToType<XdbfEntry>(dataReader);
                        var entryType = ConvertEndian(xdbfEntry.Type);
                        var entryOffset = baseOffset + ConvertEndian(xdbfEntry.Offset);
                        var entryLength = ConvertEndian(xdbfEntry.Length);
                        var entryIdentifier1 = ConvertEndian(xdbfEntry.Identifier1);
                        var entryIdentifier2 = ConvertEndian(xdbfEntry.Identifier2);
                        if (entryType == 2 && entryIdentifier1 == 0 && entryIdentifier2 == 0x8000)
                        {
                            if (entryLength <= 0x3D00)
                            {
                                var tempPosition = dataStream.Position;
                                dataStream.Position = entryOffset;
                                metaData.Thumbnail = dataReader.ReadBytes((int)entryLength);
                                dataStream.Position = tempPosition; 
                            }
                        }
                        else if (entryType == 1 && entryIdentifier1 == 0 && entryIdentifier2 == XSRC) 
                        {
                            var tempPosition = dataStream.Position;
                            dataStream.Position = entryOffset;
                            var xsrcData = ExtractXsrc(dataReader.ReadBytes((int)entryLength));
                            dataStream.Position = tempPosition;

                            var namespaceManager = new XmlNamespaceManager(new NameTable());
                            namespaceManager.AddNamespace("xlast", "http://www.xboxlive.com/xlast");

                            var xboxLiveSubmissionDocument = XDocument.Parse(StringHelper.GetUnicodeString(xsrcData));
                            var gameConfigProjectElement = xboxLiveSubmissionDocument.XPathSelectElement("/xlast:XboxLiveSubmissionProject/xlast:GameConfigProject", namespaceManager);
                            if (gameConfigProjectElement != null)
                            {
                                var titleNameAttribue = gameConfigProjectElement.Attribute(XName.Get("titleName"));
                                if (titleNameAttribue != null)
                                {
                                    metaData.TitleName = GetLocalizedElementString(xboxLiveSubmissionDocument, namespaceManager, "32768", titleNameAttribue.Value);
                                }
                            }

                            var productInformationElement = xboxLiveSubmissionDocument.XPathSelectElement("/xlast:XboxLiveSubmissionProject/xlast:GameConfigProject/xlast:ProductInformation", namespaceManager);
                            if (productInformationElement != null)
                            {
                                var sellTextStringIdAttribue = productInformationElement.Attribute(XName.Get("sellTextStringId"));
                                if (sellTextStringIdAttribue != null)
                                {
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

            return metaData;
        }
    }
}
