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
    public enum TxfPixelFormat : byte
    {
        Argb8888 = 0x00,
        RgbaDxt1 = 0x02,
        RgbaDxt3 = 0x04,
        RgbaDxt5 = 0x06,
        //Indexed8bpp = 0x09,
        Argb1555 = 0x0B,
        Argb4444 = 0x0C,
        Rgb565 = 0x0D /* assumed */
    };

    public class TxfHeader
    {
        public TxfPixelFormat PixelFormat { get; private set; }
        public byte Unknown0x01 { get; private set; }
        public ushort Unknown0x02 { get; private set; }
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }
        public ushort Unknown0x08 { get; private set; }
        public ushort Unknown0x0A { get; private set; }
        public uint PixelDataSize { get; private set; }

        public TxfHeader(EndianBinaryReader reader)
        {
            PixelFormat = (TxfPixelFormat)reader.ReadByte();
            Unknown0x01 = reader.ReadByte();
            Unknown0x02 = reader.ReadUInt16(Endian.BigEndian);
            Width = reader.ReadUInt16(Endian.BigEndian);
            Height = reader.ReadUInt16(Endian.BigEndian);
            Unknown0x08 = reader.ReadUInt16(Endian.BigEndian);
            Unknown0x0A = reader.ReadUInt16(Endian.BigEndian);
            PixelDataSize = reader.ReadUInt32(Endian.BigEndian);

            if (!Enum.IsDefined(typeof(TxfPixelFormat), PixelFormat)) throw new Exception("Unknown pixel format");
        }
    }

    [FilenamePattern("^.*\\.txf$")]
    public class TXF : ImageFormat
    {
        public TxfHeader Header { get; private set; }

        public byte[] PaletteData { get; private set; }
        public byte[] PixelData { get; private set; }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            Header = new TxfHeader(reader);

            PaletteData = null;
            PixelData = reader.ReadBytes((int)Header.PixelDataSize);
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
            ImageBinary imageBinary = new ImageBinary();

            PixelDataFormat pixelDataFormat = PixelDataFormat.Undefined;
            switch (Header.PixelFormat)
            {
                case TxfPixelFormat.Argb8888: pixelDataFormat = PixelDataFormat.FormatArgb8888; break;
                case TxfPixelFormat.RgbaDxt1: pixelDataFormat = PixelDataFormat.FormatDXT1Rgba; break;
                case TxfPixelFormat.RgbaDxt3: pixelDataFormat = PixelDataFormat.FormatDXT3; break;
                case TxfPixelFormat.RgbaDxt5: pixelDataFormat = PixelDataFormat.FormatDXT5; break;
                case TxfPixelFormat.Argb1555: pixelDataFormat = PixelDataFormat.FormatArgb1555; break;
                case TxfPixelFormat.Argb4444: pixelDataFormat = PixelDataFormat.FormatArgb4444; break;
                case TxfPixelFormat.Rgb565: pixelDataFormat = PixelDataFormat.FormatRgb565; break;
            }

            imageBinary.Width = Header.Width;
            imageBinary.Height = Header.Height;
            //imageBinary.InputPaletteFormat = PixelDataFormat.FormatArgb8888;
            imageBinary.InputPixelFormat = pixelDataFormat;
            imageBinary.InputEndianness = Endian.BigEndian;

            //imageBinary.AddInputPalette(PaletteData);
            imageBinary.AddInputPixels(PixelData);

            return imageBinary.GetBitmap();
        }
    }
}
