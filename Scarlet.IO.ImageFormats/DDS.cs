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
    // TODO: finish me!

    [MagicNumber("DDS ", 0x00)]
    public class DDS : ImageFormat
    {
        internal string MagicNumber { get; private set; }
        internal DDSHeader DDSHeader { get; private set; }

        public byte[] PixelData { get; private set; }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            DDSHeader = new DDSHeader(reader);

            PixelData = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
        }

        public override int GetImageCount()
        {
            return 1;
        }

        public override int GetPaletteCount()
        {
            return 0;
        }

        protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
        {
            PixelDataFormat inputPixelFormat = PixelDataFormat.Undefined;

            int physicalWidth, physicalHeight;

            if (DDSHeader.PixelFormat.Flags.HasFlag(DDPF.FourCC))
            {
                switch (DDSHeader.PixelFormat.FourCC)
                {
                    case "DXT1": inputPixelFormat = PixelDataFormat.FormatDXT1Rgba; break;
                    case "DXT3": inputPixelFormat = PixelDataFormat.FormatDXT3; break;
                    case "DXT5": inputPixelFormat = PixelDataFormat.FormatDXT5; break;
                    case "PVR3": inputPixelFormat = PixelDataFormat.FormatPVRT4_Vita; break;    // TODO: verify, probably not quite like PVRT4?
                    case "PVR4": inputPixelFormat = PixelDataFormat.FormatPVRT4_Vita; break;
                }

                physicalWidth = physicalHeight = 1;
                while (physicalWidth < DDSHeader.Width) physicalWidth *= 2;
                while (physicalHeight < DDSHeader.Height) physicalHeight *= 2;
            }
            else
            {
                // TODO: actually implement uncompressed DDS formats...?

                PixelDataFormat alphaFormat = PixelDataFormat.Undefined, colorFormat = PixelDataFormat.Undefined;

                if (DDSHeader.PixelFormat.Flags.HasFlag(DDPF.AlphaPixels))
                {
                    switch (DDSHeader.PixelFormat.ABitMask)
                    {
                        case 0xFF000000: alphaFormat |= PixelDataFormat.AlphaBits8; break;
                        default: throw new NotImplementedException("DDS alpha bit mask not implemented");
                    }
                }

                if (DDSHeader.PixelFormat.Flags.HasFlag(DDPF.RGB))
                {
                    switch (DDSHeader.PixelFormat.RBitMask)
                    {
                        case 0x00FF0000: colorFormat |= PixelDataFormat.RedBits8; break;
                        default: throw new NotImplementedException("DDS red bit mask not implemented");
                    }

                    switch (DDSHeader.PixelFormat.GBitMask)
                    {
                        case 0x0000FF00: colorFormat |= PixelDataFormat.GreenBits8; break;
                        default: throw new NotImplementedException("DDS green bit mask not implemented");
                    }

                    switch (DDSHeader.PixelFormat.BBitMask)
                    {
                        case 0x000000FF: colorFormat |= PixelDataFormat.BlueBits8; break;
                        default: throw new NotImplementedException("DDS blue bit mask not implemented");
                    }
                }

                if (DDSHeader.PixelFormat.RBitMask != 0 && DDSHeader.PixelFormat.GBitMask != 0 && DDSHeader.PixelFormat.BBitMask != 0 && DDSHeader.PixelFormat.ABitMask != 0)
                {
                    inputPixelFormat |= PixelDataFormat.ChannelsArgb;
                }
                else
                    throw new NotImplementedException("DDS channel setup not implemented");

                if (DDSHeader.PixelFormat.RGBBitCount == 32)
                {
                    inputPixelFormat |= PixelDataFormat.Bpp32;
                }
                else
                    throw new NotImplementedException("DDS bits per pixel not implemented");

                inputPixelFormat |= colorFormat;
                inputPixelFormat |= alphaFormat;

                physicalWidth = (int)DDSHeader.Width;
                physicalHeight = (int)DDSHeader.Height;
            }

            if (inputPixelFormat == PixelDataFormat.Undefined)
                throw new NotImplementedException("DDS pixel format not implemented");

            if (DDSHeader.Reserved1[0x0A] == 0x00020008)
            {
                // TODO: total guesswork, verify me!
                inputPixelFormat |= PixelDataFormat.PixelOrderingSwizzledVita;
            }

            ImageBinary imageBinary = new ImageBinary();

            imageBinary.Width = (int)DDSHeader.Width;
            imageBinary.Height = (int)DDSHeader.Height;
            imageBinary.PhysicalWidth = physicalWidth;
            imageBinary.PhysicalHeight = physicalHeight;
            imageBinary.InputPixelFormat = inputPixelFormat;
            imageBinary.InputEndianness = Endian.LittleEndian;
            imageBinary.AddInputPixels(PixelData);

            return imageBinary.GetBitmap();
        }
    }

    [Flags]
    internal enum DDSD
    {
        Caps = 0x1,
        Height = 0x2,
        Width = 0x4,
        Pitch = 0x8,
        PixelFormat = 0x1000,
        MipMapCount = 0x20000,
        LinearSize = 0x80000,
        Depth = 0x800000
    }

    [Flags]
    internal enum DDSCaps
    {
        Complex = 0x8,
        MipMap = 0x400000,
        Texture = 0x1000
    }

    [Flags]
    internal enum DDSCaps2
    {
        CubeMap = 0x200,
        CubeMap_PositiveX = 0x400,
        CubeMap_NegativeX = 0x800,
        CubeMap_PositiveY = 0x1000,
        CubeMap_NegativeY = 0x2000,
        CubeMap_PositiveZ = 0x4000,
        CubeMap_NegativeZ = 0x8000,
        Volume = 0x200000
    }

    internal class DDSHeader
    {
        public uint Size { get; set; }
        public DDSD Flags { get; set; }
        public uint Height { get; set; }
        public uint Width { get; set; }
        public uint PitchOrLinearSize { get; set; }
        public uint Depth { get; set; }
        public uint MipMapCount { get; set; }
        public uint[] Reserved1 { get; set; }
        public DDSPixelFormat PixelFormat { get; set; }
        public DDSCaps Caps { get; set; }
        public DDSCaps2 Caps2 { get; set; }
        public uint Caps3 { get; set; }
        public uint Caps4 { get; set; }
        public uint Reserved2 { get; set; }

        public DDSHeader(EndianBinaryReader reader)
        {
            Size = reader.ReadUInt32(Endian.LittleEndian);
            Flags = (DDSD)reader.ReadUInt32(Endian.LittleEndian);
            Height = reader.ReadUInt32(Endian.LittleEndian);
            Width = reader.ReadUInt32(Endian.LittleEndian);
            PitchOrLinearSize = reader.ReadUInt32(Endian.LittleEndian);
            Depth = reader.ReadUInt32(Endian.LittleEndian);
            MipMapCount = reader.ReadUInt32(Endian.LittleEndian);
            Reserved1 = new uint[11];
            for (int i = 0; i < Reserved1.Length; i++) Reserved1[i] = reader.ReadUInt32(Endian.LittleEndian);
            PixelFormat = new DDSPixelFormat(reader);
            Caps = (DDSCaps)reader.ReadUInt32(Endian.LittleEndian);
            Caps2 = (DDSCaps2)reader.ReadUInt32(Endian.LittleEndian);
            Caps3 = reader.ReadUInt32(Endian.LittleEndian);
            Caps4 = reader.ReadUInt32(Endian.LittleEndian);
            Reserved2 = reader.ReadUInt32(Endian.LittleEndian);
        }
    }

    [Flags]
    internal enum DDPF
    {
        AlphaPixels = 0x1,
        Alpha = 0x2,
        FourCC = 0x4,
        RGB = 0x40,
        YUV = 0x200,
        Luminance = 0x20000
    }

    internal class DDSPixelFormat
    {
        public uint Size { get; set; }
        public DDPF Flags { get; set; }
        public string FourCC { get; set; }
        public uint RGBBitCount { get; set; }
        public uint RBitMask { get; set; }
        public uint GBitMask { get; set; }
        public uint BBitMask { get; set; }
        public uint ABitMask { get; set; }

        public DDSPixelFormat(EndianBinaryReader reader)
        {
            Size = reader.ReadUInt32(Endian.LittleEndian);
            Flags = (DDPF)reader.ReadUInt32(Endian.LittleEndian);
            FourCC = Encoding.ASCII.GetString(reader.ReadBytes(4));
            RGBBitCount = reader.ReadUInt32(Endian.LittleEndian);
            RBitMask = reader.ReadUInt32(Endian.LittleEndian);
            GBitMask = reader.ReadUInt32(Endian.LittleEndian);
            BBitMask = reader.ReadUInt32(Endian.LittleEndian);
            ABitMask = reader.ReadUInt32(Endian.LittleEndian);
        }
    }
}
