using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Imaging;

namespace Scarlet.Drawing
{
    /// <summary>
    /// Specifies components of a pixel data format
    /// </summary>
    [Flags]
    public enum PixelDataFormat : ulong
    {
        /*
         * BPP 000000000000001F
         * Cha 000000000007FFE0
         * Red 0000000000380000
         * Grn 0000000003C00000
         * Blu 000000001C000000
         * Alp 00000000E0000000
         * Spc 00000FFF00000000
         * Pst 0000F00000000000
         * Fil 000F000000000000
         * Rsv FFF0000000000000
         */

        /// <summary>
        /// Format has 0 bits per pixel (invalid)
        /// </summary>
        Bpp0 = Undefined,

        /// <summary>
        /// Format has 4 bits per pixel
        /// </summary>
        Bpp4 = ((ulong)1 << 0),

        /// <summary>
        /// Format has 8 bits per pixel
        /// </summary>
        Bpp8 = ((ulong)1 << 1),

        /// <summary>
        /// Format has 16 bits per pixel
        /// </summary>
        Bpp16 = ((ulong)1 << 2),

        /// <summary>
        /// Format has 24 bits per pixel
        /// </summary>
        Bpp24 = ((ulong)1 << 3),

        /// <summary>
        /// Format has 32 bits per pixel
        /// </summary>
        Bpp32 = ((ulong)1 << 4),

        /// <summary>
        /// Mask for extracting BPP value
        /// </summary>
        MaskBpp = ((((ulong)1 << 5) - 1) << 0), /* 000000000000001F */

        /// <summary>
        /// Format has no color channels (invalid)
        /// </summary>
        ChannelsNone = Undefined,

        /// <summary>
        /// Format has channels in RGB order
        /// </summary>
        ChannelsRgb = ((ulong)1 << 5),

        /// <summary>
        /// Format has channels in BGR order
        /// </summary>
        ChannelsBgr = ((ulong)1 << 6),

        /// <summary>
        /// Format has channels in RGBA order
        /// </summary>
        ChannelsRgba = ((ulong)1 << 7),

        /// <summary>
        /// Format has channels in BGRA order
        /// </summary>
        ChannelsBgra = ((ulong)1 << 8),

        /// <summary>
        /// Format has channels in ARGB order
        /// </summary>
        ChannelsArgb = ((ulong)1 << 9),

        /// <summary>
        /// Format has channels in ABGR order
        /// </summary>
        ChannelsAbgr = ((ulong)1 << 10),

        /// <summary>
        /// Format has channels in RGB order, with trailing dummy A channel
        /// </summary>
        ChannelsRgbx = ((ulong)1 << 11),

        /// <summary>
        /// Format has channels in BGR order, with trailing dummy A channel
        /// </summary>
        ChannelsBgrx = ((ulong)1 << 12),

        /// <summary>
        /// Format has channels in RGB order, with leading dummy A channel
        /// </summary>
        ChannelsXrgb = ((ulong)1 << 13),

        /// <summary>
        /// Format has channels in BGR order, with leading dummy A channel
        /// </summary>
        ChannelsXbgr = ((ulong)1 << 14),

        /// <summary>
        /// Format has channels in L order
        /// </summary>
        ChannelsLuminance = ((ulong)1 << 15),

        /// <summary>
        /// Format has channels in A order
        /// </summary>
        ChannelsAlpha = ((ulong)1 << 16),

        /// <summary>
        /// Format has channels in LA order
        /// </summary>
        ChannelsLuminanceAlpha = ((ulong)1 << 17),

        /// <summary>
        /// Format is indexed color
        /// </summary>
        ChannelsIndexed = ((ulong)1 << 18),

        /// <summary>
        /// Mask for extracting channels value
        /// </summary>
        MaskChannels = ((((ulong)1 << 14) - 1) << 5), /* 000000000007FFE0 */

        /// <summary>
        /// Format has 0 bits in red channel
        /// </summary>
        RedBits0 = Undefined,

        /// <summary>
        /// Format has 8 bits in red channel
        /// </summary>
        RedBits8 = ((ulong)1 << 19),

        /// <summary>
        /// Format has 5 bits in red channel
        /// </summary>
        RedBits5 = ((ulong)1 << 20),

        /// <summary>
        /// Format has 4 bits in red channel
        /// </summary>
        RedBits4 = ((ulong)1 << 21),

        /// <summary>
        /// Mask for extracting red bits value
        /// </summary>
        MaskRedBits = ((((ulong)1 << 3) - 1) << 19), /* 0000000000380000 */

        /// <summary>
        /// Format has 0 bits in green channel
        /// </summary>
        GreenBits0 = Undefined,

        /// <summary>
        /// Format has 8 bits in green channel
        /// </summary>
        GreenBits8 = ((ulong)1 << 22),

        /// <summary>
        /// Format has 6 bits in green channel
        /// </summary>
        GreenBits6 = ((ulong)1 << 23),

        /// <summary>
        /// Format has 5 bits in green channel
        /// </summary>
        GreenBits5 = ((ulong)1 << 24),

        /// <summary>
        /// Format has 4 bits in green channel
        /// </summary>
        GreenBits4 = ((ulong)1 << 25),

        /// <summary>
        /// Mask for extracting green bits value
        /// </summary>
        MaskGreenBits = ((((ulong)1 << 4) - 1) << 22), /* 0000000003C00000 */

        /// <summary>
        /// Format has 0 bits in blue channel
        /// </summary>
        BlueBits0 = Undefined,

        /// <summary>
        /// Format has 8 bits in blue channel
        /// </summary>
        BlueBits8 = ((ulong)1 << 26),

        /// <summary>
        /// Format has 5 bits in blue channel
        /// </summary>
        BlueBits5 = ((ulong)1 << 27),

        /// <summary>
        /// Format has 4 bits in blue channel
        /// </summary>
        BlueBits4 = ((ulong)1 << 28),

        /// <summary>
        /// Mask for extracting blue bits value
        /// </summary>
        MaskBlueBits = ((((ulong)1 << 3) - 1) << 26), /* 000000001C000000 */

        /// <summary>
        /// Format has 0 bits in alpha channel
        /// </summary>
        AlphaBits0 = Undefined,

        /// <summary>
        /// Format has 8 bits in alpha channel
        /// </summary>
        AlphaBits8 = ((ulong)1 << 29),

        /// <summary>
        /// Format has 4 bits in alpha channel
        /// </summary>
        AlphaBits4 = ((ulong)1 << 30),

        /// <summary>
        /// Format has 1 bit in alpha channel
        /// </summary>
        AlphaBits1 = ((ulong)1 << 31),

        /// <summary>
        /// Mask for extracting alpha bits value
        /// </summary>
        MaskAlphaBits = ((((ulong)1 << 3) - 1) << 29), /* 00000000E0000000 */

        /// <summary>
        /// Format has 0 bits in luminance channel (same as red bits enumeration)
        /// </summary>
        LuminanceBits0 = RedBits0,

        /// <summary>
        /// Format has 8 bits in luminance channel (same as red bits enumeration)
        /// </summary>
        LuminanceBits8 = RedBits8,

        /// <summary>
        /// Format has 5 bits in luminance channel (same as red bits enumeration)
        /// </summary>
        LuminanceBits5 = RedBits5,

        /// <summary>
        /// Format has 4 bits in luminance channel (same as red bits enumeration)
        /// </summary>
        LuminanceBits4 = RedBits4,

        /// <summary>
        /// Mask for extracting luminance bits value (same as red bits enumeration)
        /// </summary>
        MaskLuminanceBits = MaskRedBits,

        /// <summary>
        /// Special format with 3DS-style ETC1 data
        /// </summary>
        FormatETC1_3DS = ((ulong)1 << 32),

        /// <summary>
        /// Special format with 3DS-style ETC1A4 data
        /// </summary>
        FormatETC1A4_3DS = ((ulong)1 << 33),

        /// <summary>
        /// Special format with DXT1 data
        /// </summary>
        FormatDXT1 = ((ulong)1 << 34),

        /// <summary>
        /// Special format with DXT1 data, alternate
        /// </summary>
        FormatDXT1Alt = ((ulong)1 << 35),

        /// <summary>
        /// Special format with DXT3 data
        /// </summary>
        FormatDXT3 = ((ulong)1 << 36),

        /// <summary>
        /// Special format with DXT3 data, alternate
        /// </summary>
        FormatDXT3Alt = ((ulong)1 << 37),

        /// <summary>
        /// Special format with DXT5 data
        /// </summary>
        FormatDXT5 = ((ulong)1 << 38),

        /// <summary>
        /// Special format with DXT5 data, alternate
        /// </summary>
        FormatDXT5Alt = ((ulong)1 << 39),

        /// <summary>
        /// Special format with Vita-style PVRT2 data
        /// </summary>
        FormatPVRT2_Vita = ((ulong)1 << 40),

        /// <summary>
        /// Special format with Vita-style PVRT4 data
        /// </summary>
        FormatPVRT4_Vita = ((ulong)1 << 41),

        /// <summary>
        /// Mask for extracting special format value
        /// </summary>
        MaskSpecial = ((((ulong)1 << 12) - 1) << 32), /* 00000FFF00000000 */

        /// <summary>
        /// Format does not need post-processing
        /// </summary>
        PostProcessNone = Undefined,

        /// <summary>
        /// Format needs 3DS-style untiling
        /// </summary>
        PostProcessUntile_3DS = ((ulong)1 << 44),

        /// <summary>
        /// Format needs PSP-style untiling
        /// </summary>
        PostProcessUntile_PSP = ((ulong)1 << 45),

        /// <summary>
        /// Format needs Vita-style unswizzling
        /// </summary>
        PostProcessUnswizzle_Vita = ((ulong)1 << 46),

        /// <summary>
        /// Format needs PSP-style unswizzling
        /// </summary>
        PostProcessUnswizzle_PSP = ((ulong)1 << 47),

        /// <summary>
        /// Mask for extracting post-processing value
        /// </summary>
        MaskPostProcess = ((((ulong)1 << 4) - 1) << 44), /* 0000F00000000000 */

        /// <summary>
        /// Format will not apply filtering
        /// </summary>
        FilterNone = Undefined,

        /// <summary>
        /// Format applies simple, ordered dither filter
        /// </summary>
        FilterOrderedDither = ((ulong)1 << 48),

        /// <summary>
        /// Mask for extracting filtering value
        /// </summary>
        MaskFilter = ((((ulong)1 << 4) - 1) << 48), /* 000F000000000000 */

        /// <summary>
        /// Reserved
        /// </summary>
        Reserved = Undefined,

        /// <summary>
        /// Mask for extracting reserved bits
        /// </summary>
        MaskReserved = ((((ulong)1 << 12) - 1) << 52), /* FFF0000000000000 */

        /// <summary>
        /// Format is 24-bit RGB888
        /// </summary>
        FormatRgb888 = (Bpp24 | ChannelsRgb | RedBits8 | GreenBits8 | BlueBits8 | AlphaBits0),

        /// <summary>
        /// Format is 16-bit RGB565
        /// </summary>
        FormatRgb565 = (Bpp16 | ChannelsRgb | RedBits5 | GreenBits6 | BlueBits5 | AlphaBits0),

        /// <summary>
        /// Format is 24-bit BGR888
        /// </summary>
        FormatBgr888 = (Bpp24 | ChannelsBgr | RedBits8 | GreenBits8 | BlueBits8 | AlphaBits0),

        /// <summary>
        /// Format is 16-bit BGR565
        /// </summary>
        FormatBgr565 = (Bpp16 | ChannelsBgr | RedBits5 | GreenBits6 | BlueBits5 | AlphaBits0),

        /// <summary>
        /// Format is 32-bit RGBA8888
        /// </summary>
        FormatRgba8888 = (Bpp32 | ChannelsRgba | RedBits8 | GreenBits8 | BlueBits8 | AlphaBits8),

        /// <summary>
        /// Format is 16-bit RGBA5551
        /// </summary>
        FormatRgba5551 = (Bpp16 | ChannelsRgba | RedBits5 | GreenBits5 | BlueBits5 | AlphaBits1),

        /// <summary>
        /// Format is 16-bit RGBA4444
        /// </summary>
        FormatRgba4444 = (Bpp16 | ChannelsRgba | RedBits4 | GreenBits4 | BlueBits4 | AlphaBits4),

        /// <summary>
        /// Format is 32-bit RGBA8888
        /// </summary>
        FormatBgra8888 = (Bpp32 | ChannelsBgra | RedBits8 | GreenBits8 | BlueBits8 | AlphaBits8),

        /// <summary>
        /// Format is 16-bit BGRA5551
        /// </summary>
        FormatBgra5551 = (Bpp16 | ChannelsBgra | RedBits5 | GreenBits5 | BlueBits5 | AlphaBits1),

        /// <summary>
        /// Format is 16-bit BGRA4444
        /// </summary>
        FormatBgra4444 = (Bpp16 | ChannelsBgra | RedBits4 | GreenBits4 | BlueBits4 | AlphaBits4),

        /// <summary>
        /// Format is 32-bit ARGB8888
        /// </summary>
        FormatArgb8888 = (Bpp32 | ChannelsArgb | RedBits8 | GreenBits8 | BlueBits8 | AlphaBits8),

        /// <summary>
        /// Format is 16-bit ARGB1555
        /// </summary>
        FormatArgb1555 = (Bpp16 | ChannelsArgb | RedBits5 | GreenBits5 | BlueBits5 | AlphaBits1),

        /// <summary>
        /// Format is 16-bit ARGB4444
        /// </summary>
        FormatArgb4444 = (Bpp16 | ChannelsArgb | RedBits4 | GreenBits4 | BlueBits4 | AlphaBits4),

        /// <summary>
        /// Format is 32-bit ABGR8888
        /// </summary>
        FormatAbgr8888 = (Bpp32 | ChannelsAbgr | RedBits8 | GreenBits8 | BlueBits8 | AlphaBits8),

        /// <summary>
        /// Format is 16-bit ABGR1555
        /// </summary>
        FormatAbgr1555 = (Bpp16 | ChannelsAbgr | RedBits5 | GreenBits5 | BlueBits5 | AlphaBits1),

        /// <summary>
        /// Format is 16-bit ABGR4444
        /// </summary>
        FormatAbgr4444 = (Bpp16 | ChannelsAbgr | RedBits4 | GreenBits4 | BlueBits4 | AlphaBits4),

        /// <summary>
        /// Format is 32-bit RGB888 with dummy trailing 8-bit alpha channel
        /// </summary>
        FormatRgbx8888 = (Bpp32 | ChannelsRgbx | RedBits8 | GreenBits8 | BlueBits8 | AlphaBits8),

        /// <summary>
        /// Format is 32-bit BGR888 with dummy trailing 8-bit alpha channel
        /// </summary>
        FormatBgrx8888 = (Bpp32 | ChannelsBgrx | RedBits8 | GreenBits8 | BlueBits8 | AlphaBits8),

        /// <summary>
        /// Format is 32-bit RGB888 with dummy leading 8-bit alpha channel
        /// </summary>
        FormatXrgb8888 = (Bpp32 | ChannelsXrgb | RedBits8 | GreenBits8 | BlueBits8 | AlphaBits8),

        /// <summary>
        /// Format is 32-bit BGR888 with dummy leading 8-bit alpha channel
        /// </summary>
        FormatXbgr8888 = (Bpp32 | ChannelsXbgr | RedBits8 | GreenBits8 | BlueBits8 | AlphaBits8),

        /// <summary>
        /// Format is 16-bit LA88
        /// </summary>
        FormatLuminanceAlpha88 = (Bpp16 | ChannelsLuminanceAlpha | RedBits8 | GreenBits0 | BlueBits0 | AlphaBits8),

        /// <summary>
        /// Format is 8-bit LA44
        /// </summary>
        FormatLuminanceAlpha44 = (Bpp8 | ChannelsLuminanceAlpha | RedBits4 | GreenBits0 | BlueBits0 | AlphaBits4),

        /// <summary>
        /// Format is 8-bit L8
        /// </summary>
        FormatLuminance8 = (Bpp8 | ChannelsLuminance | RedBits8 | GreenBits0 | BlueBits0 | AlphaBits0),

        /// <summary>
        /// Format is 4-bit L4
        /// </summary>
        FormatLuminance4 = (Bpp4 | ChannelsLuminance | RedBits4 | GreenBits0 | BlueBits0 | AlphaBits0),

        /// <summary>
        /// Format is 8-bit A8
        /// </summary>
        FormatAlpha8 = (Bpp8 | ChannelsAlpha | RedBits0 | GreenBits0 | BlueBits0 | AlphaBits8),

        /// <summary>
        /// Format is 4-bit A4
        /// </summary>
        FormatAlpha4 = (Bpp4 | ChannelsAlpha | RedBits0 | GreenBits0 | BlueBits0 | AlphaBits4),

        /// <summary>
        /// Format is 8-bit indexed
        /// </summary>
        FormatIndexed8 = (Bpp8 | ChannelsIndexed | RedBits0 | GreenBits0 | BlueBits0 | AlphaBits0),

        /// <summary>
        /// Format is 4-bit indexed
        /// </summary>
        FormatIndexed4 = (Bpp4 | ChannelsIndexed | RedBits0 | GreenBits0 | BlueBits0 | AlphaBits0),

        /// <summary>
        /// Undefined value
        /// </summary>
        Undefined = 0x00000000
    }

    internal static class Constants
    {
        internal static Dictionary<PixelDataFormat, int> RealBitsPerPixel = new Dictionary<PixelDataFormat, int>()
        {
            { PixelDataFormat.Bpp4, 4 },
            { PixelDataFormat.Bpp8, 8 },
            { PixelDataFormat.Bpp16, 16 },
            { PixelDataFormat.Bpp24, 24 },
            { PixelDataFormat.Bpp32, 32 }
        };

        internal static Dictionary<PixelDataFormat, int> InputBitsPerPixel = new Dictionary<PixelDataFormat, int>()
        {
            { PixelDataFormat.Bpp4, 8 },
            { PixelDataFormat.Bpp8, 8 },
            { PixelDataFormat.Bpp16, 16 },
            { PixelDataFormat.Bpp24, 24 },
            { PixelDataFormat.Bpp32, 32 }
        };

        internal static Dictionary<PixelDataFormat, int> BitsPerChannel = new Dictionary<PixelDataFormat, int>()
        {
            { PixelDataFormat.RedBits8, 8 },
            { PixelDataFormat.RedBits5, 5 },
            { PixelDataFormat.RedBits4, 4 },
            { PixelDataFormat.GreenBits8, 8 },
            { PixelDataFormat.GreenBits6, 6 },
            { PixelDataFormat.GreenBits5, 5 },
            { PixelDataFormat.GreenBits4, 4 },
            { PixelDataFormat.BlueBits8, 8 },
            { PixelDataFormat.BlueBits5, 5 },
            { PixelDataFormat.BlueBits4, 4 },
            { PixelDataFormat.AlphaBits8, 8 },
            { PixelDataFormat.AlphaBits4, 4 },
            { PixelDataFormat.AlphaBits1, 1 },
        };

        internal static int[,] DitheringBayerMatrix8x8 = new int[8, 8]
        {
            {  1, 49, 13, 61,  4, 52, 16, 64 },
            { 33, 17, 45, 29, 36, 20, 48, 32 },
            {  9, 57,  5, 53, 12, 60,  8, 56 },
            { 41, 25, 37, 21, 44, 28, 40, 24 },
            {  3, 51, 15, 63,  2, 50, 14, 62 },
            { 35, 19, 47, 31, 34, 18, 46, 30 },
            { 11, 59,  7, 55, 10, 58,  6, 54 },
            { 43, 27, 39, 23, 42, 26, 38, 22 }
        };
    }
}
