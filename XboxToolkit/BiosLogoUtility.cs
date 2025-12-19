using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;

namespace XboxToolkit
{
    public class BiosLogoUtility
    {
        private void DecodeLogo(byte[] imageData, ref int index, ref int size, ref int intensity)
        {
            int value1 = imageData[index];
            bool type1 = (value1 & 0x01) > 0;
            if (type1)
            {
                size = (value1 & 0x0e) >> 1;
                intensity = value1 & 0xf0;
                index += 1;
                return;
            }

            int value2 = imageData[index + 1] << 8 | imageData[index];
            bool type2 = (value2 & 0x0002) > 0;
            if (type2)
            {
                size = (value2 & 0x0ffc) >> 2;
                intensity = (value2 & 0xf000) >> 8;
                index += 2;
                return;
            }

            throw new ArgumentOutOfRangeException();
        }

        public void DecodeLogoImage(byte[]? input, out byte[]? output)
        {
            output = null;

            if (input == null)
            {
                return;
            }

            using var image = new Image<L8>(100, 17);

            int imageOffset = 0;
            int index = 0;
            int size = 0;
            int intensity = 0;
            int x = 0;
            int y = 0;

            while (index < input.Length)
            {
                DecodeLogo(input, ref index, ref size, ref intensity);
                for (var i = 0; i < size; i++)
                {
                    image[x, y] = new L8((byte)intensity);
                    imageOffset++;

                    x++;
                    if (x == image.Width)
                    {
                        x = 0;
                        y++;
                    }
                }
            }
            using var imageStream = new MemoryStream();
            image.SaveAsPng(imageStream);
            output = imageStream.ToArray();
        }

        private void EncodeLogo(int size, int intensity, List<byte> imageData)
        {
            while (size > 0)
            {
                if (size <= 7)
                {
                    byte value1 = 1;
                    value1 |= (byte)(size << 1);
                    value1 |= (byte)(intensity << 4);
                    imageData.Add(value1);
                    size = 0;
                    break;
                }
                int clampedSize = Math.Min(size, 1023);
                int value2 = 2;
                value2 |= clampedSize << 2;
                value2 |= intensity << 12;
                imageData.Add((byte)(value2 & 0xff));
                imageData.Add((byte)(value2 >> 8 & 0xff));
                size -= clampedSize;
            }
        }

        public void EncodeLogoImage(byte[]? input, int wodth, int height, out byte[]? output)
        {
            output = null;

            if (input == null)
            {
                return;
            }

            var imageData = new List<byte>();

            using var image = Image.Load<L8>(input);
            image.Mutate(i => i.Resize(wodth, height));

            int size = 1;
            int intensity = image[0, 0].PackedValue >> 4;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < wodth; x++)
                {
                    int pixelIntensity = image[x, y].PackedValue >> 4;
                    if (pixelIntensity == intensity)
                    {
                        size++;
                        continue;
                    }
                    EncodeLogo(size, intensity, imageData);
                    intensity = pixelIntensity;
                    size = 1;
                }
            }

            output = imageData.ToArray();
        }
    }
}
