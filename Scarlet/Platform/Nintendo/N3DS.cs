using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Scarlet.Drawing;

namespace Scarlet.Platform.Nintendo
{
    public enum PicaDataType : uint
    {
        Byte = 0x1400,
        UnsignedByte = 0x1401,
        Short = 0x1402,
        UnsignedShort = 0x1403,
        Int = 0x1404,
        UnsignedInt = 0x1405,
        Float = 0x1406,
        UnsignedByte44DMP = 0x6760,
        Unsigned4BitsDMP = 0x6761,
        UnsignedShort4444 = 0x8033,
        UnsignedShort5551 = 0x8034,
        UnsignedShort565 = 0x8363
    };

    public enum PicaPixelFormat : uint
    {
        RGBANativeDMP = 0x6752,
        RGBNativeDMP = 0x6754,
        AlphaNativeDMP = 0x6756,
        LuminanceNativeDMP = 0x6757,
        LuminanceAlphaNativeDMP = 0x6758,
        ETC1RGB8NativeDMP = 0x675A,
        ETC1AlphaRGB8A4NativeDMP = 0x675B
    };

    public enum SdkPixelFormat : ushort
    {
        RGBA8 = 0x0000,
        RGB8 = 0x0001,
        RGBA5551 = 0x0002,
        RGB565 = 0x0003,
        RGBA4 = 0x0004,
        LA8 = 0x0005,
        HILO8 = 0x0006,
        L8 = 0x0007,
        A8 = 0x0008,
        LA4 = 0x0009,
        L4 = 0x000A,
        A4 = 0x000B,
        ETC1 = 0x000C,
        ETC1A4 = 0x000D
    };

    public static class N3DS
    {
        static readonly Dictionary<Tuple<PicaDataType, PicaPixelFormat>, PixelDataFormat> formatMapPica = new Dictionary<Tuple<PicaDataType, PicaPixelFormat>, PixelDataFormat>()
        {
            /* RGBA4444 */  { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.UnsignedShort4444, PicaPixelFormat.RGBANativeDMP), PixelDataFormat.FormatRgba4444 | PixelDataFormat.PixelOrderingTiled3DS },
            /* RGBA5551 */  { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.UnsignedShort5551, PicaPixelFormat.RGBANativeDMP), PixelDataFormat.FormatRgba5551 | PixelDataFormat.PixelOrderingTiled3DS },
            /* RGBA8888 */  { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.UnsignedByte, PicaPixelFormat.RGBANativeDMP), PixelDataFormat.FormatRgba8888 | PixelDataFormat.PixelOrderingTiled3DS },
            /* RGB565 */    { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.UnsignedShort565, PicaPixelFormat.RGBNativeDMP), PixelDataFormat.FormatRgb565 | PixelDataFormat.PixelOrderingTiled3DS },
            /* RGB888 */    { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.UnsignedByte, PicaPixelFormat.RGBNativeDMP), PixelDataFormat.FormatRgb888 | PixelDataFormat.PixelOrderingTiled3DS },
            /* ETC1 */      { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.UnsignedByte, PicaPixelFormat.ETC1RGB8NativeDMP), PixelDataFormat.FormatETC1_3DS },
            /* ETC1_A4 */   { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.UnsignedByte, PicaPixelFormat.ETC1AlphaRGB8A4NativeDMP), PixelDataFormat.FormatETC1A4_3DS },
            /* A8 */        { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.UnsignedByte, PicaPixelFormat.AlphaNativeDMP), PixelDataFormat.FormatAlpha8 | PixelDataFormat.PixelOrderingTiled3DS },
            /* A4 */        { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.Unsigned4BitsDMP, PicaPixelFormat.AlphaNativeDMP), PixelDataFormat.FormatAlpha4 | PixelDataFormat.PixelOrderingTiled3DS },
            /* L8 */        { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.UnsignedByte, PicaPixelFormat.LuminanceNativeDMP), PixelDataFormat.FormatLuminance8 | PixelDataFormat.PixelOrderingTiled3DS },
            /* L4 */        { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.Unsigned4BitsDMP, PicaPixelFormat.LuminanceNativeDMP), PixelDataFormat.FormatLuminance4 | PixelDataFormat.PixelOrderingTiled3DS },
            /* LA88 */      { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.UnsignedByte, PicaPixelFormat.LuminanceAlphaNativeDMP), PixelDataFormat.FormatLuminanceAlpha88 | PixelDataFormat.PixelOrderingTiled3DS },
            /* LA44 */      { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.UnsignedByte44DMP, PicaPixelFormat.LuminanceAlphaNativeDMP), PixelDataFormat.FormatLuminanceAlpha44 | PixelDataFormat.PixelOrderingTiled3DS }
        };

        static readonly Dictionary<SdkPixelFormat, PixelDataFormat> formatMapSdk = new Dictionary<SdkPixelFormat, PixelDataFormat>()
        {
            /* RGBA8 */     { SdkPixelFormat.RGBA8, PixelDataFormat.FormatRgba8888 | PixelDataFormat.PixelOrderingTiled3DS },
            /* RGB8 */      { SdkPixelFormat.RGB8, PixelDataFormat.FormatRgb888 | PixelDataFormat.PixelOrderingTiled3DS },
            /* RGBA5551 */  { SdkPixelFormat.RGBA5551, PixelDataFormat.FormatRgba5551 | PixelDataFormat.PixelOrderingTiled3DS },
            /* RGB565 */    { SdkPixelFormat.RGB565, PixelDataFormat.FormatRgb565 | PixelDataFormat.PixelOrderingTiled3DS },
            /* RGBA4 */     { SdkPixelFormat.RGBA4, PixelDataFormat.FormatRgba4444 | PixelDataFormat.PixelOrderingTiled3DS },
            /* LA8 */       { SdkPixelFormat.LA8, PixelDataFormat.FormatLuminanceAlpha88 | PixelDataFormat.PixelOrderingTiled3DS },
            /* HILO8 */     { SdkPixelFormat.HILO8, PixelDataFormat.FormatLuminanceAlpha88 | PixelDataFormat.PixelOrderingTiled3DS },   // FIXME, what IS HILO8 anyway?
            /* L8 */        { SdkPixelFormat.L8, PixelDataFormat.FormatLuminance8 | PixelDataFormat.PixelOrderingTiled3DS },
            /* A8 */        { SdkPixelFormat.A8, PixelDataFormat.FormatAlpha8 | PixelDataFormat.PixelOrderingTiled3DS },
            /* LA4 */       { SdkPixelFormat.LA4, PixelDataFormat.FormatLuminanceAlpha44 | PixelDataFormat.PixelOrderingTiled3DS },
            /* L4 */        { SdkPixelFormat.L4, PixelDataFormat.FormatLuminance4 | PixelDataFormat.PixelOrderingTiled3DS },
            /* A4 */        { SdkPixelFormat.A4, PixelDataFormat.FormatAlpha4 | PixelDataFormat.PixelOrderingTiled3DS },
            /* ETC1 */      { SdkPixelFormat.ETC1, PixelDataFormat.FormatETC1_3DS },
            /* ETC1_A4 */   { SdkPixelFormat.ETC1A4, PixelDataFormat.FormatETC1A4_3DS },
        };

        public static PixelDataFormat GetPixelDataFormat(PicaDataType dataType, PicaPixelFormat pixelFormat)
        {
            Tuple<PicaDataType, PicaPixelFormat> tuple = new Tuple<PicaDataType, PicaPixelFormat>(dataType, pixelFormat);
            if (!formatMapPica.ContainsKey(tuple)) throw new Exception("No matching pixel data format known");
            return formatMapPica[tuple];
        }

        public static PixelDataFormat GetPixelDataFormat(SdkPixelFormat pixelFormat)
        {
            if (!formatMapSdk.ContainsKey(pixelFormat)) throw new Exception("No matching pixel data format known");
            return formatMapSdk[pixelFormat];
        }
    }
}
