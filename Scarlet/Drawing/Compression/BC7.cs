using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;

using Scarlet.IO;
using Scarlet.Drawing;

namespace Scarlet.Drawing.Compression
{
    /* https://www.khronos.org/registry/OpenGL/extensions/ARB/ARB_texture_compression_bptc.txt
     * https://msdn.microsoft.com/en-us/library/windows/desktop/hh308953(v=vs.85).aspx */

    internal static class BC7
    {
        internal static readonly int[] numSubsetsPerPartition = { 3, 2, 3, 2, 1, 1, 1, 2 };
        internal static readonly int[] partitionBits = { 4, 6, 6, 6, 0, 0, 0, 6 };
        internal static readonly int[] rotationBits = { 0, 0, 0, 0, 2, 2, 0, 0 };
        internal static readonly int[] indexSelectionBits = { 0, 0, 0, 0, 1, 0, 0, 0 };
        internal static readonly int[] colorBits = { 4, 6, 5, 7, 5, 7, 7, 5 };
        internal static readonly int[] alphaBits = { 0, 0, 0, 0, 6, 8, 7, 5 };
        internal static readonly int[] endpointPBits = { 1, 0, 0, 1, 0, 0, 1, 1 };
        internal static readonly int[] sharedPBits = { 0, 1, 0, 0, 0, 0, 0, 0 };
        internal static readonly int[] indexBitsPerElement = { 3, 3, 2, 2, 2, 2, 4, 2 };
        internal static readonly int[] secondaryIndexBitsPerElement = { 0, 0, 0, 0, 3, 2, 0, 0 };

        internal static readonly int[] interpolationFactors2 = { 0, 21, 43, 64 };
        internal static readonly int[] interpolationFactors3 = { 0, 9, 18, 27, 37, 46, 55, 64 };
        internal static readonly int[] interpolationFactors4 = { 0, 4, 9, 13, 17, 21, 26, 30, 34, 38, 43, 47, 51, 55, 60, 64 };

        public static byte[] Decompress(EndianBinaryReader reader, int width, int height, PixelDataFormat inputFormat, long readLength)
        {
            byte[] outPixels = new byte[readLength * 8];

            PixelOrderingDelegate pixelOrderingFunc = ImageBinary.GetPixelOrderingFunction(inputFormat & PixelDataFormat.MaskPixelOrdering);

            for (int y = 0; y < height; y += 4)
            {
                for (int x = 0; x < width; x += 4)
                {
                    byte[] decompressedBlock = DecodeBlock(reader, inputFormat);

                    int rx, ry;
                    pixelOrderingFunc(x / 4, y / 4, width / 4, height / 4, inputFormat, out rx, out ry);
                    rx *= 4; ry *= 4;

                    for (int py = 0; py < 4; py++)
                    {
                        for (int px = 0; px < 4; px++)
                        {
                            int ix = (rx + px);
                            int iy = (ry + py);

                            if (ix >= width || iy >= height) continue;

                            for (int c = 0; c < 4; c++)
                                outPixels[(((iy * width) + ix) * 4) + c] = decompressedBlock[(((py * 4) + px) * 4) + c];
                        }
                    }
                }
            }

            return outPixels;
        }

        private static byte[] DecodeBlock(EndianBinaryReader reader, PixelDataFormat inputFormat)
        {
            byte[] outData = new byte[(4 * 4) * 4];

            BC7Block inBlock = null;

            inBlock = new BC7Block(reader.ReadUInt64(), reader.ReadUInt64());

            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    byte[] pixelData = inBlock.GetPixelColor(x, y);
                    Buffer.BlockCopy(pixelData, 0, outData, ((y * 4) + x) * 4, pixelData.Length);
                }
            }

            return outData;
        }
    }

    internal class BC7Block
    {
        public BigInteger Data { get; private set; }

        public int Mode { get; private set; }
        public int NumSubsets { get; private set; }
        public int Partitions { get; private set; }
        public int Rotation { get; private set; }
        public int IndexSelection { get; private set; }
        public int[,,] Color { get; private set; }
        public int[,,] Alpha { get; private set; }
        public int EndpointPBit { get; private set; }
        public int SharedPBit { get; private set; }
        public int[] PrimaryIndices { get; private set; }
        public int[] SecondaryIndices { get; private set; }

        bool isValid;
        bool hasAlpha { get { return (BC7.alphaBits[Mode] != 0); } }

        public BC7Block(ulong data0, ulong data1)
        {
            // Assemble BigInteger
            Data = data1;
            Data <<= 64;
            Data |= data0;

            // Skip empty blocks
            if (Data == 0)
                return;

            // Extract mode value
            Mode = -1;
            for (int i = 0; i < 8; i++)
            {
                if ((Data & (ulong)(1 << i)) != 0)
                {
                    Mode = i;
                    break;
                }
            }
            if (Mode == -1)
                throw new Exception("Unexpected/unknown BC7 block mode");

            // Store number of subsets
            NumSubsets = BC7.numSubsetsPerPartition[Mode];

            // Extract data
            int index = (Mode + 1);
            Partitions = ExtractData(ref index, BC7.partitionBits[Mode]);
            Rotation = ExtractData(ref index, BC7.rotationBits[Mode]);
            IndexSelection = ExtractData(ref index, BC7.indexSelectionBits[Mode]);

            Color = new int[2, NumSubsets, 3];
            for (int ep = 0; ep < 2; ep++)
                for (int sub = 0; sub < NumSubsets; sub++)
                    for (int col = 0; col < 3; col++)
                        Color[ep, sub, col] = ExtractData(ref index, BC7.colorBits[Mode]);

            Alpha = new int[2, NumSubsets, 1];
            for (int ep = 0; ep < 2; ep++)
                for (int sub = 0; sub < NumSubsets; sub++)
                    for (int alp = 0; alp < 1; alp++)
                        Alpha[ep, sub, alp] = ExtractData(ref index, BC7.alphaBits[Mode]);

            EndpointPBit = ExtractData(ref index, BC7.endpointPBits[Mode]);
            SharedPBit = ExtractData(ref index, BC7.sharedPBits[Mode]);

            PrimaryIndices = new int[16];
            for (int idx = 0; idx < PrimaryIndices.Length; idx++)
                PrimaryIndices[idx] = ExtractData(ref index, BC7.indexBitsPerElement[Mode]);

            SecondaryIndices = new int[16];
            for (int idx = 0; idx < SecondaryIndices.Length; idx++)
                SecondaryIndices[idx] = ExtractData(ref index, BC7.secondaryIndexBitsPerElement[Mode]);

            isValid = true;
        }

        private int ExtractData(ref int index, int len)
        {
            int last = (index + len);
            BigInteger mask = (((BigInteger)(1 << (last - index)) - 1) << index);
            int value = (int)((Data & mask) >> index);
            index += len;
            return value;
        }

        public byte[] GetPixelColor(int x, int y)
        {
            byte[] pixelData = new byte[4];

            if (isValid)
            {
                int idx = (y * 4) + x;
                int colorIdx = PrimaryIndices[idx];
                int alphaIdx = SecondaryIndices[idx];

                pixelData[1] = Interpolate(Color[0, 0, 0], Color[1, 0, 0], colorIdx, BC7.indexBitsPerElement[Mode]);
                pixelData[2] = Interpolate(Color[0, 0, 1], Color[1, 0, 1], colorIdx, BC7.indexBitsPerElement[Mode]);
                pixelData[3] = Interpolate(Color[0, 0, 2], Color[1, 0, 2], colorIdx, BC7.indexBitsPerElement[Mode]);

                if (hasAlpha)
                    pixelData[0] = Interpolate(Alpha[0, 0, 0], Alpha[1, 0, 0], alphaIdx, BC7.secondaryIndexBitsPerElement[Mode]);
                else
                    pixelData[0] = 0xFF;
            }

            return pixelData;
        }

        private byte Interpolate(int ep0, int ep1, int index, int bits)
        {
            if (bits == 2)
                return (byte)((((64 - BC7.interpolationFactors2[index]) * ep0) + (BC7.interpolationFactors2[index] * ep1) + 32) >> 6);
            else if (bits == 3)
                return (byte)((((64 - BC7.interpolationFactors3[index]) * ep0) + (BC7.interpolationFactors3[index] * ep1) + 32) >> 6);
            else
                return (byte)((((64 - BC7.interpolationFactors4[index]) * ep0) + (BC7.interpolationFactors4[index] * ep1) + 32) >> 6);
        }
    }
}
