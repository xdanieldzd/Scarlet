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
    // TODO: more formats?
    public enum KsltPixelFormat : byte
    {
        Argb8888 = 0x00000000,
        DXT5 = 0x00000006,
    }

    public class KsltImageData
    {
        public KsltPixelFormat PixelFormat { get; private set; }
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }
        public uint Unknown0x08 { get; private set; }               // always zero?
        public uint Unknown0x0C { get; private set; }               // always 0x00000001?
        public uint Unknown0x10 { get; private set; }               // always 0x00000001?
        public uint Unknown0x14 { get; private set; }               // always 0x00000001?
        public uint Unknown0x18 { get; private set; }               // always 0x00000001?
        public uint PixelDataSize { get; private set; }
        public uint Unknown0x20 { get; private set; }               // 0x00000800, 0x00001000, ...?
        public byte[] Unknown0x24 { get; private set; }             // 0x24 bytes, zero?

        public byte[] PixelData { get; private set; }

        public KsltImageData(EndianBinaryReader reader)
        {
            PixelFormat = (KsltPixelFormat)reader.ReadUInt32();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            Unknown0x08 = reader.ReadUInt32();
            Unknown0x0C = reader.ReadUInt32();
            Unknown0x10 = reader.ReadUInt32();
            Unknown0x14 = reader.ReadUInt32();
            Unknown0x18 = reader.ReadUInt32();
            PixelDataSize = reader.ReadUInt32();
            Unknown0x20 = reader.ReadUInt32();
            Unknown0x24 = reader.ReadBytes(0x24);

            PixelData = reader.ReadBytes((int)PixelDataSize);
        }
    }

    [MagicNumber("TLSK1100", 0x00)]
    public class KSLT : ImageFormat
    {
        public string MagicNumber { get; private set; }
        public uint NumImages { get; private set; }
        public uint FileSize { get; private set; }
        public uint ImageNamesOffset { get; private set; }          // relative to 0x40
        public uint ImageNamesSize { get; private set; }
        public byte[] Unknown0x18 { get; private set; }             // 0x28 bytes, zero?

        public uint[] ImageNameLengths { get; private set; }
        public uint[] ImageDataOffsets { get; private set; }        // absolute

        public string[] ImageNames { get; private set; }
        public KsltImageData[] ImageData { get; private set; }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(8));
            NumImages = reader.ReadUInt32();
            FileSize = reader.ReadUInt32();
            ImageNamesOffset = reader.ReadUInt32();
            ImageNamesSize = reader.ReadUInt32();
            Unknown0x18 = reader.ReadBytes(0x28);

            ImageNameLengths = new uint[NumImages];
            ImageDataOffsets = new uint[NumImages];

            for (int i = 0; i < NumImages; i++)
            {
                ImageNameLengths[i] = reader.ReadUInt32();
                ImageDataOffsets[i] = reader.ReadUInt32();
            }

            ImageNames = new string[NumImages];
            reader.BaseStream.Seek(0x40 + ImageNamesOffset, SeekOrigin.Begin);
            for (int i = 0; i < NumImages; i++)
                ImageNames[i] = Encoding.ASCII.GetString(reader.ReadBytes((int)ImageNameLengths[i] + 1)).TrimEnd('\0');

            ImageData = new KsltImageData[NumImages];
            for (int i = 0; i < NumImages; i++)
            {
                reader.BaseStream.Seek(ImageDataOffsets[i], SeekOrigin.Begin);
                ImageData[i] = new KsltImageData(reader);
            }
        }

        public override int GetImageCount()
        {
            return (int)NumImages;
        }

        public override int GetPaletteCount()
        {
            return 0;
        }

        public override string GetImageName(int imageIndex)
        {
            return ImageNames[imageIndex];
        }

        protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
        {
            KsltImageData imageData = ImageData[imageIndex];

            ImageBinary imageBinary = new ImageBinary();

            imageBinary.Width = imageData.Width;
            imageBinary.Height = imageData.Height;
            imageBinary.InputEndianness = Endian.LittleEndian;

            switch (imageData.PixelFormat)
            {
                case KsltPixelFormat.Argb8888:
                    imageBinary.InputPixelFormat = PixelDataFormat.FormatArgb8888;
                    break;

                case KsltPixelFormat.DXT5:
                    imageBinary.InputPixelFormat = PixelDataFormat.FormatDXT5;
                    imageBinary.InputPixelFormat |= PixelDataFormat.PixelOrderingSwizzledVita;
                    break;

                default: throw new NotImplementedException(string.Format("KSLT format 0x{0:X8}", (uint)imageData.PixelFormat));
            }

            imageBinary.AddInputPixels(imageData.PixelData);

            return imageBinary.GetBitmap();
        }
    }
}
