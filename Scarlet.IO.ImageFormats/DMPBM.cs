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
    public enum DMPBMPixelFormat : byte
    {
        Alpha8 = 0x00,
        Rgba5551 = 0x01,
        Rgba4444 = 0x02,
        Rgba8888 = 0x03,
        Indexed8 = 0x04
    }

    // TODO: verify indexed format & palette handling

    [MagicNumber("DMPBM", 0x00)]
    public class DMPBM : ImageFormat
    {
        public string MagicNumber { get; private set; }
        public DMPBMPixelFormat PixelFormat { get; private set; }
        public uint Width { get; private set; }
        public uint Height { get; private set; }

        public byte[] PaletteData { get; private set; }
        public byte[] PixelData { get; private set; }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(5));
            PixelFormat = (DMPBMPixelFormat)reader.ReadByte();
            Width = reader.ReadUInt32();
            Height = reader.ReadUInt32();

            switch (PixelFormat)
            {
                case DMPBMPixelFormat.Indexed8: PaletteData = reader.ReadBytes(256 * 2); break;
                default: PaletteData = null; break;
            }

            PixelData = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
        }

        public override int GetImageCount()
        {
            return 1;
        }

        public override int GetPaletteCount()
        {
            return (PaletteData != null ? 1 : 0);
        }

        protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
        {
            ImageBinary imageBinary = new ImageBinary();

            PixelDataFormat pixelDataFormat = PixelDataFormat.Undefined;
            switch (PixelFormat)
            {
                case DMPBMPixelFormat.Alpha8: pixelDataFormat = PixelDataFormat.FormatAlpha8; break;
                case DMPBMPixelFormat.Rgba4444: pixelDataFormat = PixelDataFormat.FormatRgba4444; break;
                case DMPBMPixelFormat.Rgba5551: pixelDataFormat = PixelDataFormat.FormatRgba5551; break;
                case DMPBMPixelFormat.Rgba8888: pixelDataFormat = PixelDataFormat.FormatRgba8888; break;
                case DMPBMPixelFormat.Indexed8: pixelDataFormat = PixelDataFormat.FormatIndexed8; break;
                default: throw new NotImplementedException(string.Format("DMPBM format 0x{0:X}", PixelFormat));
            }
            pixelDataFormat |= PixelDataFormat.PixelOrderingTiled3DS;

            imageBinary.Width = (int)Width;
            imageBinary.Height = (int)Height;
            imageBinary.InputPixelFormat = pixelDataFormat;
            imageBinary.InputEndianness = Endian.LittleEndian;

            imageBinary.AddInputPixels(PixelData);

            if (PaletteData != null)
            {
                switch (PixelFormat)
                {
                    // XBGR1555 ?
                    case DMPBMPixelFormat.Indexed8:
                        imageBinary.InputPaletteFormat = (PixelDataFormat.Bpp16 | PixelDataFormat.ChannelsXbgr | PixelDataFormat.RedBits5 | PixelDataFormat.GreenBits5 | PixelDataFormat.BlueBits5 | PixelDataFormat.AlphaBits1);
                        break;

                    default: throw new NotImplementedException(string.Format("DMPBM palette for format 0x{0:X}", PixelFormat));
                }

                imageBinary.AddInputPalette(PaletteData);
            }

            Bitmap bitmap = imageBinary.GetBitmap();
            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            return bitmap;
        }
    }
}
