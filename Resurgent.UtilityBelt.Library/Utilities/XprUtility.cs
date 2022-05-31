using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Resurgent.UtilityBelt.Library.Utilities
{
    public static class XprUtility
    {
        private static Rgba32 UintRgba32ToRgba32(uint colour)
        {
            return new Rgba32((byte)(colour >> 16 & 0xff), (byte)(colour >> 8 & 0xff), (byte)(colour & 0xff), (byte)(colour >> 24 & 0xff));
        }

        private static void DXT1toARGB(byte[] src, uint srcOffset, Image<Rgba32> dest, uint destOffset, uint destWidth)
        {
            try
            {
                // colour is in R5G6B5 format, convert to R8G8B8
                uint[] colour = new uint[4];
                byte[] red = new byte[4];
                byte[] green = new byte[4];
                byte[] blue = new byte[4];

                for (int i = 0; i < 2; i++)
                {
                    red[i] = (byte)(src[2 * i + 1 + srcOffset] & 0xf8);
                    green[i] = (byte)((src[2 * i + 1 + srcOffset] & 0x7) << 5 | (src[2 * i + srcOffset] & 0xe0) >> 3);
                    blue[i] = (byte)((src[2 * i + srcOffset] & 0x1f) << 3);
                    colour[i] = (uint)(red[i] << 16 | green[i] << 8 | blue[i]);
                }

                if (colour[0] > colour[1])
                {
                    red[2] = (byte)((2 * red[0] + red[1] + 1) / 3);
                    green[2] = (byte)((2 * green[0] + green[1] + 1) / 3);
                    blue[2] = (byte)((2 * blue[0] + blue[1] + 1) / 3);
                    red[3] = (byte)((red[0] + 2 * red[1] + 1) / 3);
                    green[3] = (byte)((green[0] + 2 * green[1] + 1) / 3);
                    blue[3] = (byte)((blue[0] + 2 * blue[1] + 1) / 3);
                    for (int i = 0; i < 4; i++)
                    {
                        colour[i] = (uint)(red[i] << 16 | green[i] << 8 | blue[i] | 0xFF000000);
                    }
                }
                else
                {
                    red[2] = (byte)((red[0] + red[1]) / 2);
                    green[2] = (byte)((green[0] + green[1]) / 2);
                    blue[2] = (byte)((blue[0] + blue[1]) / 2);
                    for (int i = 0; i < 3; i++)
                    {
                        colour[i] = (uint)(red[i] << 16 | green[i] << 8 | blue[i] | 0xFF000000);
                    }
                    colour[2] = 0;  // transparent
                }
                for (int y = 0; y < 4; y++)
                {
                    var offset = destWidth * y + destOffset;
                    var offsetx = (int)(offset % dest.Width);
                    var offsety = (int)(offset / dest.Height);
                    dest[offsetx, offsety] = UintRgba32ToRgba32(colour[src[4 + y + srcOffset] & 0x03]);
                    dest[offsetx + 1, offsety] = UintRgba32ToRgba32(colour[(src[4 + y + srcOffset] & 0x0e) >> 2]);
                    dest[offsetx + 2, offsety] = UintRgba32ToRgba32(colour[(src[4 + y + srcOffset] & 0x30) >> 4]);
                    dest[offsetx + 3, offsety] = UintRgba32ToRgba32(colour[(src[4 + y + srcOffset] & 0xe0) >> 6]);
                }
            }
            catch (Exception ex)
            {
                var a = 1;
            }
        }

        private static void DXT3toARGB(byte[] src, uint srcOffset, Image<Rgba32> dest, uint destOffset, uint destWidth)
        {
            try
            {
                uint b = srcOffset;
                //BYTE* b = (BYTE*)src;
                // colour is in R5G6B5 format, convert to R8G8B8
                uint[] colour = new uint[4];
                byte[] red = new byte[4];
                byte[] green = new byte[4];
                byte[] blue = new byte[4];
                byte[,] alpha = new byte[4, 4];

                alpha[0, 0] = (byte)((src[0 + b] & 0x0f) << 4 | src[0 + b] & 0x0f);
                alpha[0, 1] = (byte)(src[0 + b] & 0xf0 | (byte)((src[0 + b] & 0xf0) >> 4));
                alpha[0, 2] = (byte)((src[1 + b] & 0x0f) << 4 | (byte)(src[1 + b] & 0x0f));
                alpha[0, 3] = (byte)(src[1 + b] & 0xf0 | (byte)((src[1 + b] & 0xf0) >> 4));

                alpha[1, 0] = (byte)((src[2 + b] & 0x0f) << 4 | src[2 + b] & 0x0f);
                alpha[1, 1] = (byte)(src[2 + b] & 0xf0 | (src[2 + b] & 0xf0) >> 4);
                alpha[1, 2] = (byte)((src[3 + b] & 0x0f) << 4 | src[3 + b] & 0x0f);
                alpha[1, 3] = (byte)(src[3 + b] & 0xf0 | (src[3 + b] & 0xf0) >> 4);

                alpha[2, 0] = (byte)((src[4 + b] & 0x0f) << 4 | src[4 + b] & 0x0f);
                alpha[2, 1] = (byte)(src[4 + b] & 0xf0 | (src[4 + b] & 0xf0) >> 4);
                alpha[2, 2] = (byte)((src[5 + b] & 0x0f) << 4 | src[5 + b] & 0x0f);
                alpha[2, 3] = (byte)(src[5 + b] & 0xf0 | (src[5 + b] & 0xf0) >> 4);

                alpha[3, 0] = (byte)((src[6 + b] & 0x0f) << 4 | src[6 + b] & 0x0f);
                alpha[3, 1] = (byte)(src[6 + b] & 0xf0 | (src[6 + b] & 0xf0) >> 4);
                alpha[3, 2] = (byte)((src[7 + b] & 0x0f) << 4 | src[7 + b] & 0x0f);
                alpha[3, 3] = (byte)(src[7 + b] & 0xf0 | (src[7 + b] & 0xf0) >> 4);

                b += 8;

                for (int i = 0; i < 2; i++)
                {
                    red[i] = (byte)(src[2 * i + 1 + b] & 0xf8);
                    green[i] = (byte)((src[2 * i + 1 + b] & 0x7) << 5 | (src[2 * i + b] & 0xe0) >> 3);
                    blue[i] = (byte)((src[2 * i + b] & 0x1f) << 3);
                    colour[i] = (uint)(red[i] << 16 | green[i] << 8 | blue[i]);
                }

                red[2] = (byte)((2 * red[0] + red[1] + 1) / 3);
                green[2] = (byte)((2 * green[0] + green[1] + 1) / 3);
                blue[2] = (byte)((2 * blue[0] + blue[1] + 1) / 3);
                red[3] = (byte)((red[0] + 2 * red[1] + 1) / 3);
                green[3] = (byte)((green[0] + 2 * green[1] + 1) / 3);
                blue[3] = (byte)((blue[0] + 2 * blue[1] + 1) / 3);
                for (int i = 0; i < 4; i++)
                {
                    colour[i] = (uint)(red[i] << 16 | green[i] << 8 | blue[i]);
                }

                // ok, now grab the bits
                for (int y = 0; y < 4; y++)
                {
                    var offset = destWidth * y + destOffset;
                    var offsetx = (int)(offset % dest.Width);
                    var offsety = (int)(offset / dest.Height);
                    dest[offsetx, offsety] = UintRgba32ToRgba32(colour[src[4 + y + b] & 0x03] | (uint)(alpha[y, 0] << 24));
                    dest[offsetx + 1, offsety] = UintRgba32ToRgba32(colour[(src[4 + y + b] & 0x0e) >> 2] | (uint)(alpha[y, 1] << 24));
                    dest[offsetx + 2, offsety] = UintRgba32ToRgba32(colour[(src[4 + y + b] & 0x30) >> 4] | (uint)(alpha[y, 2] << 24));
                    dest[offsetx + 3, offsety] = UintRgba32ToRgba32(colour[(src[4 + y + b] & 0xe0) >> 6] | (uint)(alpha[y, 3] << 24));

                }
            }
            catch (Exception ex)
            {
                var a = 1;
            }
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
            var width = (uint)dest.Width;
            var height = (uint)dest.Height;
            for (uint y = 0; y < height; y += 4)
            {
                for (uint x = 0; x < width; x += 4)
                {
                    uint s = y * width / 2 + x * 2;
                    uint d = y * width + x;
                    DXT1toARGB(src, s, dest, d, width);
                }
            }
        }

        private static void ConvertDXT3(byte[] src, Image<Rgba32> dest)
        {
            var width = (uint)dest.Width;
            var height = (uint)dest.Height;
            for (uint y = 0; y < height; y += 4)
            {
                for (uint x = 0; x < width; x += 4)
                {
                    uint s = y * width + x * 4;
                    uint d = y * width + x;
                    DXT3toARGB(src, s, dest, d, width);
                }
            }
        }

        private static void ConvertARGB(byte[] src, Image<Rgba32> dest)
        {
            var buffer = new byte[dest.Width * dest.Height * 4];
            Unswizzle(src, 4, (uint)dest.Width, (uint)dest.Height, ref buffer);

            var width = (uint)dest.Width;
            var height = (uint)dest.Height;
            for (uint y = 0; y < height; y++)
            {
                for (uint x = 0; x < width; x++)
                {
                    uint s = y * width + x << 2;

                    var red = (uint)(buffer[s] << 16);
                    var green = (uint)(buffer[s + 1] << 8);
                    var blue = (uint)buffer[s + 2];
                    var alpha = (uint)(buffer[s + 3] << 24);
                    var color = red | green | blue | alpha;
                    dest[(int)x, (int)y] = UintRgba32ToRgba32(color);
                }
            }
        }

        private static void ConvertRGB(byte[] src, Image<Rgba32> dest)
        {
            var buffer = new byte[dest.Width * dest.Height * 4];
            Unswizzle(src, 4, (uint)dest.Width, (uint)dest.Height, ref buffer);

            var width = (uint)dest.Width;
            var height = (uint)dest.Height;
            for (uint y = 0; y < height; y++)
            {
                for (uint x = 0; x < width; x++)
                {
                    uint s = (y * width + x) * 4;
                    var red = (uint)(buffer[s] << 16);
                    var green = (uint)(buffer[s + 1] << 8);
                    var blue = (uint)buffer[s + 2];
                    var color = red | green | blue | 0xff000000;
                    dest[(int)x, (int)y] = UintRgba32ToRgba32(color);
                }
            }
        }

        private static void ConvertRGBA(byte[] src, Image<Rgba32> dest)
        {
            var buffer = new byte[dest.Width * dest.Height * 4];
            Unswizzle(src, 4, (uint)dest.Width, (uint)dest.Height, ref buffer);

            var width = (uint)dest.Width;
            var height = (uint)dest.Height;
            for (uint y = 0; y < height; y++)
            {
                for (uint x = 0; x < width; x++)
                {
                    uint s = (y * width + x) * 4;
                    var red = (uint)(buffer[s + 3] << 16);
                    var green = (uint)(buffer[s + 2] << 8);
                    var blue = (uint)buffer[s + 1];
                    var alpha = (uint)(buffer[s] << 24);
                    var color = red | green | blue | alpha;
                    dest[(int)x, (int)y] = UintRgba32ToRgba32(color);
                }
            }
        }

        private static void ConvertA4R4G4B4(byte[] src, Image<Rgba32> dest)
        {
            var buffer = new byte[dest.Width * dest.Height * 2];
            Unswizzle(src, 2, (uint)dest.Width, (uint)dest.Height, ref buffer);

            var width = (uint)dest.Width;
            var height = (uint)dest.Height;
            for (uint y = 0; y < height; y++)
            {
                for (uint x = 0; x < width; x++)
                {
                    uint s = (y * width + x) * 2;
                    var alpha = (uint)(buffer[s + 1] & 0xf0) << 24;
                    var red = (uint)((buffer[s + 1] & 0x0f) << 20);
                    var green = (uint)((buffer[s + 0] & 0xf0) << 8);
                    var blue = (uint)((buffer[s + 0] & 0x0f) << 4);
                    var color = red | green | blue | alpha;
                    dest[(int)x, (int)y] = UintRgba32ToRgba32(color);
                }
            }
        }


        private static void ConvertR5G6B5(byte[] src, Image<Rgba32> dest)
        {
            var buffer = new byte[dest.Width * dest.Height * 2];
            Unswizzle(src, 2, (uint)dest.Width, (uint)dest.Height, ref buffer);

            var width = (uint)dest.Width;
            var height = (uint)dest.Height;
            for (uint y = 0; y < height; y++)
            {
                for (uint x = 0; x < width; x++)
                {
                    uint s = (y * width + x) * 2;
                    var red = (uint)(buffer[s + 1] & 0xf8) << 16;
                    var green1 = (uint)((buffer[s + 1] & 0x07) << 13);
                    var green2 = (uint)((buffer[s + 0] & 0xe0) << 5);
                    var blue = (uint)((buffer[s + 0] & 0x1f) << 3);
                    var color = red | green1 | green2 | blue | 0xff000000;
                    dest[(int)x, (int)y] = UintRgba32ToRgba32(color);
                }
            }
        }

        public static bool ConvertXprToPng(byte[]? input, out byte[]? output)
        {
            output = null;

            if (input == null)
            {
                return false;
            }

            using var inputStream = new MemoryStream(input);
            using var reader = new BinaryReader(inputStream);

            var magic = reader.ReadUInt32();
            if (magic != 0x30525058)
            {
                return false;
            }
            
            var totalSize = reader.ReadUInt32();
            if (totalSize != input.Length)
            {
                return false;
            }

            var headerSize = reader.ReadUInt32();
            if (headerSize != 0x800)
            {
                return false;
            }

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

            var end = reader.ReadUInt32();
            if (end != 0xffffffff)
            {
                return false;
            }

            var paddingSize = headerSize - inputStream.Position;
            var padding = reader.ReadBytes((int)paddingSize);
            foreach (var value in padding)
            {
                if (value != 0xad)
                {
                    return false;
                }
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
            image.SaveAsPng(outputStream);
            output = outputStream.ToArray();

            var result = inputStream.Position == totalSize;
            return result;
        }

    }
}
