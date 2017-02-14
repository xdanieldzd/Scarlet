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
        Indexed8bpp = 0x09,
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
        public TxfHeader[] PixelHeaders { get; private set; }
        public TxfHeader[] PaletteHeaders { get; private set; }

        public byte[][] PixelData { get; private set; }
        public byte[][] PaletteData { get; private set; }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            TxfHeader header = new TxfHeader(reader);

            if (header.PixelDataSize != 0 && header.PixelFormat != TxfPixelFormat.Indexed8bpp)
            {
                PixelHeaders = new TxfHeader[1] { header };
                PaletteHeaders = new TxfHeader[0];

                PixelData = new byte[1][];
                PixelData[0] = reader.ReadBytes((int)header.PixelDataSize);
                PaletteData = new byte[0][];
            }
            else
            {
                List<TxfHeader> pixelHeaders = new List<TxfHeader>();
                List<TxfHeader> paletteHeaders = new List<TxfHeader>();

                pixelHeaders.Add(header);

                while (true)
                {
                    header = new TxfHeader(reader);
                    if (header.Unknown0x01 != 0x01 && header.Unknown0x02 != 0x0101) break;

                    if (header.PixelFormat == TxfPixelFormat.Indexed8bpp)
                        pixelHeaders.Add(header);
                    else
                        paletteHeaders.Add(header);
                }

                PixelData = new byte[pixelHeaders.Count][];
                PaletteData = new byte[paletteHeaders.Count][];

                reader.BaseStream.Seek(-0x10, SeekOrigin.Current);
                long dataStartAddress = reader.BaseStream.Position;

                for (int i = 0; i < pixelHeaders.Count; i++)
                {
                    reader.BaseStream.Seek(dataStartAddress + pixelHeaders[i].PixelDataSize, SeekOrigin.Begin);
                    PixelData[i] = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
                }

                for (int i = 0; i < paletteHeaders.Count; i++)
                {
                    reader.BaseStream.Seek(dataStartAddress + paletteHeaders[i].PixelDataSize, SeekOrigin.Begin);
                    PaletteData[i] = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
                }

                PixelHeaders = pixelHeaders.ToArray();
                PaletteHeaders = paletteHeaders.ToArray();
            }
        }

        public override int GetImageCount()
        {
            return PixelHeaders.Length;
        }

        public override int GetPaletteCount()
        {
            return PaletteHeaders.Length;
        }

        protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
        {
            ImageBinary imageBinary = new ImageBinary();

            TxfHeader pixelHeader = PixelHeaders[imageIndex];
            PixelDataFormat pixelDataFormat = PixelDataFormat.Undefined;

            Endian inputEndian = Endian.BigEndian;

            switch (pixelHeader.PixelFormat)
            {
                case TxfPixelFormat.Argb8888: pixelDataFormat = PixelDataFormat.FormatArgb8888; break;
                case TxfPixelFormat.RgbaDxt1: pixelDataFormat = PixelDataFormat.FormatDXT1Rgba; inputEndian = Endian.LittleEndian; break;
                case TxfPixelFormat.RgbaDxt3: pixelDataFormat = PixelDataFormat.FormatDXT3; inputEndian = Endian.LittleEndian; break;
                case TxfPixelFormat.RgbaDxt5: pixelDataFormat = PixelDataFormat.FormatDXT5; inputEndian = Endian.LittleEndian; break;
                case TxfPixelFormat.Indexed8bpp: pixelDataFormat = PixelDataFormat.FormatIndexed8; break;
                case TxfPixelFormat.Argb1555: pixelDataFormat = PixelDataFormat.FormatArgb1555; break;
                case TxfPixelFormat.Argb4444: pixelDataFormat = PixelDataFormat.FormatArgb4444; break;
                case TxfPixelFormat.Rgb565: pixelDataFormat = PixelDataFormat.FormatRgb565; break;
            }

            imageBinary.Width = pixelHeader.Width;
            imageBinary.Height = pixelHeader.Height;
            imageBinary.InputPixelFormat = pixelDataFormat;
            imageBinary.InputEndianness = inputEndian;
            imageBinary.AddInputPixels(PixelData[imageIndex]);

            if (PaletteHeaders.Length > 0)
            {
                TxfHeader paletteHeader = PaletteHeaders[paletteIndex];
                PixelDataFormat paletteDataFormat = PixelDataFormat.Undefined;

                switch (paletteHeader.PixelFormat)
                {
                    case TxfPixelFormat.Argb8888: paletteDataFormat = PixelDataFormat.FormatArgb8888; break;
                    case TxfPixelFormat.Argb1555: paletteDataFormat = PixelDataFormat.FormatArgb1555; break;
                    case TxfPixelFormat.Argb4444: paletteDataFormat = PixelDataFormat.FormatArgb4444; break;
                    case TxfPixelFormat.Rgb565: paletteDataFormat = PixelDataFormat.FormatRgb565; break;
                }

                imageBinary.InputPaletteFormat = paletteDataFormat;
                imageBinary.AddInputPalette(PaletteData[paletteIndex]);
            }

            return imageBinary.GetBitmap();
        }
    }
}
