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
    [MagicNumber("SHTXFS", 0x00)]
    public class SHTXFS : ImageFormat
    {
        public string Tag { get; private set; }
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }
        public byte Unknown0x0A { get; private set; }
        public byte Unknown0x0B { get; private set; }

        public byte[] PaletteData { get; private set; }
        public byte[] PixelData { get; private set; }

        ImageBinary imageBinary;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            Tag = Encoding.ASCII.GetString(reader.ReadBytes(6));
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            Unknown0x0A = reader.ReadByte();
            Unknown0x0B = reader.ReadByte();

            PaletteData = reader.ReadBytes(256 * 4);
            PixelData = reader.ReadBytes(Width * Height);

            /* Initialize ImageBinary */
            imageBinary = new ImageBinary();
            imageBinary.Width = Width;
            imageBinary.Height = Height;
            imageBinary.InputPaletteFormat = PixelDataFormat.FormatAbgr8888;
            imageBinary.InputPixelFormat = PixelDataFormat.FormatIndexed8;
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
