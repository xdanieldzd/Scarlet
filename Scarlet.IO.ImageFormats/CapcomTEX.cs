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
    // Header layout from https://github.com/FrozenFish24/TurnaboutTools/blob/master/TEXporter/TEXporter/Program.cs

    [MagicNumber("TEX\0", 0x00)]
    public class CapcomTEX : ImageFormat
    {
        public string MagicNumber { get; private set; }
        public uint[] Data { get; private set; }

        public uint Constant { get; private set; }
        public uint Unknown1 { get; private set; }
        public uint Unknown2 { get; private set; }
        public uint MipmapCount { get; private set; }
        public uint Width { get; private set; }
        public uint Height { get; private set; }
        public uint Format { get; private set; }

        PixelDataFormat pixelDataFormat = PixelDataFormat.Undefined;

        public byte[] PixelData { get; private set; }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));

            Data = new uint[3];
            for (int i = 0; i < Data.Length; i++) Data[i] = reader.ReadUInt32();

            Constant = (Data[0] & 0x00000FFF);
            Unknown1 = ((Data[0] >> 12) & 0xFFF);
            Unknown2 = ((Data[0] >> 28) & 0xF);
            MipmapCount = (Data[1] & 0x3F);
            Width = ((Data[1] >> 6) & 0x1FFF);
            Height = ((Data[1] >> 19) & 0x1FFF);
            Format = ((Data[2] >> 8) & 0xFF);

            int pixelDataStart = 0x10;

            // TODO: remove this hacky crap ~(<_>)~
            if (Unknown1 != 0x40)
                pixelDataStart += (int)(Constant > 0xA4 ? (4 * MipmapCount) : 0);
            else
            {
                if (MipmapCount == 2)
                    pixelDataStart += 0x6C;
                else if (MipmapCount == 3)
                    pixelDataStart += 0xB4;
            }

            reader.BaseStream.Seek(pixelDataStart, SeekOrigin.Begin);
            PixelData = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));

            switch (Format)
            {
                case 0x03: pixelDataFormat = PixelDataFormat.FormatRgba8888 | PixelDataFormat.PixelOrderingTiled3DS; break;
                case 0x07: pixelDataFormat = PixelDataFormat.FormatLuminanceAlpha88 | PixelDataFormat.PixelOrderingTiled3DS; break;
                case 0x0B: pixelDataFormat = PixelDataFormat.FormatETC1_3DS; break;
                case 0x0C: pixelDataFormat = PixelDataFormat.FormatETC1A4_3DS; break;
                case 0x0E: pixelDataFormat = PixelDataFormat.FormatLuminance4 | PixelDataFormat.PixelOrderingTiled3DS; break;
                case 0x10: pixelDataFormat = PixelDataFormat.FormatLuminanceAlpha44 | PixelDataFormat.PixelOrderingTiled3DS; break;
                case 0x11: pixelDataFormat = PixelDataFormat.FormatRgb888 | PixelDataFormat.PixelOrderingTiled3DS; break;
                default: throw new NotImplementedException(string.Format("Capcom TEX format 0x{0:X}", Format));
            }
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
            ImageBinary imageBinary = new ImageBinary();

            imageBinary.Width = (int)Width;
            imageBinary.Height = (int)Height;
            imageBinary.InputPixelFormat = pixelDataFormat;
            imageBinary.InputEndianness = Endian.LittleEndian;

            imageBinary.AddInputPixels(PixelData);

            return imageBinary.GetBitmap();
        }
    }
}
