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
        public ushort TiledFlag { get; private set; }
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
            TiledFlag = reader.ReadUInt16();
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

            int pixelDataSize = ((ImageWidth * ImageHeight) / (ColorCount == 256 ? 1 : 2));

            if (TiledFlag == 0)
                PixelData = reader.ReadBytes(pixelDataSize);
            else
            {
                // TODO: move to ImageBinary post-processing?
                PixelData = new byte[pixelDataSize];

                int tw = 16, th = 8;
                int iw = (ColorCount == 256 ? ImageWidth : ImageWidth / 2);

                for (int iy = 0; iy < ImageHeight; iy += th)
                {
                    for (int ix = 0; ix < iw; ix += tw)
                    {
                        for (int ty = 0; ty < th; ty++)
                        {
                            if (ColorCount == 256)
                            {
                                for (int tx = 0; tx < tw; tx++)
                                {
                                    byte idx = reader.ReadByte();
                                    PixelData[((iy + ty) * iw) + (ix + tx)] = idx;
                                }
                            }
                            else
                            {
                                for (int tx = 0; tx < tw; tx++)
                                {
                                    byte idx = reader.ReadByte();
                                    PixelData[((iy + ty) * iw) + (ix + tx)] = (byte)((idx << 4) | (idx >> 4));
                                }
                            }
                        }
                    }
                }
            }

            imageBinary = new ImageBinary();
            imageBinary.Width = ImageWidth;
            imageBinary.Height = ImageHeight;
            imageBinary.InputPaletteFormat = PixelDataFormat.FormatRgba8888;
            imageBinary.InputPixelFormat = (ColorCount == 256 ? PixelDataFormat.FormatIndexed8 : PixelDataFormat.FormatIndexed4);
            imageBinary.InputEndianness = Endian.BigEndian;

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
