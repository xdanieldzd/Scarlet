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
    // TODO: verify me!

    [MagicNumber("GBIX", 0x00)]
    public class GBIX : ImageFormat
    {
        public string MagicNumber { get; private set; }
        public uint Unknown0x04 { get; private set; }
        public uint Unknown0x08 { get; private set; }
        public uint Unknown0x0C { get; private set; }

        public string MagicNumberUVRT { get; private set; }
        public uint UVRTDataSize { get; private set; }
        public uint Format { get; private set; } // Maybe bitfield?
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }

        public byte[] PaletteData { get; private set; }
        public byte[] PixelData { get; private set; }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            Unknown0x04 = reader.ReadUInt32();
            Unknown0x08 = reader.ReadUInt32();
            Unknown0x0C = reader.ReadUInt32();

            MagicNumberUVRT = Encoding.ASCII.GetString(reader.ReadBytes(4));
            UVRTDataSize = reader.ReadUInt32();
            Format = reader.ReadUInt32();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();

            if (Format == 0x8602)
                PaletteData = reader.ReadBytes(16 * 2);
            else if (Format == 0x8803)
                PaletteData = reader.ReadBytes(16 * 4);
            else if (Format == 0x8A02)
                PaletteData = reader.ReadBytes(256 * 2);
            else if (Format == 0x8C03)
                PaletteData = reader.ReadBytes(256 * 4);
            else if (Format == 0x800A)
                PaletteData = new byte[0];
            else
                throw new NotImplementedException();

            PixelData = reader.ReadBytes((int)(UVRTDataSize - 8 - PaletteData.Length));
        }

        public override int GetImageCount()
        {
            return 1;
        }

        public override int GetPaletteCount()
        {
            return (PaletteData.Length > 0 ? 1 : 0);
        }

        protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
        {
            ImageBinary imageBinary = new ImageBinary();

            PixelDataFormat pixelDataFormat, paletteDataFormat;

            if (Format == 0x8602)
            {
                pixelDataFormat = PixelDataFormat.FormatIndexed4 | PixelDataFormat.PixelOrderingSwizzledPSP;
                paletteDataFormat = PixelDataFormat.FormatAbgr4444;
            }
            else if (Format == 0x8803)
            {
                pixelDataFormat = PixelDataFormat.FormatIndexed4 | PixelDataFormat.PixelOrderingSwizzledPSP;
                paletteDataFormat = PixelDataFormat.FormatAbgr8888;
            }
            else if (Format == 0x8A02)
            {
                pixelDataFormat = PixelDataFormat.FormatIndexed8 | PixelDataFormat.PixelOrderingSwizzledPSP;
                paletteDataFormat = PixelDataFormat.FormatAbgr4444;
            }
            else if (Format == 0x8C03)
            {
                pixelDataFormat = PixelDataFormat.FormatIndexed8 | PixelDataFormat.PixelOrderingSwizzledPSP;
                paletteDataFormat = PixelDataFormat.FormatAbgr8888;
            }
            else if (Format == 0x800A)
            {
                pixelDataFormat = PixelDataFormat.FormatDXT1_PSP;
                paletteDataFormat = PixelDataFormat.Undefined;
            }
            else
                throw new NotImplementedException();

            imageBinary.Width = Width;
            imageBinary.Height = Height;
            imageBinary.InputPaletteFormat = paletteDataFormat;
            imageBinary.InputPixelFormat = pixelDataFormat;
            imageBinary.InputEndianness = Endian.LittleEndian;

            imageBinary.AddInputPalette(PaletteData);
            imageBinary.AddInputPixels(PixelData);

            return imageBinary.GetBitmap();
        }
    }
}
