using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Scarlet.IO;
using Scarlet.Drawing;

namespace Scarlet.Drawing.Compression
{
    internal static class ETC1
    {
        /* Specs: https://www.khronos.org/registry/gles/extensions/OES/OES_compressed_ETC1_RGB8_texture.txt */

        /* Other implementations:
         * https://github.com/richgel999/rg-etc1/blob/master/rg_etc1.cpp
         * https://github.com/Gericom/EveryFileExplorer/blob/master/3DS/GPU/Textures.cs
         * https://github.com/gdkchan/Ohana3DS-Rebirth/blob/master/Ohana3DS%20Rebirth/Ohana/TextureCodec.cs */

        static readonly int[,] etc1ModifierTables =
        {
            { 2, 8, -2, -8 },
            { 5, 17, -5, -17 },
            { 9, 29, -9, -29 },
            { 13, 42, -13, -42 },
            { 18, 60, -18, -60 },
            { 24, 80, -24, -80 },
            { 33, 106, -33, -106 },
            { 47, 183, -47, -183 }
        };

        public static byte[] Decompress(EndianBinaryReader reader, int width, int height, PixelDataFormat inputFormat, long readLength)
        {
            bool hasAlpha = (inputFormat == PixelDataFormat.FormatETC1A4_3DS);
            byte[] pixelData = new byte[readLength * (hasAlpha ? 4 : 8)];

            for (int y = 0; y < height; y += 8)
                for (int x = 0; x < width; x += 8)
                    DecodeETC1Tile(reader, pixelData, x, y, width, height, (hasAlpha ? true : false));

            return pixelData;
        }

        private static void DecodeETC1Tile(BinaryReader reader, byte[] pixelData, int x, int y, int width, int height, bool hasAlpha)
        {
            for (int by = 0; by < 8; by += 4)
            {
                for (int bx = 0; bx < 8; bx += 4)
                {
                    ulong alpha = (hasAlpha ? reader.ReadUInt64() : 0xFFFFFFFFFFFFFFFF);
                    ulong block = reader.ReadUInt64();

                    using (BinaryReader decodedReader = new BinaryReader(new MemoryStream(DecodeETC1Block(block))))
                    {
                        for (int py = 0; py < 4; py++)
                        {
                            for (int px = 0; px < 4; px++)
                            {
                                if (x + bx + px >= width) continue;
                                if (y + by + py >= height) continue;

                                int pixelOffset = (int)((((y + by + py) * width) + (x + bx + px)) * 4);
                                Buffer.BlockCopy(decodedReader.ReadBytes(3), 0, pixelData, pixelOffset, 3);
                                byte pixelAlpha = (byte)((alpha >> (((px * 4) + py) * 4)) & 0xF);
                                pixelData[pixelOffset + 3] = (byte)((pixelAlpha << 4) | pixelAlpha);
                            }
                        }
                    }
                }
            }
        }
        private static byte[] DecodeETC1Block(ulong block)
        {
            byte r1, g1, b1, r2, g2, b2;

            byte tableIndex1 = (byte)((block >> 37) & 0x07);
            byte tableIndex2 = (byte)((block >> 34) & 0x07);
            byte diffBit = (byte)((block >> 33) & 0x01);
            byte flipBit = (byte)((block >> 32) & 0x01);

            if (diffBit == 0x00)
            {
                /* Individual mode */
                r1 = (byte)(((block >> 60) & 0x0F) << 4 | (block >> 60) & 0x0F);
                g1 = (byte)(((block >> 52) & 0x0F) << 4 | (block >> 52) & 0x0F);
                b1 = (byte)(((block >> 44) & 0x0F) << 4 | (block >> 44) & 0x0F);

                r2 = (byte)(((block >> 56) & 0x0F) << 4 | (block >> 56) & 0x0F);
                g2 = (byte)(((block >> 48) & 0x0F) << 4 | (block >> 48) & 0x0F);
                b2 = (byte)(((block >> 40) & 0x0F) << 4 | (block >> 40) & 0x0F);
            }
            else
            {
                /* Differential mode */

                /* 5bit base values */
                byte r1a = (byte)(((block >> 59) & 0x1F));
                byte g1a = (byte)(((block >> 51) & 0x1F));
                byte b1a = (byte)(((block >> 43) & 0x1F));

                /* Subblock 1, 8bit extended */
                r1 = (byte)((r1a << 3) | (r1a >> 2));
                g1 = (byte)((g1a << 3) | (g1a >> 2));
                b1 = (byte)((b1a << 3) | (b1a >> 2));

                /* 3bit modifiers */
                sbyte dr2 = (sbyte)((block >> 56) & 0x07);
                sbyte dg2 = (sbyte)((block >> 48) & 0x07);
                sbyte db2 = (sbyte)((block >> 40) & 0x07);
                if (dr2 >= 4) dr2 -= 8;
                if (dg2 >= 4) dg2 -= 8;
                if (db2 >= 4) db2 -= 8;

                /* Subblock 2, 8bit extended */
                r2 = (byte)((r1a + dr2) << 3 | (r1a + dr2) >> 2);
                g2 = (byte)((g1a + dg2) << 3 | (g1a + dg2) >> 2);
                b2 = (byte)((b1a + db2) << 3 | (b1a + db2) >> 2);
            }

            byte[] decodedData = new byte[(4 * 4) * 3];

            using (BinaryWriter writer = new BinaryWriter(new MemoryStream(decodedData)))
            {
                for (int py = 0; py < 4; py++)
                {
                    for (int px = 0; px < 4; px++)
                    {
                        int index = (int)(((block >> ((px * 4) + py)) & 0x1) | ((block >> (((px * 4) + py) + 16)) & 0x1) << 1);

                        if ((flipBit == 0x01 && py < 2) || (flipBit == 0x00 && px < 2))
                        {
                            int modifier = etc1ModifierTables[tableIndex1, index];
                            writer.Write((byte)((b1 + modifier)).Clamp<int>(byte.MinValue, byte.MaxValue));
                            writer.Write((byte)((g1 + modifier)).Clamp<int>(byte.MinValue, byte.MaxValue));
                            writer.Write((byte)((r1 + modifier)).Clamp<int>(byte.MinValue, byte.MaxValue));
                        }
                        else
                        {
                            int modifier = etc1ModifierTables[tableIndex2, index];
                            writer.Write((byte)((b2 + modifier)).Clamp<int>(byte.MinValue, byte.MaxValue));
                            writer.Write((byte)((g2 + modifier)).Clamp<int>(byte.MinValue, byte.MaxValue));
                            writer.Write((byte)((r2 + modifier)).Clamp<int>(byte.MinValue, byte.MaxValue));
                        }
                    }
                }
            }

            return decodedData;
        }
    }
}
