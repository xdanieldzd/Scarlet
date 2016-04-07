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

    public static class N3DS
    {
        static readonly Dictionary<Tuple<PicaDataType, PicaPixelFormat>, PixelDataFormat> formatMap = new Dictionary<Tuple<PicaDataType, PicaPixelFormat>, PixelDataFormat>()
        {
            /* RGBA4444 */  { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.UnsignedShort4444, PicaPixelFormat.RGBANativeDMP), PixelDataFormat.FormatRgba4444 | PixelDataFormat.PostProcessUntile_3DS },
            /* RGBA5551 */  { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.UnsignedShort5551, PicaPixelFormat.RGBANativeDMP), PixelDataFormat.FormatRgba5551 | PixelDataFormat.PostProcessUntile_3DS },
            /* RGBA8888 */  { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.UnsignedByte, PicaPixelFormat.RGBANativeDMP), PixelDataFormat.FormatRgba8888 | PixelDataFormat.PostProcessUntile_3DS },
            /* RGB565 */    { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.UnsignedShort565, PicaPixelFormat.RGBNativeDMP), PixelDataFormat.FormatRgb565 | PixelDataFormat.PostProcessUntile_3DS },
            /* RGB888 */    { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.UnsignedByte, PicaPixelFormat.RGBNativeDMP), PixelDataFormat.FormatRgb888 | PixelDataFormat.PostProcessUntile_3DS },
            /* ETC1 */      { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.UnsignedByte, PicaPixelFormat.ETC1RGB8NativeDMP), PixelDataFormat.FormatETC1_3DS },
            /* ETC1_A4 */   { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.UnsignedByte, PicaPixelFormat.ETC1AlphaRGB8A4NativeDMP), PixelDataFormat.FormatETC1A4_3DS },
            /* A8 */        { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.UnsignedByte, PicaPixelFormat.AlphaNativeDMP), PixelDataFormat.FormatAlpha8 | PixelDataFormat.PostProcessUntile_3DS },
            /* A4 */        { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.Unsigned4BitsDMP, PicaPixelFormat.AlphaNativeDMP), PixelDataFormat.FormatAlpha4 | PixelDataFormat.PostProcessUntile_3DS },
            /* L8 */        { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.UnsignedByte, PicaPixelFormat.LuminanceNativeDMP), PixelDataFormat.FormatLuminance8 | PixelDataFormat.PostProcessUntile_3DS },
            /* L4 */        { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.Unsigned4BitsDMP, PicaPixelFormat.LuminanceNativeDMP), PixelDataFormat.FormatLuminance4 | PixelDataFormat.PostProcessUntile_3DS },
            /* LA88 */      { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.UnsignedByte, PicaPixelFormat.LuminanceAlphaNativeDMP), PixelDataFormat.FormatLuminanceAlpha88 | PixelDataFormat.PostProcessUntile_3DS },
            /* LA44 */      { new Tuple<PicaDataType, PicaPixelFormat>(PicaDataType.UnsignedByte44DMP, PicaPixelFormat.LuminanceAlphaNativeDMP), PixelDataFormat.FormatLuminanceAlpha44 | PixelDataFormat.PostProcessUntile_3DS }
        };

        public static PixelDataFormat GetPixelDataFormat(PicaDataType dataType, PicaPixelFormat pixelFormat)
        {
            Tuple<PicaDataType, PicaPixelFormat> tuple = new Tuple<PicaDataType, PicaPixelFormat>(dataType, pixelFormat);
            if (!formatMap.ContainsKey(tuple)) throw new Exception("No matching pixel data format known");
            return formatMap[tuple];
        }
    }
}
