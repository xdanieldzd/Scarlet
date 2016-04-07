using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Scarlet.Drawing;
using Scarlet.IO;

namespace Scarlet.Platform.Sony
{
    [Flags]
    public enum PS2WrapMode
    {
        HorizontalRepeat = 0x0000,
        VerticalRepeat = 0x0000,
        HorizontalClamp = 0x0100,
        VerticalClamp = 0x0400,
    }

    public enum PS2PixelFormat
    {
        PSMCT32 = 0x00,
        PSMCT24 = 0x01,
        PSMCT16 = 0x02,
        PSMCT16S = 0x0A,
        PSMT8 = 0x13,
        PSMT4 = 0x14,
        PSMT8H = 0x1B,
        PSMT4HL = 0x24,
        PSMT4HH = 0x2C
    }

    /* TODO: more formats, verify existing information, etc... */
    public static class PS2
    {
        static readonly Dictionary<PS2PixelFormat, PixelDataFormat> formatMap = new Dictionary<PS2PixelFormat, PixelDataFormat>()
        {
            { PS2PixelFormat.PSMT8, PixelDataFormat.FormatIndexed8 },
            { PS2PixelFormat.PSMT4, PixelDataFormat.FormatIndexed4 },
            { PS2PixelFormat.PSMCT16, PixelDataFormat.FormatArgb1555 },
            { PS2PixelFormat.PSMCT24, PixelDataFormat.FormatRgb888 },
            { PS2PixelFormat.PSMCT32, PixelDataFormat.FormatArgb8888 },
        };

        public static PixelDataFormat GetPixelDataFormat(PS2PixelFormat pixelFormat)
        {
            if (!formatMap.ContainsKey(pixelFormat)) throw new Exception("No matching pixel data format known");
            return formatMap[pixelFormat];
        }

        public static bool IsFormatIndexed(PS2PixelFormat pixelFormat)
        {
            /* TODO: turn into Dictionary as well? */
            return (pixelFormat != PS2PixelFormat.PSMCT32 && pixelFormat != PS2PixelFormat.PSMCT24 && pixelFormat != PS2PixelFormat.PSMCT16 && pixelFormat != PS2PixelFormat.PSMCT16S);
        }

        public static byte ScaleAlpha(byte a)
        {
            return (byte)Math.Min((255.0f * (a / 128.0f)), 0xFF);
        }

        public static byte[] ReadPaletteData(EndianBinaryReader reader, PS2PixelFormat pixelFormat, PS2PixelFormat paletteFormat)
        {
            int colorCount = (pixelFormat == PS2PixelFormat.PSMT4 ? 16 : (pixelFormat == PS2PixelFormat.PSMT8 ? 256 : 0));
            byte[] tempPalette = new byte[colorCount * 4];

            byte r, g, b, a;
            for (int i = 0; i < tempPalette.Length; i += 4)
            {
                if (paletteFormat == PS2PixelFormat.PSMCT32)
                {
                    uint color = reader.ReadUInt32();
                    r = (byte)color;
                    g = (byte)(color >> 8);
                    b = (byte)(color >> 16);
                    a = ScaleAlpha((byte)(color >> 24));
                }
                else
                {
                    ushort color = reader.ReadUInt16();
                    r = (byte)((color & 0x001F) << 3);
                    g = (byte)(((color & 0x03E0) >> 5) << 3);
                    b = (byte)(((color & 0x7C00) >> 10) << 3);
                    a = ScaleAlpha((byte)(i == 0 ? 0 : 0x80));
                }

                tempPalette[i + 0] = a;
                tempPalette[i + 1] = r;
                tempPalette[i + 2] = g;
                tempPalette[i + 3] = b;
            }

            byte[] paletteData;

            if (colorCount == 256)
            {
                paletteData = new byte[tempPalette.Length];
                for (int i = 0; i < paletteData.Length; i += (32 * 4))
                {
                    Buffer.BlockCopy(tempPalette, i + (0 * 4), paletteData, i + (0 * 4), (8 * 4));
                    Buffer.BlockCopy(tempPalette, i + (8 * 4), paletteData, i + (16 * 4), (8 * 4));
                    Buffer.BlockCopy(tempPalette, i + (16 * 4), paletteData, i + (8 * 4), (8 * 4));
                    Buffer.BlockCopy(tempPalette, i + (24 * 4), paletteData, i + (24 * 4), (8 * 4));
                }
            }
            else
                paletteData = tempPalette;

            return paletteData;
        }
    }
}
