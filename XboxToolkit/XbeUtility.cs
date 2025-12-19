using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using XboxToolkit.Internal;
using XboxToolkit.Internal.Xbe;
using XboxToolkit.Models.Xbe;

namespace XboxToolkit
{
    public static class XbeUtility
    {
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
            var attachHeader = StructUtility.ByteToType<XbeHheader>(attachReader);
            var atatchBaseAddress = attachHeader.Base;
            var atatchCertAddress = attachHeader.Certificate_Addr;

            using var donorStream = new MemoryStream(donor);
            using var donorReader = new BinaryReader(donorStream);
            var donorHeader = StructUtility.ByteToType<XbeHheader>(donorReader);
            var donorBaseAddress = donorHeader.Base;
            var donorCertAddress = donorHeader.Certificate_Addr;

            Array.Fill<byte>(donor, 0, (int)(donorCertAddress - donorBaseAddress) + 12, 80);
            var title = Encoding.Unicode.GetBytes(xbeTitle);
            Array.Copy(title, 0, donor, (donorCertAddress - donorBaseAddress) + 12, Math.Min(80, title.Length));

            output = new byte[attach.Length];
            Array.Copy(attach, output, attach.Length);
            Array.Copy(donor, donorCertAddress - donorBaseAddress, output, atatchCertAddress - atatchBaseAddress, Marshal.SizeOf(typeof(XbeCertificate)));

            // Force diff version for TU's to work
            output[(atatchCertAddress - atatchBaseAddress) + 175] = (byte)(output[(atatchCertAddress - atatchBaseAddress) + 175] | 0x80);

            return true;
        }

        public static bool TryGetXbeCert(byte[]? input, out XbeCertificate? output)
        {
            output = null;

            if (input == null)
            {
                return false;
            }

            try
            {
                using var stream = new MemoryStream(input);
                using var reader = new BinaryReader(stream);
                var header = StructUtility.ByteToType<XbeHheader>(reader);

                var baseAddress = header.Base;
                var certAddress = header.Certificate_Addr;
                stream.Position = certAddress - baseAddress;

                output = StructUtility.ByteToType<XbeCertificate>(reader);
                if (output != null)
                {
                    output.Version &= 0x7fffffff;
                }
                return true;
            }
            catch
            {

            }
            return false;
        }

        public static bool TryReplaceXbeTitleImage(byte[]? input, byte[]? image)
        {
            if (input == null)
            {
                return false;
            }

            if (image == null)
            {
                return false;
            }

            using var memoryStream = new MemoryStream(image);
            using var icon = Image.Load(memoryStream);
            icon.Mutate(m => m.Resize(128, 128));
            icon.Mutate(m => m.Flip(FlipMode.Vertical));
            icon.Mutate(m => m.BackgroundColor(Color.Black));
            using var tempImage = icon.CloneAs<Bgr24>();
            var iconBuffer = new byte[49152];
            tempImage.CopyPixelDataTo(iconBuffer);

            using var stream = new MemoryStream(input);
            using var reader = new BinaryReader(stream);
            var header = StructUtility.ByteToType<XbeHheader>(reader);

            var baseAddress = header.Base;
            var certAddress = header.Certificate_Addr;
            stream.Position = certAddress - baseAddress;

            var cert = StructUtility.ByteToType<XbeCertificate>(reader);
            if (cert == null)
            {
                return false;
            }

            var bitmapAddress = header.Logo_Bitmap_Addr;
            var bitmapSize = header.Logo_Bitmap_Size;
            stream.Position = bitmapAddress - baseAddress;

            var title_Name = UnicodeHelper.GetUnicodeString(cert.Title_Name);

            if (header.Sections > 0)
            {
                var sectionAddress = header.Section_Headers_Addr;

                for (int i = 0; i < header.Sections; i++)
                {
                    stream.Position = (sectionAddress - baseAddress) + (i * Marshal.SizeOf(typeof(XbeSectionHeader)));
                    var section = StructUtility.ByteToType<XbeSectionHeader>(reader);

                    var name = "";
                    if (section.Section_Name_Addr != 0)
                    {
                        stream.Position = section.Section_Name_Addr - baseAddress;

                        var sectionNameBytes = reader.ReadBytes(20);
                        name = UnicodeHelper.GetUtf8String(sectionNameBytes);
                    }

                    var rawaddress = section.Raw_Addr;
                    var rawsize = section.Sizeof_Raw;
                    stream.Position = rawaddress;

                    if (name == "$$XTIMAGE" && rawsize == 49206)
                    {
                        Array.Copy(iconBuffer, 0, input, rawaddress + (49206 - 49152), 49152);
                        return true;
                    }
                }
            }

            return false;
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
            var header = StructUtility.ByteToType<XbeHheader>(reader);

            var baseAddress = header.Base;
            var certAddress = header.Certificate_Addr;
            stream.Position = certAddress - baseAddress;

            var cert = StructUtility.ByteToType<XbeCertificate>(reader);
            if (cert == null)
            {
                return false;
            }

            var bitmapAddress = header.Logo_Bitmap_Addr;
            var bitmapSize = header.Logo_Bitmap_Size;
            stream.Position = bitmapAddress - baseAddress;

            if (imageType == ImageType.LogoImage)
            {
                output = reader.ReadBytes((int)bitmapSize);
                return true;
            }

            var title_Name = UnicodeHelper.GetUnicodeString(cert.Title_Name);

            if (header.Sections > 0)
            {
                var sectionAddress = header.Section_Headers_Addr;

                for (int i = 0; i < header.Sections; i++)
                {
                    stream.Position = (sectionAddress - baseAddress) + (i * Marshal.SizeOf(typeof(XbeSectionHeader)));
                    var section = StructUtility.ByteToType<XbeSectionHeader>(reader);

                    var name = "";
                    if (section.Section_Name_Addr != 0)
                    {
                        stream.Position = section.Section_Name_Addr - baseAddress;

                        var sectionNameBytes = reader.ReadBytes(20);
                        name = UnicodeHelper.GetUtf8String(sectionNameBytes);
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
