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
    [FilenamePattern("^.*\\.txp$")]
    public class TXP : ImageFormat
    {
        // TODO: verify me! beautify me!

        public ushort ImageWidth { get; private set; }
        public ushort ImageHeight { get; private set; }
        public ushort ColorCount { get; private set; }
        public ushort Unknown0x06 { get; private set; }
        public ushort PaletteWidth { get; private set; }
        public ushort PaletteHeight { get; private set; }
        public ushort SwizzleFlag { get; private set; }
        public ushort Unknown0x0E { get; private set; }

        public byte[][] PaletteData { get; private set; }
        public byte[] PixelData { get; private set; }

        ImageBinary imageBinary;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            ImageWidth = reader.ReadUInt16();
            ImageHeight = reader.ReadUInt16();
            ColorCount = reader.ReadUInt16();
            Unknown0x06 = reader.ReadUInt16();
            PaletteWidth = reader.ReadUInt16();
            PaletteHeight = reader.ReadUInt16();
            SwizzleFlag = reader.ReadUInt16();
            Unknown0x0E = reader.ReadUInt16();

            PaletteData = new byte[PaletteHeight][];
            for (int py = 0; py < PaletteHeight; py++)
            {
                PaletteData[py] = new byte[ColorCount * 4];
                for (int px = 0; px < PaletteWidth; px++)
                {
                    Buffer.BlockCopy(reader.ReadBytes(4), 0, PaletteData[py], px * 4, 4);
                }
            }

            PixelData = reader.ReadBytes((ImageWidth * ImageHeight) / (ColorCount == 256 ? 1 : 2));

            imageBinary = new ImageBinary();
            imageBinary.Width = ImageWidth;
            imageBinary.Height = ImageHeight;
            imageBinary.InputPaletteFormat = PixelDataFormat.FormatAbgr8888;
            imageBinary.InputPixelFormat = (ColorCount == 256 ? PixelDataFormat.FormatIndexed8 : PixelDataFormat.FormatIndexed4);
            if (SwizzleFlag != 0) imageBinary.InputPixelFormat |= PixelDataFormat.PostProcessUnswizzle_PSP;
            imageBinary.InputEndianness = Endian.LittleEndian;

            foreach (byte[] palette in PaletteData) imageBinary.AddInputPalette(palette);
            imageBinary.AddInputPixels(PixelData);
        }

        public override int GetImageCount()
        {
            return 1;
        }

        public override int GetPaletteCount()
        {
            return PaletteData.Length;
        }

        protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
        {
            return imageBinary.GetBitmap(imageIndex, paletteIndex);
        }
    }
}
