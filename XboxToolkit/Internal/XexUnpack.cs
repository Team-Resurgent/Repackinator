using System;
using System.IO;
using System.Runtime.InteropServices;

namespace XboxToolkit.Internal
{
    internal unsafe static class XexUnpack
    {
        [DllImport("libXexUnpack", CallingConvention = CallingConvention.Cdecl)]
        private static extern void LZXUnpack(byte* inputData, uint inputSize, byte* outputData, uint outputDataSize, uint windowSize, ref uint error);

        private static bool OptimizePackData(byte[] input, uint firstDataSize, out byte[] packedData)
        {
            var buffer = new byte[input.Length];

            uint currLen = firstDataSize;
            uint nextLen = 1;

            uint compressedSz = 0;
            uint currPos = 0;
            uint lastPos = 0;

            using var ms = new MemoryStream(input);
            using var br = new BinaryReader(ms);

            ms.Position = currPos;

            while ((currPos < input.Length) && (nextLen != 0))
            {
                ms.Position = currPos;
                nextLen = Helpers.ConvertEndian(br.ReadUInt32());
                currPos += 0x18;

                uint innerLen = 1;
                do
                {
                    ms.Position = currPos;
                    innerLen = Helpers.ConvertEndian(br.ReadUInt16());
                    currPos += 2;

                    if (innerLen > 0)
                    {
                        if (compressedSz + innerLen >= buffer.Length || currPos + innerLen >= input.Length)
                        {
                            packedData = Array.Empty<byte>();
                            return false;
                        }
                        Array.Copy(input, currPos, buffer, compressedSz, innerLen);
                        currPos += innerLen;
                        compressedSz += innerLen;
                    }

                } while (innerLen > 0);

                currPos = lastPos + currLen;
                currLen = nextLen;
                lastPos = currPos;
            }

            packedData = buffer.AsSpan(0, (int)compressedSz).ToArray();
            return true;
        }

        public unsafe static bool UnpackXexData(byte[] input, uint imageSize, uint windowSize, uint firstSize, out byte[] output)
        {
            if (OptimizePackData(input, firstSize, out var optimizedData) == false)
            {
                output = Array.Empty<byte>();
                return false;
            }
            return LZXUnpack(optimizedData, imageSize, windowSize, out output);
        }

        public unsafe static bool LZXUnpack(byte[] input, uint imageSize, uint windowSize, out byte[] output)
        {
            output = new byte[imageSize];
            fixed (byte* outputArray = output)
            {
                fixed (byte* inputArray = input)
                {
                    uint error = 0xffffff;
                    LZXUnpack(inputArray, (uint)input.Length, outputArray, (uint)output.Length, windowSize, ref error);
                    return error == 0;
                }
            }
        }
    }
}
