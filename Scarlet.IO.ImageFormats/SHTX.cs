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
    [MagicNumber("SHTX", 0x00)]
    public class SHTX : ImageFormat
    {
        public string Tag { get; private set; }
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }
        public ushort Unknown0x08 { get; private set; }
        public ushort Unknown0x0A { get; private set; }
        public ushort Unknown0x0C { get; private set; }
        public ushort Unknown0x0E { get; private set; }

        public byte[] PaletteData { get; private set; }
        public byte[] PixelData { get; private set; }

        ImageBinary imageBinary;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            Tag = Encoding.ASCII.GetString(reader.ReadBytes(4));
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            Unknown0x08 = reader.ReadUInt16();
            Unknown0x0A = reader.ReadUInt16();
            Unknown0x0C = reader.ReadUInt16();
            Unknown0x0E = reader.ReadUInt16();

            PaletteData = reader.ReadBytes(16 * 4);
            PixelData = reader.ReadBytes(Width * Height);

            imageBinary = new ImageBinary();
            imageBinary.Width = Width;
            imageBinary.Height = Height;
            imageBinary.InputPaletteFormat = PixelDataFormat.FormatAbgr8888;
            imageBinary.InputPixelFormat = PixelDataFormat.FormatIndexed4;
            imageBinary.InputEndianness = Endian.LittleEndian;

            imageBinary.AddInputPalette(PaletteData);
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
            return imageBinary.GetBitmap(imageIndex, paletteIndex);
        }
    }
}
