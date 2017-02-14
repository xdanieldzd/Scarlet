using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Scarlet.IO;
using Scarlet.Drawing;

namespace Scarlet.Drawing.Compression
{
    /* https://www.opengl.org/registry/specs/EXT/texture_compression_s3tc.txt */

    internal enum DXTxBlockLayout { Normal, PSP }
    internal delegate byte[] DXTxBlockDecoderDelegate(EndianBinaryReader reader, PixelDataFormat inputFormat, DXTxBlockLayout blockLayout);

    internal static class DXTx
    {
        public static byte[] Decompress(EndianBinaryReader reader, int width, int height, PixelDataFormat inputFormat, long readLength)
        {
            byte[] outPixels = new byte[readLength * 8];

            PixelOrderingDelegate pixelOrderingFunc = ImageBinary.GetPixelOrderingFunction(inputFormat & PixelDataFormat.MaskPixelOrdering);

            DXTxBlockDecoderDelegate blockDecoder;
            DXTxBlockLayout blockLayout;

            PixelDataFormat specialFormat = (inputFormat & PixelDataFormat.MaskSpecial);
            switch (specialFormat)
            {
                case PixelDataFormat.SpecialFormatDXT1: blockDecoder = DecodeDXT1Block; blockLayout = DXTxBlockLayout.Normal; break;
                case PixelDataFormat.SpecialFormatDXT3: blockDecoder = DecodeDXT3Block; blockLayout = DXTxBlockLayout.Normal; break;
                case PixelDataFormat.SpecialFormatDXT5: blockDecoder = DecodeDXT5Block; blockLayout = DXTxBlockLayout.Normal; break;

                case PixelDataFormat.SpecialFormatDXT1_PSP: blockDecoder = DecodeDXT1Block; blockLayout = DXTxBlockLayout.PSP; break;
                case PixelDataFormat.SpecialFormatDXT3_PSP: blockDecoder = DecodeDXT3Block; blockLayout = DXTxBlockLayout.PSP; break;
                case PixelDataFormat.SpecialFormatDXT5_PSP: blockDecoder = DecodeDXT5Block; blockLayout = DXTxBlockLayout.PSP; break;

                default:
                    throw new Exception("Trying to decode DXT with format set to non-DXT");
            }

            for (int y = 0; y < height; y += 4)
            {
                for (int x = 0; x < width; x += 4)
                {
                    byte[] decompressedBlock = blockDecoder(reader, inputFormat, blockLayout);

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

        /// <summary>
        /// Decodes the given block
        /// </summary>
        /// <param name="inBlock">Block to decode</param>
        /// <param name="has1bitAlpha">Set if block contains DXT1 1-bit alpha (COMPRESSED_RGBA_S3TC_DXT1_EXT)</param>
        /// <param name="isDXT1">Set if block should be decoded as DXT1, else it will be decoded as DXT3/5</param>
        /// <returns></returns>
        private static byte[] DecodeColorBlock(DXT1Block inBlock, bool has1bitAlpha, bool isDXT1)
        {
            byte[] outData = new byte[(4 * 4) * 4];
            byte[,] colors = new byte[4, 4];

            UnpackRgb565(inBlock.Color0, out colors[0, 2], out colors[0, 1], out colors[0, 0]);
            UnpackRgb565(inBlock.Color1, out colors[1, 2], out colors[1, 1], out colors[1, 0]);
            colors[0, 3] = 255;
            colors[1, 3] = 255;

            if (isDXT1 && inBlock.Color0 <= inBlock.Color1)
            {
                colors[2, 0] = (byte)((colors[0, 0] + colors[1, 0]) / 2);
                colors[2, 1] = (byte)((colors[0, 1] + colors[1, 1]) / 2);
                colors[2, 2] = (byte)((colors[0, 2] + colors[1, 2]) / 2);
                colors[2, 3] = 255;

                colors[3, 0] = 0;
                colors[3, 1] = 0;
                colors[3, 2] = 0;
                colors[3, 3] = (byte)((has1bitAlpha && inBlock.Color0 <= inBlock.Color1) ? 0 : 0xFF);
            }
            else
            {
                colors[2, 0] = (byte)((2 * colors[0, 0] + colors[1, 0]) / 3);
                colors[2, 1] = (byte)((2 * colors[0, 1] + colors[1, 1]) / 3);
                colors[2, 2] = (byte)((2 * colors[0, 2] + colors[1, 2]) / 3);
                colors[2, 3] = 255;

                colors[3, 0] = (byte)((colors[0, 0] + 2 * colors[1, 0]) / 3);
                colors[3, 1] = (byte)((colors[0, 1] + 2 * colors[1, 1]) / 3);
                colors[3, 2] = (byte)((colors[0, 2] + 2 * colors[1, 2]) / 3);
                colors[3, 3] = 255;
            }

            for (int by = 0; by < 4; by++)
            {
                for (int bx = 0; bx < 4; bx++)
                {
                    byte code = inBlock.Bits[(by * 4) + bx];
                    for (int c = 0; c < 4; c++)
                        outData[(((by * 4) + bx) * 4) + c] = colors[code, c];
                }
            }

            return outData;
        }

        private static byte[] DecodeDXT1Block(EndianBinaryReader reader, PixelDataFormat inputFormat, DXTxBlockLayout blockLayout)
        {
            DXT1Block inBlock = new DXT1Block(reader, blockLayout);
            return DecodeColorBlock(inBlock, (inputFormat & PixelDataFormat.MaskChannels) != PixelDataFormat.ChannelsRgb, true);
        }

        private static byte[] DecodeDXT3Block(EndianBinaryReader reader, PixelDataFormat inputFormat, DXTxBlockLayout blockLayout)
        {
            DXT3Block inBlock = new DXT3Block(reader, blockLayout);
            byte[] outData = DecodeColorBlock(inBlock.Color, false, false);

            ulong alpha = inBlock.Alpha;
            for (int i = 0; i < outData.Length; i += 4)
            {
                outData[i + 3] = (byte)(((alpha & 0xF) << 4) | (alpha & 0xF));
                alpha >>= 4;
            }

            return outData;
        }

        private static byte[] DecodeDXT5Block(EndianBinaryReader reader, PixelDataFormat inputFormat, DXTxBlockLayout blockLayout)
        {
            DXT5Block inBlock = new DXT5Block(reader, blockLayout);
            byte[] outData = DecodeColorBlock(inBlock.Color, false, false);

            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    byte code = inBlock.Bits[(y * 4) + x];
                    int destOffset = (((y * 4) + x) * 4) + 3;

                    if (inBlock.Alpha0 > inBlock.Alpha1)
                    {
                        switch (code)
                        {
                            case 0x00: outData[destOffset] = inBlock.Alpha0; break;
                            case 0x01: outData[destOffset] = inBlock.Alpha1; break;
                            case 0x02: outData[destOffset] = (byte)((6 * inBlock.Alpha0 + 1 * inBlock.Alpha1) / 7); break;
                            case 0x03: outData[destOffset] = (byte)((5 * inBlock.Alpha0 + 2 * inBlock.Alpha1) / 7); break;
                            case 0x04: outData[destOffset] = (byte)((4 * inBlock.Alpha0 + 3 * inBlock.Alpha1) / 7); break;
                            case 0x05: outData[destOffset] = (byte)((3 * inBlock.Alpha0 + 4 * inBlock.Alpha1) / 7); break;
                            case 0x06: outData[destOffset] = (byte)((2 * inBlock.Alpha0 + 5 * inBlock.Alpha1) / 7); break;
                            case 0x07: outData[destOffset] = (byte)((1 * inBlock.Alpha0 + 6 * inBlock.Alpha1) / 7); break;
                        }
                    }
                    else
                    {
                        switch (code)
                        {
                            case 0x00: outData[destOffset] = inBlock.Alpha0; break;
                            case 0x01: outData[destOffset] = inBlock.Alpha1; break;
                            case 0x02: outData[destOffset] = (byte)((4 * inBlock.Alpha0 + 1 * inBlock.Alpha1) / 5); break;
                            case 0x03: outData[destOffset] = (byte)((3 * inBlock.Alpha0 + 2 * inBlock.Alpha1) / 5); break;
                            case 0x04: outData[destOffset] = (byte)((2 * inBlock.Alpha0 + 3 * inBlock.Alpha1) / 5); break;
                            case 0x05: outData[destOffset] = (byte)((1 * inBlock.Alpha0 + 4 * inBlock.Alpha1) / 5); break;
                            case 0x06: outData[destOffset] = 0x00; break;
                            case 0x07: outData[destOffset] = 0xFF; break;
                        }
                    }
                }
            }

            return outData;
        }

        public static byte[] ExtractBits(ulong bits, int numBits, Endian endianness)
        {
            byte[] bitsExt = new byte[16];
            for (int i = 0; i < bitsExt.Length; i++)
            {
                if (endianness == Endian.LittleEndian)
                    bitsExt[i] = (byte)((bits >> (i * numBits)) & (byte)((1 << numBits) - 1));
                else
                    bitsExt[(bitsExt.Length - 1) - i] = (byte)((bits >> (i * numBits)) & (byte)((1 << numBits) - 1));
            }
            return bitsExt;
        }

        private static void UnpackRgb565(ushort rgb565, out byte r, out byte g, out byte b)
        {
            r = (byte)((rgb565 & 0xF800) >> 11);
            r = (byte)((r << 3) | (r >> 2));
            g = (byte)((rgb565 & 0x07E0) >> 5);
            g = (byte)((g << 2) | (g >> 4));
            b = (byte)(rgb565 & 0x1F);
            b = (byte)((b << 3) | (b >> 2));
        }

    }

    internal class DXT1Block
    {
        public ushort Color0 { get; private set; }
        public ushort Color1 { get; private set; }
        public byte[] Bits { get; private set; }

        public DXT1Block(EndianBinaryReader reader, DXTxBlockLayout blockLayout)
        {
            switch (blockLayout)
            {
                case DXTxBlockLayout.Normal:
                    Color0 = reader.ReadUInt16();
                    Color1 = reader.ReadUInt16();
                    Bits = DXTx.ExtractBits(reader.ReadUInt32(), 2, reader.Endianness);
                    break;

                case DXTxBlockLayout.PSP:
                    Bits = DXTx.ExtractBits(reader.ReadUInt32(), 2, reader.Endianness);
                    Color0 = reader.ReadUInt16();
                    Color1 = reader.ReadUInt16();
                    break;

                default:
                    throw new Exception("Unknown block layout");
            }
        }
    }

    internal class DXT3Block
    {
        public ulong Alpha { get; private set; }
        public DXT1Block Color { get; private set; }

        public DXT3Block(EndianBinaryReader reader, DXTxBlockLayout blockLayout)
        {
            switch (blockLayout)
            {
                case DXTxBlockLayout.Normal:
                    Alpha = reader.ReadUInt64();
                    Color = new DXT1Block(reader, blockLayout);
                    break;

                case DXTxBlockLayout.PSP:
                    Color = new DXT1Block(reader, blockLayout);
                    Alpha = reader.ReadUInt64();
                    break;

                default:
                    throw new Exception("Unknown block layout");
            }
        }
    }

    internal class DXT5Block
    {
        public byte Alpha0 { get; private set; }
        public byte Alpha1 { get; private set; }
        public byte[] Bits { get; private set; }
        public DXT1Block Color { get; private set; }

        public DXT5Block(EndianBinaryReader reader, DXTxBlockLayout blockLayout)
        {
            // TODO: bit reading & conversion changes, see DXT1

            byte bits_5, bits_4, bits_3, bits_2, bits_1, bits_0;

            switch (blockLayout)
            {
                case DXTxBlockLayout.Normal:
                    Alpha0 = reader.ReadByte();
                    Alpha1 = reader.ReadByte();

                    bits_5 = reader.ReadByte();
                    bits_4 = reader.ReadByte();
                    bits_3 = reader.ReadByte();
                    bits_2 = reader.ReadByte();
                    bits_1 = reader.ReadByte();
                    bits_0 = reader.ReadByte();
                    Bits = DXTx.ExtractBits((((ulong)bits_0 << 40) | ((ulong)bits_1 << 32) | ((ulong)bits_2 << 24) | ((ulong)bits_3 << 16) | ((ulong)bits_4 << 8) | (ulong)bits_5), 3, reader.Endianness);

                    Color = new DXT1Block(reader, blockLayout);
                    break;

                case DXTxBlockLayout.PSP:
                    Color = new DXT1Block(reader, blockLayout);
                    Alpha0 = reader.ReadByte();
                    Alpha1 = reader.ReadByte();

                    bits_5 = reader.ReadByte();
                    bits_4 = reader.ReadByte();
                    bits_3 = reader.ReadByte();
                    bits_2 = reader.ReadByte();
                    bits_1 = reader.ReadByte();
                    bits_0 = reader.ReadByte();
                    Bits = DXTx.ExtractBits((((ulong)bits_0 << 40) | ((ulong)bits_1 << 32) | ((ulong)bits_2 << 24) | ((ulong)bits_3 << 16) | ((ulong)bits_4 << 8) | (ulong)bits_5), 3, reader.Endianness);
                    break;

                default:
                    throw new Exception("Unknown block layout");
            }
        }
    }
}
