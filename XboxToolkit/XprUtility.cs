using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using XboxToolkit.Internal;
using XboxToolkit.Models.Dds;

namespace XboxToolkit
{
    public static class XprUtility
    {
        private static int DXT1toARGB(byte[] src, int srcOffset, Image<Rgba32> dest, int x, int y)
        {
            // colour is in R5G6B5 format, convert to R8G8B8

            ushort[] colors = new ushort[2];
            Rgba32[] color = new Rgba32[4];

            for (var i = 0; i < 2; i++)
            {
                colors[i] = src[srcOffset++];
                colors[i] |= (ushort)(src[srcOffset++] << 8);

                color[i].R = (byte)((colors[i] & 0xF800) >> 11);
                color[i].G = (byte)((colors[i] & 0x7E0) >> 5);
                color[i].B = (byte)(colors[i] & 0x1f);
                color[i].R = (byte)(color[i].R << 3 | color[i].R >> 2);
                color[i].G = (byte)(color[i].G << 2 | color[i].G >> 3);
                color[i].B = (byte)(color[i].B << 3 | color[i].B >> 2);
                color[i].A = 255;
            }

            if (colors[0] > colors[1])
            {
                color[2].R = (byte)((2 * color[0].R + color[1].R) / 3);
                color[2].G = (byte)((2 * color[0].G + color[1].G) / 3);
                color[2].B = (byte)((2 * color[0].B + color[1].B) / 3);
                color[2].A = 255;

                color[3].R = (byte)((color[0].R + 2 * color[1].R) / 3);
                color[3].G = (byte)((color[0].G + 2 * color[1].G) / 3);
                color[3].B = (byte)((color[0].B + 2 * color[1].B) / 3);
                color[3].A = 255;
            }
            else
            {
                color[2].R = (byte)((color[0].R + color[1].R) / 2);
                color[2].G = (byte)((color[0].G + color[1].G) / 2);
                color[2].B = (byte)((color[0].B + color[1].B) / 2);
                color[2].A = 255;

                color[3].R = 0;
                color[3].G = 0;
                color[3].B = 0;
                color[3].A = 0;
            }

            for (int yOffset = 0; yOffset < 4; yOffset++)
            {
                var rowVal = src[srcOffset++];
                for (int xOffset = 0; xOffset < 4; xOffset++)
                {
                    var pixel = color[(rowVal >> (xOffset << 1)) & 0x03];
                    dest[x + xOffset, y + yOffset] = pixel;
                }
            }

            return srcOffset;
        }

        private static int DXT3toARGB(byte[] src, int srcOffset, Image<Rgba32> dest, int x, int y)
        {
            int alphaOffset = srcOffset;

            srcOffset += 8;

            ushort[] colors = new ushort[2];
            Rgba32[] color = new Rgba32[4];

            for (var i = 0; i < 2; i++)
            {
                colors[i] = src[srcOffset++];
                colors[i] |= (ushort)(src[srcOffset++] << 8);

                color[i].R = (byte)((colors[i] & 0xF800) >> 11);
                color[i].G = (byte)((colors[i] & 0x7E0) >> 5);
                color[i].B = (byte)(colors[i] & 0x1f);
                color[i].R = (byte)(color[i].R << 3 | color[i].R >> 2);
                color[i].G = (byte)(color[i].G << 2 | color[i].G >> 3);
                color[i].B = (byte)(color[i].B << 3 | color[i].B >> 2);
                color[i].A = 255;
            }

            color[2].R = (byte)((2 * color[0].R + color[1].R) / 3);
            color[2].G = (byte)((2 * color[0].G + color[1].G) / 3);
            color[2].B = (byte)((2 * color[0].B + color[1].B) / 3);
            color[2].A = 255;

            color[3].R = (byte)((color[0].R + 2 * color[1].R) / 3);
            color[3].G = (byte)((color[0].G + 2 * color[1].G) / 3);
            color[3].B = (byte)((color[0].B + 2 * color[1].B) / 3);
            color[3].A = 255;

            for (int yOffset = 0; yOffset < 4; yOffset++)
            {
                var rowVal = src[srcOffset++];

                ushort rowAlpha = src[alphaOffset++];
                rowAlpha |= (ushort)(src[alphaOffset++] << 8);

                for (int xOffset = 0; xOffset < 4; xOffset++)
                {
                    byte currentAlpha = (byte)((rowAlpha >> (xOffset * 4)) & 0x0f);
                    currentAlpha |= (byte)(currentAlpha << 4);

                    var pixel = color[(rowVal >> (xOffset << 1)) & 0x03];
                    pixel.A = currentAlpha;

                    dest[x + xOffset, y + yOffset] = pixel;
                }
            }

            return srcOffset;
        }

        private static void Unswizzle(byte[] src, uint depth, uint width, uint height, ref byte[] dest)
        {
            for (uint y = 0; y < height; y++)
            {
                uint sy = 0;
                if (y < width)
                {
                    for (int bit = 0; bit < 16; bit++)
                    {
                        sy |= (y >> bit & 1) << 2 * bit;
                    }
                    sy <<= 1; // y counts twice
                }
                else
                {
                    uint y_mask = y % width;
                    for (int bit = 0; bit < 16; bit++)
                    {
                        sy |= (y_mask >> bit & 1) << 2 * bit;
                    }
                    sy <<= 1; // y counts twice
                    sy += y / width * width * width;
                }
                uint d = y * width * depth;
                for (uint x = 0; x < width; x++)
                {
                    uint sx = 0;
                    if (x < height * 2)
                    {
                        for (int bit = 0; bit < 16; bit++)
                        {
                            sx |= (x >> bit & 1) << 2 * bit;
                        }
                    }
                    else
                    {
                        uint x_mask = x % (2 * height);
                        for (int bit = 0; bit < 16; bit++)
                        {
                            sx |= (x_mask >> bit & 1) << 2 * bit;
                        }
                        sx += x / (2 * height) * 2 * height * height;
                    }
                    uint s = (sx + sy) * depth;
                    for (uint i = 0; i < depth; ++i)
                    {
                        dest[d] = src[s + i];
                        d = d + 1;
                    }
                }
            }
        }

        private static void ConvertDXT1(byte[] src, Image<Rgba32> dest)
        {
            var srcOffset = 0;
            var width = dest.Width;
            var height = dest.Height;
            for (var y = 0; y < height; y += 4)
            {
                for (var x = 0; x < width; x += 4)
                {
                    srcOffset = DXT1toARGB(src, srcOffset, dest, x, y);
                }
            }
        }

        private static void ConvertDXT3(byte[] src, Image<Rgba32> dest)
        {
            var srcOffset = 0;
            var width = dest.Width;
            var height = dest.Height;
            for (var y = 0; y < height; y += 4)
            {
                for (var x = 0; x < width; x += 4)
                {
                    srcOffset = DXT3toARGB(src, srcOffset, dest, x, y);
                }
            }
        }

        private static void ConvertARGB(byte[] src, Image<Rgba32> dest)
        {
            var buffer = new byte[dest.Width * dest.Height * 4];
            Unswizzle(src, 4, (uint)dest.Width, (uint)dest.Height, ref buffer);

            var srcOffset = 0;
            var width = dest.Width;
            var height = dest.Height;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var blue = buffer[srcOffset++];
                    var green = buffer[srcOffset++];
                    var red = buffer[srcOffset++];
                    var alpha = buffer[srcOffset++];
                    dest[x, y] = new Rgba32(red, green, blue, alpha);
                }
            }
        }

        private static void ConvertRGB(byte[] src, Image<Rgba32> dest)
        {
            var buffer = new byte[dest.Width * dest.Height * 4];
            Unswizzle(src, 4, (uint)dest.Width, (uint)dest.Height, ref buffer);

            var srcOffset = 0;
            var width = dest.Width;
            var height = dest.Height;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var blue = buffer[srcOffset++];
                    var green = buffer[srcOffset++];
                    var red = buffer[srcOffset++];
                    srcOffset++;
                    dest[x, y] = new Rgba32(red, green, blue, 255);
                }
            }
        }

        private static void ConvertRGBA(byte[] src, Image<Rgba32> dest)
        {
            var buffer = new byte[dest.Width * dest.Height * 4];
            Unswizzle(src, 4, (uint)dest.Width, (uint)dest.Height, ref buffer);

            var srcOffset = 0;
            var width = dest.Width;
            var height = dest.Height;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var alpha = buffer[srcOffset++];
                    var blue = buffer[srcOffset++];
                    var green = buffer[srcOffset++];
                    var red = buffer[srcOffset++];
                    dest[x, y] = new Rgba32(red, green, blue, alpha);
                }
            }
        }

        private static void ConvertA4R4G4B4(byte[] src, Image<Rgba32> dest)
        {
            var buffer = new byte[dest.Width * dest.Height * 2];
            Unswizzle(src, 2, (uint)dest.Width, (uint)dest.Height, ref buffer);

            var srcOffset = 0;
            var width = dest.Width;
            var height = dest.Height;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var blue = (byte)((buffer[srcOffset + 0] & 0x0f) << 4);
                    var green = (byte)(buffer[srcOffset++] & 0xf0);
                    var red = (byte)((buffer[srcOffset] & 0x0f) << 4);
                    var alpha = (byte)(buffer[srcOffset++] & 0xf0);
                    dest[x, y] = new Rgba32(red, green, blue, alpha);
                }
            }
        }


        private static void ConvertR5G6B5(byte[] src, Image<Rgba32> dest)
        {
            var buffer = new byte[dest.Width * dest.Height * 2];
            Unswizzle(src, 2, (uint)dest.Width, (uint)dest.Height, ref buffer);

            var srcOffset = 0;
            var width = dest.Width;
            var height = dest.Height;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var blue = (byte)((buffer[srcOffset] & 0x1f) << 3);
                    var green = (byte)((buffer[srcOffset++] & 0xe0) >> 3);
                    green |= (byte)((buffer[srcOffset] & 0x07) << 5);
                    var red = (byte)(buffer[srcOffset++] & 0xf8);
                    dest[x, y] = new Rgba32(red, green, blue, 255);
                }
            }
        }

        public static bool ConvertDdsToPng(byte[]? input, out byte[]? output)
        {
            output = null;

            if (input == null)
            {
                return false;
            }

            using var inputStream = new MemoryStream(input);
            using var reader = new BinaryReader(inputStream);

            var magic = reader.ReadUInt32();
            if (magic != 0x20534444)
            {
                return false;
            }

            var header = StructUtility.ByteToType<DdsHeader>(reader);


            if (header.PixelFormat.FourCC == 0x31545844)
            {
                try
                {
                    using var image = new Image<Rgba32>((int)header.Width, (int)header.Height);
                    var imageData = reader.ReadBytes((int)((header.Width * header.Height) << 1));
                    ConvertDXT1(imageData, image);

                    using var outputStream = new MemoryStream();
                    image.SaveAsPng(outputStream);
                    output = outputStream.ToArray();
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else if (header.PixelFormat.FourCC == 0x33545844)
            {
                try
                {
                    using var image = new Image<Rgba32>((int)header.Width, (int)header.Height);
                    var imageData = reader.ReadBytes((int)(header.Width * header.Height));
                    ConvertDXT3(imageData, image);

                    using var outputStream = new MemoryStream();
                    image.SaveAsPng(outputStream);
                    output = outputStream.ToArray();
                    return true;

                }
                catch (Exception)
                {
                    return false;
                }
            }
            return false;
        }


        public static bool ConvertXprToJpeg(byte[]? input, out byte[]? output)
        {
            output = null;

            if (input == null)
            {
                return false;
            }

            using var inputStream = new MemoryStream(input);
            using var reader = new BinaryReader(inputStream);

            var magic = reader.ReadUInt32();

            if (magic == 0x20534444)
            {
                return ConvertDdsToPng(input, out output);
            }

            if (magic != 0x30525058)
            {
                try
                {
                    inputStream.Position = 0;
                    using var imagex = Image.Load(input);
                    using var outputStream2 = new MemoryStream();
                    imagex.SaveAsPng(outputStream2);
                    output = outputStream2.ToArray();
                    return true;
                }
                catch
                {
                }
                return false;
            }

            var totalSize = reader.ReadUInt32();
            if (totalSize > input.Length)
            {
                return false;
            }

            var headerSize = reader.ReadUInt32();
            var flags = reader.ReadUInt32();
            var count = flags & 0xffff;
            if (count != 1)
            {
                return false;
            }

            var type = flags >> 16 & 0x7;
            if (type != 4)
            {
                return false;
            }

            var unused1 = reader.ReadUInt32();
            if (unused1 != 0)
            {
                return false;
            }

            var unused2 = reader.ReadUInt32();
            if (unused2 != 0)
            {
                return false;
            }

            var format = reader.ReadUInt32();
            var mipLevels = (int)(format >> 24 & 0xff);
            var dimension = 1 << mipLevels;

            using var image = new Image<Rgba32>(dimension, dimension);

            var unused3 = reader.ReadInt32();
            if (unused3 != 0)
            {
                return false;
            }

            if (headerSize != 32)
            {
                var end = reader.ReadUInt32();
                if (end != 0xffffffff)
                {
                    return false;
                }
            }

            var paddingSize = headerSize - inputStream.Position;
            if (paddingSize > 0)
            {
                _ = reader.ReadBytes((int)paddingSize);
            }

            var imageSize = totalSize - headerSize;
            var imageData = reader.ReadBytes((int)imageSize);

            var dxt = format >> 8 & 0xff;
            if (dxt == 0x0c)
            {
                ConvertDXT1(imageData, image);
            }
            else if (dxt == 0x0e)
            {
                ConvertDXT3(imageData, image);
            }
            else if (dxt == 0x04)
            {
                ConvertA4R4G4B4(imageData, image);
            }
            else if (dxt == 0x05)
            {
                ConvertR5G6B5(imageData, image);
            }
            else if (dxt == 0x06)
            {
                ConvertARGB(imageData, image);
            }
            else if (dxt == 0x07)
            {
                ConvertRGB(imageData, image);
            }
            else if (dxt == 0x3c)
            {
                ConvertRGBA(imageData, image);
            }
            else
            {
                return false;
            }

            using var outputStream = new MemoryStream();
            image.SaveAsJpeg(outputStream);
            output = outputStream.ToArray();

            var result = inputStream.Position == totalSize;
            return result;
        }

    }
}
