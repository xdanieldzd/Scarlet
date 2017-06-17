using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Scarlet.Drawing;
using Scarlet.IO;

namespace Scarlet.Platform.Nintendo
{
    // TODO: indexed formats, the oddballs RGB5A3 & RGBA8...

    public enum GCNTextureFormat : byte
    {
        I4 = 0x00,
        I8 = 0x01,
        IA4 = 0x02,
        IA8 = 0x03,
        RGB565 = 0x04,
        RGB5A3 = 0x05,
        RGBA8 = 0x06,
        CI4 = 0x08,
        CI8 = 0x09,
        CI14 = 0x0A,
        CMPR = 0x0E
    }

    public enum GCNPaletteFormat : byte
    {
        IA8 = 0x00,
        RGB565 = 0x01,
        RGB5A3 = 0x02
    }

    public static class GCN
    {
        static readonly Dictionary<GCNTextureFormat, PixelDataFormat> pixelFormatMap = new Dictionary<GCNTextureFormat, PixelDataFormat>()
        {
            { GCNTextureFormat.I4, PixelDataFormat.FormatLuminance4 },
            { GCNTextureFormat.I8, PixelDataFormat.FormatLuminance8 },
            { GCNTextureFormat.IA4, PixelDataFormat.FormatLuminanceAlpha44 },
            { GCNTextureFormat.IA8, PixelDataFormat.FormatLuminanceAlpha88 },
            { GCNTextureFormat.RGB565, PixelDataFormat.FormatRgb565 },
            { GCNTextureFormat.CMPR, PixelDataFormat.FormatDXT1Rgb },
        };

        static readonly Dictionary<GCNPaletteFormat, PixelDataFormat> paletteFormatMap = new Dictionary<GCNPaletteFormat, PixelDataFormat>()
        {
            { GCNPaletteFormat.IA8, PixelDataFormat.FormatLuminanceAlpha88 },
            { GCNPaletteFormat.RGB565, PixelDataFormat.FormatRgb565 },

        };

        public static PixelDataFormat GetPixelDataFormat(GCNTextureFormat pixelFormat)
        {
            if (!pixelFormatMap.ContainsKey(pixelFormat)) throw new Exception("No matching pixel data format known");
            return pixelFormatMap[pixelFormat];
        }

        public static PixelDataFormat GetPaletteDataFormat(GCNPaletteFormat paletteFormat)
        {
            if (!paletteFormatMap.ContainsKey(paletteFormat)) throw new Exception("No matching palette data format known");
            return paletteFormatMap[paletteFormat];
        }
    }
}
