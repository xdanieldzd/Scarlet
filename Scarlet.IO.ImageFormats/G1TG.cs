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
    public interface IG1TGImageHeader
    {
        byte GetPixelFormat();
        byte GetPackedDimensions();
    }

    // TODO: *mostly* used in 0050 G1TGs, but also in some 0060...?
    public class G1TG0050ImageHeader : IG1TGImageHeader
    {
        public byte Unknown0x00 { get; private set; }   /* always 0x01? */
        public byte PixelFormat { get; private set; }
        public byte PackedDimensions { get; private set; }
        public byte Unknown0x03 { get; private set; }   /* always 0x00? */
        public uint Unknown0x04 { get; private set; }   /* always 0x00011200? */

        public G1TG0050ImageHeader(EndianBinaryReader reader)
        {
            Unknown0x00 = reader.ReadByte();
            PixelFormat = reader.ReadByte();
            PackedDimensions = reader.ReadByte();
            Unknown0x03 = reader.ReadByte();
            Unknown0x04 = reader.ReadUInt32();
        }

        public byte GetPixelFormat() { return PixelFormat; }
        public byte GetPackedDimensions() { return PackedDimensions; }
    }

    public class G1TG0060ImageHeader : IG1TGImageHeader
    {
        public byte Unknown0x00 { get; private set; }   /* always 0x01? */
        public byte PixelFormat { get; private set; }
        public byte PackedDimensions { get; private set; }
        public byte Unknown0x03 { get; private set; }   /* always 0x00? */
        public uint Unknown0x04 { get; private set; }   /* always 0x00011201? */
        public uint Unknown0x08 { get; private set; }   //0x0000000C ?
        public uint Unknown0x0C { get; private set; }   //zero?
        public uint Unknown0x10 { get; private set; }   //zero?

        public G1TG0060ImageHeader(EndianBinaryReader reader)
        {
            Unknown0x00 = reader.ReadByte();
            PixelFormat = reader.ReadByte();
            PackedDimensions = reader.ReadByte();
            Unknown0x03 = reader.ReadByte();
            Unknown0x04 = reader.ReadUInt32();
            Unknown0x08 = reader.ReadUInt32();
            Unknown0x0C = reader.ReadUInt32();
            Unknown0x10 = reader.ReadUInt32();
        }

        public byte GetPixelFormat() { return PixelFormat; }
        public byte GetPackedDimensions() { return PackedDimensions; }
    }

    internal class G1TGImageDataShim
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public PixelDataFormat Format { get; private set; }
        public byte[] Data { get; private set; }

        public G1TGImageDataShim(IG1TGImageHeader header, EndianBinaryReader reader)
        {
            byte pixelFormat = header.GetPixelFormat();
            switch (pixelFormat)
            {
                case 0x01: Format = PixelDataFormat.FormatRgba8888; break;
                case 0x06: Format = PixelDataFormat.FormatDXT1Rgb; break;
                case 0x08: Format = PixelDataFormat.FormatDXT5; break;
                default: throw new Exception($"Unhandled G1TG image format 0x{pixelFormat:X}");
            }

            // TODO: verify this is correct...?
            byte packedDimensions = header.GetPackedDimensions();
            Width = (int)Math.Pow(2, ((packedDimensions >> 4) & 0x0F));
            Height = (int)Math.Pow(2, (packedDimensions & 0x0F));

            // TODO: also this? there's no datasize thingy for this...
            Data = reader.ReadBytes((int)((Width * Height) * (Constants.RealBitsPerPixel[Format & PixelDataFormat.MaskBpp] / 8.0)));
        }
    }

    [MagicNumber("G1TG", 0x00)]
    public class G1TG : ImageFormat
    {
        public string MagicNumber { get; private set; }                 /* 'G1TG' */
        public string Version { get; private set; }                     /* '0060', '0050' */
        public uint FileSize { get; private set; }
        public uint Unknown0x0C { get; private set; }                   //0x20,24,28, depending on numimages?
        public uint NumImages { get; private set; }
        public uint Unknown0x14 { get; private set; }                   /* always 0x00000001? */
        public uint Unknown0x18 { get; private set; }
        public ulong[] UnknownImageValues { get; private set; }

        G1TGImageDataShim[] imageDataShims;

        ImageBinary imageBinary;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            reader.Endianness = Endian.BigEndian;

            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            Version = Encoding.ASCII.GetString(reader.ReadBytes(4));
            FileSize = reader.ReadUInt32();
            Unknown0x0C = reader.ReadUInt32();
            NumImages = reader.ReadUInt32();
            Unknown0x14 = reader.ReadUInt32();
            Unknown0x18 = reader.ReadUInt32();

            UnknownImageValues = new ulong[NumImages];
            for (int i = 0; i < UnknownImageValues.Length; i++) UnknownImageValues[i] = reader.ReadUInt64();

            imageDataShims = new G1TGImageDataShim[NumImages];
            for (int i = 0; i < NumImages; i++)
            {
                reader.BaseStream.Seek(4, SeekOrigin.Current);
                uint headerConst = reader.ReadUInt32();
                reader.BaseStream.Seek(-8, SeekOrigin.Current);

                IG1TGImageHeader imageHeader;
                switch (headerConst)
                {
                    case 0x00011200: imageHeader = new G1TG0050ImageHeader(reader); break;
                    case 0x00011201: imageHeader = new G1TG0060ImageHeader(reader); break;
                    default: throw new Exception($"Unimplemented G1TG version {Version}");
                }
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
