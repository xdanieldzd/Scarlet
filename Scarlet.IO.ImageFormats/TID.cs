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
    // TODO: finish & verify me! Can TIDs contain multiple images? If so, how's it work?

    public enum TidFormatUnknownBit0 : byte
    {
        Unset = 0x00,
        Set = 0x01
    }

    public enum TidFormatChannelOrder : byte
    {
        Rgba = 0x00,
        Argb = 0x02
    }

    public enum TidFormatCompressionFlag : byte
    {
        NotCompressed = 0x00,
        Compressed = 0x04
    }

    public enum TidFormatUnknownBit3 : byte
    {
        Unset = 0x00,
        Set = 0x08
    }

    public enum TidFormatVerticalFlip : byte
    {
        NotFlipped = 0x00,
        Flipped = 0x10
    }

    public enum TidFormatUnknownBit5 : byte
    {
        Unset = 0x00,
        Set = 0x20
    }

    public enum TidFormatUnknownBit6 : byte
    {
        Unset = 0x00,
        Set = 0x40
    }

    public enum TidFormatUnknownBit7 : byte
    {
        Unset = 0x00,
        Set = 0x80
    }

    [MagicNumber("TID", 0x00)]
    public class TID : ImageFormat
    {
        public string MagicNumber { get; private set; }
        public byte PixelFormat { get; private set; }
        public uint FileSize { get; private set; }
        public uint DataOffset { get; private set; } // 0x80?
        public uint Unknown0x0C { get; private set; } // 1? Num images?
        public uint Unknown0x10 { get; private set; } // 1? Num filenames?
        public uint Unknown0x14 { get; private set; } // 0x20? Filename offset?
        public uint Unknown0x18 { get; private set; }
        public uint Unknown0x1C { get; private set; }
        public string FileName { get; private set; }
        public uint Unknown0x40 { get; private set; }
        public uint Width { get; private set; }
        public uint Height { get; private set; }
        public uint Unknown0x4C { get; private set; } // 0x20?
        public ushort Unknown0x50 { get; private set; } // 1?
        public ushort Unknown0x52 { get; private set; } // 1?
        public uint Unknown0x54 { get; private set; }
        public uint ImageDataSize { get; private set; }
        public uint Unknown0x5C { get; private set; } // 0x80?
        public uint Unknown0x60 { get; private set; } // mostly 0, 0x04 w/ DXT5?
        public string CompressionFourCC { get; private set; }
        public uint Unknown0x68 { get; private set; } // mostly 0?
        public uint Unknown0x6C { get; private set; }
        public uint Unknown0x70 { get; private set; }
        public uint Unknown0x74 { get; private set; }
        public byte Unknown0x78 { get; private set; } // 0 or 1?
        public byte Unknown0x79 { get; private set; } // 0 or 1?
        public ushort Unknown0x7A { get; private set; } // mostly 0, 0x02 w/ DXT5?
        public ushort Unknown0x7C { get; private set; }
        public ushort Unknown0x7E { get; private set; }

        public byte[] PixelData { get; private set; }

        ImageBinary imageBinary;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(3));
            PixelFormat = reader.ReadByte();
            FileSize = reader.ReadUInt32();
            DataOffset = reader.ReadUInt32();
            Unknown0x0C = reader.ReadUInt32();
            Unknown0x10 = reader.ReadUInt32();
            Unknown0x14 = reader.ReadUInt32();
            Unknown0x18 = reader.ReadUInt32();
            Unknown0x1C = reader.ReadUInt32();
            FileName = Encoding.ASCII.GetString(reader.ReadBytes(0x20)).TrimEnd('\0');
            Unknown0x40 = reader.ReadUInt32();
            Width = reader.ReadUInt32();
            Height = reader.ReadUInt32();
            Unknown0x4C = reader.ReadUInt32();
            Unknown0x50 = reader.ReadUInt16();
            Unknown0x52 = reader.ReadUInt16();
            Unknown0x54 = reader.ReadUInt32();
            ImageDataSize = reader.ReadUInt32();
            Unknown0x5C = reader.ReadUInt32();
            Unknown0x60 = reader.ReadUInt32();
            CompressionFourCC = Encoding.ASCII.GetString(reader.ReadBytes(4)).TrimEnd('\0');
            Unknown0x68 = reader.ReadUInt32();
            Unknown0x6C = reader.ReadUInt32();
            Unknown0x70 = reader.ReadUInt32();
            Unknown0x74 = reader.ReadUInt32();
            Unknown0x78 = reader.ReadByte();
            Unknown0x79 = reader.ReadByte();
            Unknown0x7A = reader.ReadUInt16();
            Unknown0x7C = reader.ReadUInt16();
            Unknown0x7E = reader.ReadUInt16();

            reader.BaseStream.Seek(DataOffset, SeekOrigin.Begin);
            PixelData = reader.ReadBytes((int)ImageDataSize);

            TidFormatChannelOrder pixelChannelOrder = ((TidFormatChannelOrder)PixelFormat & TidFormatChannelOrder.Argb);
            TidFormatCompressionFlag pixelCompression = ((TidFormatCompressionFlag)PixelFormat & TidFormatCompressionFlag.Compressed);

            bool pixelUnknownBit0 = (((TidFormatUnknownBit0)PixelFormat & TidFormatUnknownBit0.Set) == TidFormatUnknownBit0.Set);
            bool pixelUnknownBit3 = (((TidFormatUnknownBit3)PixelFormat & TidFormatUnknownBit3.Set) == TidFormatUnknownBit3.Set);
            bool pixelUnknownBit5 = (((TidFormatUnknownBit5)PixelFormat & TidFormatUnknownBit5.Set) == TidFormatUnknownBit5.Set);
            bool pixelUnknownBit6 = (((TidFormatUnknownBit6)PixelFormat & TidFormatUnknownBit6.Set) == TidFormatUnknownBit6.Set);
            bool pixelUnknownBit7 = (((TidFormatUnknownBit7)PixelFormat & TidFormatUnknownBit7.Set) == TidFormatUnknownBit7.Set);

            PixelDataFormat pixelFormat;

            if (pixelCompression == TidFormatCompressionFlag.Compressed)
            {
                switch (CompressionFourCC)
                {
                    case "DXT1": pixelFormat = PixelDataFormat.FormatDXT1Rgba; break;
                    case "DXT3": pixelFormat = PixelDataFormat.FormatDXT3; break;
                    case "DXT5": pixelFormat = PixelDataFormat.FormatDXT5; break;
                    default: throw new Exception(string.Format("Unimplemented TID compression format '{0}'", CompressionFourCC));
                }
            }
            else if (pixelCompression == TidFormatCompressionFlag.NotCompressed)
            {
                if (pixelChannelOrder == TidFormatChannelOrder.Rgba)
                    pixelFormat = PixelDataFormat.FormatRgba8888;
                else if (pixelChannelOrder == TidFormatChannelOrder.Argb)
                    pixelFormat = PixelDataFormat.FormatArgb8888;
                else
                    throw new Exception("Invalid channel order; should not be reached?!");
            }
            else
                throw new Exception("Invalid compression flag; should not be reached?!");

            // TODO: verify if [Compressed == Swizzled] is correct, or if swizzling depends on other factors

            if (pixelCompression == TidFormatCompressionFlag.Compressed)
                pixelFormat |= PixelDataFormat.PixelOrderingSwizzledVita;

            imageBinary = new ImageBinary();
            imageBinary.Width = (int)Width;
            imageBinary.Height = (int)Height;
            imageBinary.InputPixelFormat = pixelFormat;
            imageBinary.InputEndianness = Endian.BigEndian;

            imageBinary.AddInputPixels(PixelData);
        }

        public override int GetImageCount()
        {
            return 1;
        }

        public override int GetPaletteCount()
        {
            return 1;
        }

        protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
        {
            Bitmap image = imageBinary.GetBitmap(imageIndex, paletteIndex);

            if (((TidFormatVerticalFlip)PixelFormat & TidFormatVerticalFlip.Flipped) == TidFormatVerticalFlip.Flipped)
            {
                image.RotateFlip(RotateFlipType.RotateNoneFlipY);
            }

            return image;
        }
    }
}
