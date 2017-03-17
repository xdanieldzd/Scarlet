using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

using Scarlet.Drawing;
using Scarlet.IO;

using Scarlet.Platform.Nintendo;

namespace Scarlet.IO.ImageFormats
{
    /* TODO!
     * Whatever the additional pixel data in ex. lightmap_5_10_a_grawpstory001 is -- it's not mipmaps, but also *probably* not garbage as DataSize & co. include it...?
     * Figure out relationship between Unknown0x10, DataSize and DataSizeAlt */

    internal class BTGAMipmapInfo
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public byte[] PixelData { get; private set; }

        public BTGAMipmapInfo(int width, int height, byte[] data)
        {
            Width = width;
            Height = height;
            PixelData = data;
        }
    }

    internal interface IBTGAHeader
    {
        ushort Width { get; }
        ushort Height { get; }
        SdkPixelFormat PixelFormat { get; }
        uint NumMipmaps { get; }
    }

    internal class BTGAHeader0x1 : IBTGAHeader
    {
        public uint Unknown0x00 { get; private set; }   // Always 0x00000001? Can be used as magic number?
        public uint Unknown0x04 { get; private set; }   // Always 0x00000020? ""
        public uint Unknown0x08 { get; private set; }   // Always 0x00000020? ""
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }
        public uint Unknown0x10 { get; private set; }   // Sometimes has data, sometimes doesn't; sometimes same as DataSize
        public SdkPixelFormat PixelFormat { get; private set; }
        public ushort Unknown0x16 { get; private set; }
        public uint NumMipmaps { get; private set; }
        public uint Unknown0x1C { get; private set; }
        public uint Unknown0x20 { get; private set; }
        public uint Unknown0x24 { get; private set; }
        public uint Unknown0x28 { get; private set; }
        public uint Unknown0x2C { get; private set; }
        public uint DataSize { get; private set; }
        public uint DataSizeAlt { get; private set; }

        public BTGAHeader0x1(EndianBinaryReader reader)
        {
            Unknown0x00 = reader.ReadUInt32();
            Unknown0x04 = reader.ReadUInt32();
            Unknown0x08 = reader.ReadUInt32();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            Unknown0x10 = reader.ReadUInt32();
            PixelFormat = (SdkPixelFormat)reader.ReadUInt16();
            Unknown0x16 = reader.ReadUInt16();
            NumMipmaps = reader.ReadUInt32();
            Unknown0x1C = reader.ReadUInt32();
            Unknown0x20 = reader.ReadUInt32();
            Unknown0x24 = reader.ReadUInt32();
            Unknown0x28 = reader.ReadUInt32();
            Unknown0x2C = reader.ReadUInt32();
            DataSize = reader.ReadUInt32();
            DataSizeAlt = reader.ReadUInt32();
        }
    }

    internal class BTGAHeader0x400 : IBTGAHeader
    {
        public uint Unknown0x00 { get; private set; }
        public uint Unknown0x04 { get; private set; }
        public uint Unknown0x08 { get; private set; }
        public uint Unknown0x0C { get; private set; }
        public uint Unknown0x10 { get; private set; }
        public uint DataSize { get; private set; }
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }
        public uint DataSizeAlt { get; private set; }
        public SdkPixelFormat PixelFormat { get; private set; }
        public ushort Unknown0x22 { get; private set; }
        public uint NumMipmaps { get; private set; }
        public uint Unknown0x28 { get; private set; }
        public uint Unknown0x2C { get; private set; }

        public BTGAHeader0x400(EndianBinaryReader reader)
        {
            Unknown0x00 = reader.ReadUInt32();
            Unknown0x04 = reader.ReadUInt32();
            Unknown0x08 = reader.ReadUInt32();
            Unknown0x0C = reader.ReadUInt32();
            Unknown0x10 = reader.ReadUInt32();
            DataSize = reader.ReadUInt32();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            DataSizeAlt = reader.ReadUInt32();
            PixelFormat = (SdkPixelFormat)reader.ReadUInt16();
            Unknown0x22 = reader.ReadUInt16();
            NumMipmaps = reader.ReadUInt32();
            Unknown0x28 = reader.ReadUInt32();
            Unknown0x2C = reader.ReadUInt32();
        }
    }

    [FilenamePattern("^.*\\.(btga|lga)$")]
    public class BTGA : ImageFormat
    {
        IBTGAHeader header;

        List<BTGAMipmapInfo> mipmapData;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            uint ident = reader.ReadUInt32();
            reader.BaseStream.Seek(-4, SeekOrigin.Current);
            switch (ident)
            {
                case 0x00000001: header = new BTGAHeader0x1(reader); break;
                case 0x00000400: header = new BTGAHeader0x400(reader); break;
                default: throw new Exception(string.Format("Unrecognized header format 0x{0:X4}", ident));
            }

            int bitsPerPixel;
            switch (header.PixelFormat)
            {
                case SdkPixelFormat.RGBA8:
                    bitsPerPixel = 32;
                    break;
                case SdkPixelFormat.RGB8:
                    bitsPerPixel = 24;
                    break;
                case SdkPixelFormat.RGBA5551:
                case SdkPixelFormat.RGB565:
                case SdkPixelFormat.RGBA4:
                case SdkPixelFormat.LA8:
                case SdkPixelFormat.HILO8:
                    bitsPerPixel = 16;
                    break;
                case SdkPixelFormat.L8:
                case SdkPixelFormat.A8:
                case SdkPixelFormat.LA4:
                case SdkPixelFormat.ETC1A4:
                    bitsPerPixel = 8;
                    break;
                case SdkPixelFormat.L4:
                case SdkPixelFormat.A4:
                case SdkPixelFormat.ETC1:
                    bitsPerPixel = 4;
                    break;

                default:
                    throw new Exception(string.Format("Unrecognized pixel format 0x{0:X4}", (uint)header.PixelFormat));
            }

            // We should already be here, but just to be safe?
            reader.BaseStream.Seek(0x38, SeekOrigin.Begin);

            mipmapData = new List<BTGAMipmapInfo>();
            for (int i = 0; i < header.NumMipmaps; i++)
            {
                // Calculate dimensions, datasize for this mipmap level
                int mipWidth = (header.Width >> i);
                int mipHeight = (header.Height >> i);
                int mipDataSize = (((bitsPerPixel * (header.Width >> i)) * (header.Height >> i)) / 8);
                byte[] mipPixelData = reader.ReadBytes(mipDataSize);

                // Store infos in list for later
                mipmapData.Add(new BTGAMipmapInfo(mipWidth, mipHeight, mipPixelData));
            }
        }

        public override int GetImageCount()
        {
            return (int)header.NumMipmaps;
        }

        public override int GetPaletteCount()
        {
            return 0;
        }

        protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
        {
            ImageBinary imageBinary = new ImageBinary();

            imageBinary = new ImageBinary();
            imageBinary.Width = mipmapData[imageIndex].Width;
            imageBinary.Height = mipmapData[imageIndex].Height;
            imageBinary.InputPixelFormat = N3DS.GetPixelDataFormat(header.PixelFormat);
            imageBinary.InputEndianness = Endian.LittleEndian;
            imageBinary.AddInputPixels(mipmapData[imageIndex].PixelData);

            // Don't pass in original imageIndex and paletteIndex; Scarlet can't handle multiple widths/heights per ImageBinary, so we have to recreate it every time

            // TODO: uh, verify the whole Y flipping business here...

            //return imageBinary.GetBitmap(0, 0);
            Bitmap bitmap = imageBinary.GetBitmap(0, 0);
            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            return bitmap;
        }
    }
}
