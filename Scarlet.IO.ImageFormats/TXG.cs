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
    public enum TxgPixelFormat : uint
    {
        Abgr8888 = 0x00,
        Bgr565 = 0x01,
        Indexed8bpp = 0x04,
    }

    [MagicNumber("TXG\0", 0x00)]
    public class TXG : ImageFormat
    {
        public string MagicNumber { get; private set; }
        public uint FileSize { get; private set; }
        public uint ImageDataOffset { get; private set; }
        public uint ImageDataSize { get; private set; }

        public uint PaletteDataOffset { get; private set; }
        public uint PaletteDataSize { get; private set; }
        public uint PixelDataOffset { get; private set; }
        public uint PixelDataSize { get; private set; }

        public TxgPixelFormat PixelFormat { get; private set; }
        public uint Unknown0x24 { get; private set; } // 3?
        public uint Width { get; private set; }
        public uint Height { get; private set; }

        public byte[] PaletteData { get; private set; }
        public byte[] PixelData { get; private set; }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            long startPosition = reader.BaseStream.Position;

            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            FileSize = reader.ReadUInt32();
            ImageDataOffset = reader.ReadUInt32();
            ImageDataSize = reader.ReadUInt32();

            PaletteDataOffset = reader.ReadUInt32();
            PaletteDataSize = reader.ReadUInt32();
            PixelDataOffset = reader.ReadUInt32();
            PixelDataSize = reader.ReadUInt32();

            PixelFormat = (TxgPixelFormat)reader.ReadUInt32();
            Unknown0x24 = reader.ReadUInt32();
            Width = reader.ReadUInt32();
            Height = reader.ReadUInt32();

            if (PaletteDataSize != 0)
            {
                reader.BaseStream.Seek(startPosition + ImageDataOffset + PaletteDataOffset, SeekOrigin.Begin);
                PaletteData = reader.ReadBytes((int)PaletteDataSize);
            }

            reader.BaseStream.Seek(startPosition + ImageDataOffset + PixelDataOffset, SeekOrigin.Begin);
            PixelData = reader.ReadBytes((int)PixelDataSize);
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

            imageBinary.Width = (int)Width;
            imageBinary.Height = (int)Height;
            imageBinary.InputEndianness = Endian.LittleEndian;

            if (PaletteData != null)
            {
                imageBinary.InputPaletteFormat = PixelDataFormat.FormatAbgr8888;
                imageBinary.AddInputPalette(PaletteData);
            }

            switch (PixelFormat)
            {
                case TxgPixelFormat.Abgr8888:
                    imageBinary.InputPixelFormat = PixelDataFormat.FormatAbgr8888 | PixelDataFormat.PostProcessUnswizzle_PSP;
                    break;

                case TxgPixelFormat.Bgr565:
                    imageBinary.InputPixelFormat = PixelDataFormat.FormatBgr565 | PixelDataFormat.PostProcessUntile_PSP;
                    break;

                case TxgPixelFormat.Indexed8bpp:
                    imageBinary.InputPixelFormat = PixelDataFormat.FormatIndexed8 | PixelDataFormat.PostProcessUnswizzle_PSP;
                    break;

                default: throw new NotImplementedException();
            }

            imageBinary.AddInputPixels(PixelData);

            return imageBinary.GetBitmap();
        }
    }
}
