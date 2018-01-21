using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

using Scarlet.Drawing;
using Scarlet.IO;

namespace Scarlet.IO.ImageFormats
{
    // TODO: more imagetypes, verify mipmap stuff, figure out unknown DXT-ish datatype, etc.

    // "E:\[SSD User Data]\Desktop\Misc Stuff\PSP K-ON Test\PSP_GAME" -keep
    // "E:\[SSD User Data]\Desktop\Misc Stuff\PSP K-ON Test\Unimplemented stuff test"

    public enum GbixDataType : byte
    {
        Abgr1555 = 0x00,
        Bgr565 = 0x01,
        Abgr4444 = 0x02,
        Abgr8888 = 0x03,

        DXT1Rgba = 0x0A,
        DXT_Unknown = 0x0C,
    }

    public enum GbixImageType : byte
    {
        Linear = 0x80,
        LinearMipmaps = 0x81,
        // Unknown? 0x82
        // Unknown w/ mipmaps? 0x83
        // Unknown2? 0x84
        // Unknown2 w/ mipmaps? 0x85
        Indexed4 = 0x86,
        // Indexed4 w/ mipmaps? 0x87
        Indexed4_Alt = 0x88,
        // Indexed4 alt w/ mipmaps? 0x89
        Indexed8 = 0x8A,
        // Indexed8 w/ mipmaps? 0x8B
        Indexed8_Alt = 0x8C,
        // Indexed8 alt w/ mipmaps? 0x8D
        // Unknown3? 0x8E
        // Unknown3 w/ mipmaps? 0x8F
    }

    [MagicNumber("GBIX", 0x00)]
    public class GBIX : ImageFormat
    {
        static readonly Dictionary<GbixDataType, int> dataTypeSizeMap = new Dictionary<GbixDataType, int>()
        {
            { GbixDataType.Abgr1555, 16 },
            { GbixDataType.Bgr565, 16 },
            { GbixDataType.Abgr4444, 16 },
            { GbixDataType.Abgr8888, 32 },
        };

        static readonly Dictionary<GbixDataType, PixelDataFormat> dataTypeFormatMap = new Dictionary<GbixDataType, PixelDataFormat>()
        {
            { GbixDataType.Abgr1555, PixelDataFormat.FormatAbgr1555 },
            { GbixDataType.Bgr565, PixelDataFormat.FormatBgr565 },
            { GbixDataType.Abgr4444, PixelDataFormat.FormatAbgr4444 },
            { GbixDataType.Abgr8888, PixelDataFormat.FormatAbgr8888 },
            { GbixDataType.DXT1Rgba, PixelDataFormat.FormatDXT1Rgba_PSP },
            //{ GbixDataType.DXT_Unknown, PixelDataFormat.FormatDXT3 },         // TODO: disabled for now, better no image than a garbage image imo
        };

        public string MagicNumber { get; private set; }
        public uint Unknown0x04 { get; private set; }
        public uint Unknown0x08 { get; private set; }
        public uint Unknown0x0C { get; private set; }

        public string MagicNumberUVRT { get; private set; }
        public uint UVRTDataSize { get; private set; }
        public GbixDataType DataType { get; private set; }
        public GbixImageType ImageType { get; private set; }
        public ushort Unknown0x1A { get; private set; }
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }

        public byte[] PaletteData { get; private set; }
        public byte[] PixelData { get; private set; }

        bool isIndexed, isCompressed, hasMipmaps;

        List<MipmapLevel> mipmapData;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            Unknown0x04 = reader.ReadUInt32();
            Unknown0x08 = reader.ReadUInt32();
            Unknown0x0C = reader.ReadUInt32();

            MagicNumberUVRT = Encoding.ASCII.GetString(reader.ReadBytes(4));
            UVRTDataSize = reader.ReadUInt32();
            DataType = (GbixDataType)reader.ReadByte();
            ImageType = (GbixImageType)reader.ReadByte();
            Unknown0x1A = reader.ReadUInt16();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();

            // TODO: more conditions once more values for ImageType are known!
            isIndexed = (ImageType != GbixImageType.Linear && ImageType != GbixImageType.LinearMipmaps);
            isCompressed = ((ImageType == GbixImageType.Linear || ImageType == GbixImageType.LinearMipmaps) && (DataType == GbixDataType.DXT1Rgba || DataType == GbixDataType.DXT_Unknown));
            hasMipmaps = (ImageType == GbixImageType.LinearMipmaps);

            if (isIndexed)
            {
                if (!dataTypeSizeMap.ContainsKey(DataType))
                    throw new NotImplementedException(string.Format("GBIX: unimplemented palette data size for {0} (0x{1:X2})", DataType, (byte)DataType));

                switch (ImageType)
                {
                    case GbixImageType.Indexed4:
                    case GbixImageType.Indexed4_Alt:
                        PaletteData = reader.ReadBytes(16 * (dataTypeSizeMap[DataType] / 8));
                        break;

                    case GbixImageType.Indexed8:
                    case GbixImageType.Indexed8_Alt:
                        PaletteData = reader.ReadBytes(256 * (dataTypeSizeMap[DataType] / 8));
                        break;

                    default:
                        throw new NotImplementedException(string.Format("GBIX: unimplemented indexed image type {0} (0x{1:X2})", ImageType, (byte)ImageType));
                }
            }

            PixelData = reader.ReadBytes((int)(UVRTDataSize - 8 - (PaletteData != null ? PaletteData.Length : 0)));

            mipmapData = new List<MipmapLevel>();
            if (hasMipmaps)
            {
                int mipWidth = Width;
                int mipHeight = Height;
                int mipCount = 0, readOffset = 0;

                int bitsPerPixel = dataTypeSizeMap[DataType];

                while (mipWidth > 0 && mipHeight > 0 && readOffset < PixelData.Length)
                {
                    mipWidth = (Width >> mipCount);
                    mipHeight = (Height >> mipCount);
                    int mipDataSize = (((bitsPerPixel * (Width >> mipCount)) * (Height >> mipCount)) / 8);

                    byte[] mipPixelData = new byte[mipDataSize];
                    Buffer.BlockCopy(PixelData, readOffset, mipPixelData, 0, mipPixelData.Length);
                    readOffset += mipPixelData.Length;

                    mipmapData.Add(new MipmapLevel(mipWidth, mipHeight, mipPixelData));

                    mipCount++;
                }
            }
            else
            {
                mipmapData.Add(new MipmapLevel(Width, Height, PixelData));
            }
        }

        public override int GetImageCount()
        {
            return mipmapData.Count;
        }

        public override int GetPaletteCount()
        {
            return (PaletteData != null ? 1 : 0);
        }

        protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
        {
            ImageBinary imageBinary = new ImageBinary();

            PixelDataFormat pixelDataFormat, paletteDataFormat;

            switch (ImageType)
            {
                // Non-indexed image
                case GbixImageType.Linear:
                case GbixImageType.LinearMipmaps:
                    if (!dataTypeFormatMap.ContainsKey(DataType))
                        throw new NotImplementedException(string.Format("GBIX: unimplemented pixel data type {0} (0x{1:X2})", DataType, (byte)DataType));

                    pixelDataFormat = dataTypeFormatMap[DataType];
                    if (!isCompressed) pixelDataFormat |= PixelDataFormat.PixelOrderingSwizzledPSP;
                    paletteDataFormat = PixelDataFormat.Undefined;
                    break;

                // Indexed image (4-bit)
                case GbixImageType.Indexed4:
                case GbixImageType.Indexed4_Alt:
                    if (!dataTypeFormatMap.ContainsKey(DataType))
                        throw new NotImplementedException(string.Format("GBIX: unimplemented palette data type {0} (0x{1:X2})", DataType, (byte)DataType));

                    pixelDataFormat = PixelDataFormat.FormatIndexed4 | PixelDataFormat.PixelOrderingSwizzledPSP;
                    paletteDataFormat = dataTypeFormatMap[DataType];
                    break;

                // Indexed image (8-bit)
                case GbixImageType.Indexed8:
                case GbixImageType.Indexed8_Alt:
                    if (!dataTypeFormatMap.ContainsKey(DataType))
                        throw new NotImplementedException(string.Format("GBIX: unimplemented palette data type {0} (0x{1:X2})", DataType, (byte)DataType));

                    pixelDataFormat = PixelDataFormat.FormatIndexed8 | PixelDataFormat.PixelOrderingSwizzledPSP;
                    paletteDataFormat = dataTypeFormatMap[DataType];
                    break;

                default:
                    throw new NotImplementedException(string.Format("GBIX: unimplemented image type {0} (0x{1:X2})", ImageType, (byte)ImageType));
            }

            imageBinary.Width = mipmapData[imageIndex].Width;
            imageBinary.Height = mipmapData[imageIndex].Height;
            imageBinary.InputPaletteFormat = paletteDataFormat;
            imageBinary.InputPixelFormat = pixelDataFormat;
            imageBinary.InputEndianness = Endian.LittleEndian;

            imageBinary.AddInputPalette(PaletteData);
            imageBinary.AddInputPixels(mipmapData[imageIndex].PixelData);

            return imageBinary.GetBitmap(0, 0);
        }
    }
}
