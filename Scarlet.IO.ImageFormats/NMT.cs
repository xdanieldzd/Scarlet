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
    public enum NmtPixelFormat : byte
    {
        Argb8888 = 0x02,
        Indexed4bpp = 0x0A,
        Indexed8bpp = 0x0E,
    }

    [MagicNumber("nismultitexform\0", 0x00)]
    [FilenamePattern("^.*\\.nmt$")]
    public class NMT : ImageFormat
    {
        public string MagicNumber { get; private set; }

        public uint Unknown0x10 { get; private set; }//0?
        public uint Unknown0x14 { get; private set; }//almost filesize? 0x40 lower?
        public uint Unknown0x18 { get; private set; }//same?
        public uint Unknown0x1C { get; private set; }//0?

        public NmtPixelFormat PixelFormat { get; private set; }
        public byte Unknown0x21 { get; private set; }
        public byte Unknown0x22 { get; private set; }
        public byte Unknown0x23 { get; private set; }
        public byte Unknown0x24 { get; private set; }
        public byte NumPalettes { get; private set; }

        public ushort Width { get; private set; }
        public ushort Height { get; private set; }
        public ushort Unknown0x2A { get; private set; }//1?
        public ushort Unknown0x2C { get; private set; }//8bpp=same as width? 4bpp=half of width?
        public ushort Unknown0x2E { get; private set; }//0?

        public byte[][] PaletteData { get; private set; }
        public byte[] PixelData { get; private set; }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(16));

            Unknown0x10 = reader.ReadUInt32();
            Unknown0x14 = reader.ReadUInt32();
            Unknown0x18 = reader.ReadUInt32();
            Unknown0x1C = reader.ReadUInt32();

            PixelFormat = (NmtPixelFormat)reader.ReadByte();
            Unknown0x21 = reader.ReadByte();
            Unknown0x22 = reader.ReadByte();
            Unknown0x23 = reader.ReadByte();

            Unknown0x24 = reader.ReadByte();
            NumPalettes = reader.ReadByte();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            Unknown0x2A = reader.ReadUInt16();
            Unknown0x2C = reader.ReadUInt16();
            Unknown0x2E = reader.ReadUInt16();

            if (!Enum.IsDefined(typeof(NmtPixelFormat), PixelFormat)) throw new Exception("Unknown pixel format");

            PaletteData = new byte[NumPalettes][];
            for (int i = 0; i < PaletteData.Length; i++)
                PaletteData[i] = reader.ReadBytes(PixelFormat == NmtPixelFormat.Indexed8bpp ? 256 * 4 : 16 * 4);

            int bitsPerPixel = 0;

            switch (PixelFormat)
            {
                case NmtPixelFormat.Indexed4bpp: bitsPerPixel = 4; break;
                case NmtPixelFormat.Indexed8bpp: bitsPerPixel = 8; break;
                case NmtPixelFormat.Argb8888: bitsPerPixel = 32; break;
            }

            PixelData = reader.ReadBytes((Width * Height / (bitsPerPixel < 8 ? 2 : 1)) * (bitsPerPixel < 8 ? 1 : bitsPerPixel / 8));
        }

        public override int GetImageCount()
        {
            return 1;
        }

        public override int GetPaletteCount()
        {
            return (int)NumPalettes;
        }

        protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
        {
            ImageBinary imageBinary = new ImageBinary();

            PixelDataFormat pixelDataFormat = PixelDataFormat.Undefined;
            switch (PixelFormat)
            {
                case NmtPixelFormat.Indexed4bpp: pixelDataFormat = PixelDataFormat.FormatIndexed4; break;
                case NmtPixelFormat.Indexed8bpp: pixelDataFormat = PixelDataFormat.FormatIndexed8; break;
                case NmtPixelFormat.Argb8888: pixelDataFormat = PixelDataFormat.FormatArgb8888; break;
            }

            imageBinary.Width = Width;
            imageBinary.Height = Height;
            imageBinary.InputPaletteFormat = PixelDataFormat.FormatArgb8888;
            imageBinary.InputPixelFormat = pixelDataFormat;
            imageBinary.InputEndianness = Endian.LittleEndian;

            foreach (byte[] palette in PaletteData) imageBinary.AddInputPalette(palette);
            imageBinary.AddInputPixels(PixelData);

            return imageBinary.GetBitmap();
        }
    }
}
