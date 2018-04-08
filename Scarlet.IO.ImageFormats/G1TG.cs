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
    public class G1TGImageHeader
    {
        public readonly byte HeaderSize;

        /* Common */
        public byte Unknown0x00 { get; private set; }           /* always 0x01? */
        public byte PixelFormat { get; private set; }
        public byte PackedDimensions { get; private set; }
        public byte Unknown0x03 { get; private set; }           /* always 0x00? */

        public byte Unknown0x04 { get; private set; }           /* always 0x00? */
        public byte Unknown0x05 { get; private set; }           /* always 0x01? */
        public byte Unknown0x06 { get; private set; }           /* always 0x12? */
        public byte IsExtendedHeader { get; private set; }

        /* Extended */
        public uint Unknown0x08 { get; private set; }           /* always 0x0000000C? */
        public uint Unknown0x0C { get; private set; }           /* always zero? */
        public uint Unknown0x10 { get; private set; }           /* always zero? */

        public G1TGImageHeader(EndianBinaryReader reader)
        {
            byte headerSize;

            Unknown0x00 = reader.ReadByte();
            PixelFormat = reader.ReadByte();
            PackedDimensions = reader.ReadByte();
            Unknown0x03 = reader.ReadByte();

            Unknown0x04 = reader.ReadByte();
            Unknown0x05 = reader.ReadByte();
            Unknown0x06 = reader.ReadByte();
            IsExtendedHeader = reader.ReadByte();

            if (IsExtendedHeader == 0x00)
            {
                headerSize = 0x08;
            }
            else if (IsExtendedHeader == 0x01)
            {
                headerSize = 0x14;

                Unknown0x08 = reader.ReadUInt32();
                Unknown0x0C = reader.ReadUInt32();
                Unknown0x10 = reader.ReadUInt32();
            }
            else
                throw new Exception($"Unhandled G1TG header format 0x{IsExtendedHeader}");

            HeaderSize = headerSize;
        }
    }

    internal class G1TGImageDataShim
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public PixelDataFormat Format { get; private set; }
        public byte[] Data { get; private set; }

        public G1TGImageDataShim(G1TGImageHeader header, EndianBinaryReader reader)
        {
            /* Determine Scarlet pixel format */
            byte pixelFormat = header.PixelFormat;
            switch (pixelFormat)
            {
                case 0x01: Format = PixelDataFormat.FormatRgba8888; break;
                case 0x06: Format = PixelDataFormat.FormatDXT1Rgba; break;
                case 0x08: Format = PixelDataFormat.FormatDXT5; break;
                default: throw new Exception($"Unhandled G1TG image format 0x{pixelFormat:X}");
            }

            /* Extract packed dimensions */
            byte packedDimensions = header.PackedDimensions;
            Width = (1 << ((packedDimensions >> 4) & 0x0F));
            Height = (1 << (packedDimensions & 0x0F));

            /* Read image data */
            Data = reader.ReadBytes((int)((Width * Height) * (Constants.RealBitsPerPixel[Format & PixelDataFormat.MaskBpp] / 8.0)));
        }
    }

    [MagicNumber("G1TG", 0x00)]
    public class G1TG : ImageFormat
    {
        public string MagicNumber { get; private set; }                 /* 'G1TG' */
        public string Version { get; private set; }                     /* '0060', '0050' */
        public uint FileSize { get; private set; }
        public uint HeaderSize { get; private set; }
        public uint NumImages { get; private set; }
        public uint Unknown0x14 { get; private set; }                   /* always 0x00000001? */
        public uint Unknown0x18 { get; private set; }
        public uint[] UnknownValues0x1C { get; private set; }           /* [uint * NumImage]; always zero? */
        public uint[] ImageOffsets { get; private set; }                /* [uint * NumImage]; relative to end of header */

        G1TGImageDataShim[] imageDataShims;

        ImageBinary imageBinary;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            reader.Endianness = Endian.BigEndian;

            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            Version = Encoding.ASCII.GetString(reader.ReadBytes(4));
            FileSize = reader.ReadUInt32();
            HeaderSize = reader.ReadUInt32();
            NumImages = reader.ReadUInt32();
            Unknown0x14 = reader.ReadUInt32();
            Unknown0x18 = reader.ReadUInt32();

            UnknownValues0x1C = new uint[NumImages];
            for (int i = 0; i < UnknownValues0x1C.Length; i++) UnknownValues0x1C[i] = reader.ReadUInt32();

            ImageOffsets = new uint[NumImages];
            for (int i = 0; i < ImageOffsets.Length; i++) ImageOffsets[i] = reader.ReadUInt32();

            imageDataShims = new G1TGImageDataShim[NumImages];
            for (int i = 0; i < NumImages; i++)
            {
                reader.BaseStream.Seek(HeaderSize + ImageOffsets[i], SeekOrigin.Begin);
                G1TGImageHeader imageHeader = new G1TGImageHeader(reader);

                reader.BaseStream.Seek((HeaderSize + ImageOffsets[i]) + imageHeader.HeaderSize, SeekOrigin.Begin);
                imageDataShims[i] = new G1TGImageDataShim(imageHeader, reader);
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

        protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
        {
            G1TGImageDataShim imageDataShim = imageDataShims[imageIndex];

            imageBinary = new ImageBinary();
            imageBinary.AddInputPixels(imageDataShim.Data);
            imageBinary.InputPixelFormat = imageDataShim.Format;
            imageBinary.Width = imageDataShim.Width;
            imageBinary.Height = imageDataShim.Height;

            return imageBinary.GetBitmap(0, 0);
        }
    }
}
