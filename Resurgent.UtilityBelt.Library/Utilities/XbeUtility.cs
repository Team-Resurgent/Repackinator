using Resurgent.UtilityBelt.Library.Utilities.XbeModels;
using System.Runtime.InteropServices;
using System.Text;

namespace Resurgent.UtilityBelt.Library.Utilities
{
    //https://github.com/GerbilSoft/rom-properties/blob/fcb6fc09ec7bfbd8b7c6f728aff4308dfa047e2a/src/libromdata/Console/xbox_xbe_structs.h

    public static class XbeUtility
    {
        private static T? ByteToType<T>(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var theStructure = (T?)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return theStructure;
        }

        public enum ImageType
        {
            LogoImage,
            TitleImage,
            SaveImage
        }

        public static bool ReplaceCertInfo(byte[]? attach, byte[]? donor, string xbeTitle, out byte[]? output)
        {
            output = null;

            if (attach == null || donor == null)
            {
                return false;
            }

            using var attachStream = new MemoryStream(attach);
            using var attachReader = new BinaryReader(attachStream);
            var attachHeader = ByteToType<XbeHheader>(attachReader);
            var atatchBaseAddress = attachHeader.Base;
            var atatchCertAddress = attachHeader.Certificate_Addr;
            
            using var donorStream = new MemoryStream(donor);
            using var donorReader = new BinaryReader(donorStream);
            var donorHeader = ByteToType<XbeHheader>(donorReader);
            var donorBaseAddress = donorHeader.Base;
            var donorCertAddress = donorHeader.Certificate_Addr;

            Array.Fill<byte>(donor, 0, (int)(donorCertAddress - donorBaseAddress) + 12, 80);
            var title = Encoding.Unicode.GetBytes(xbeTitle);
            Array.Copy(title, 0, donor, (donorCertAddress - donorBaseAddress) + 12, Math.Min(80, title.Length));

            output = new byte[attach.Length];
            Array.Copy(attach, output, attach.Length);
            Array.Copy(donor, donorCertAddress - donorBaseAddress, output, atatchCertAddress - atatchBaseAddress, 176);

            return true;
        }

        public static bool TryGetXbeCert(byte[]? input, out XbeCertificate? output)
        {
            output = null;

            if (input == null)
            {
                return false;
            }

            using var stream = new MemoryStream(input);
            using var reader = new BinaryReader(stream);
            var header = ByteToType<XbeHheader>(reader);

            var baseAddress = header.Base;
            var certAddress = header.Certificate_Addr;
            stream.Position = certAddress - baseAddress;

            output = ByteToType<XbeCertificate>(reader);
            return true;
        }

        public static bool TryGetXbeImage(byte[]? input, ImageType imageType, out byte[]? output)
        {
            output = null;

            if (input == null)
            {
                return false;
            }

            using var stream = new MemoryStream(input);
            using var reader = new BinaryReader(stream);
            var header = ByteToType<XbeHheader>(reader);

            var baseAddress = header.Base;
            var certAddress = header.Certificate_Addr;
            stream.Position = certAddress - baseAddress;

            var cert = ByteToType<XbeCertificate>(reader);
            var bitmapAddress = header.Logo_Bitmap_Addr;
            var bitmapSize = header.Logo_Bitmap_Size;
            stream.Position = bitmapAddress - baseAddress;

            if (imageType == ImageType.LogoImage)
            {
                output = reader.ReadBytes((int)bitmapSize);
                return true;
            }

            var title_Name = StringHelper.GetUnicodeString(cert.Title_Name);

            if (header.Sections > 0)
            {
                var sectionAddress = header.Section_Headers_Addr;
                
                for (int i = 0; i < header.Sections; i++)
                {
                    stream.Position = (sectionAddress - baseAddress) + (i * Marshal.SizeOf(typeof(XbeSectionHeader)));
                    var section = ByteToType<XbeSectionHeader>(reader);

                    var name = "";
                    if (section.Section_Name_Addr != 0)
                    {           
                        stream.Position = section.Section_Name_Addr - baseAddress;

                        var sectionNameBytes = reader.ReadBytes(20);
                        name = StringHelper.GetUtf8String(sectionNameBytes);
                    }

                    var rawaddress = section.Raw_Addr;
                    var rawsize = section.Sizeof_Raw;
                    stream.Position = rawaddress;
                    
                    if (name == "$$XTIMAGE" && imageType == ImageType.TitleImage)
                    {
                        output = reader.ReadBytes((int)rawsize);
                        return true;
                    }
                    else if (name == "$$XSIMAGE" && imageType == ImageType.SaveImage)
                    {
                        output = reader.ReadBytes((int)rawsize);
                        return true;
                    }                 
                }
            }

            return false;
        }
    }
}
