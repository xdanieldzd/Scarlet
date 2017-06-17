using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using Scarlet.Drawing;
using Scarlet.IO;

using Scarlet.Platform.Nintendo;

namespace Scarlet.IO.ImageFormats
{
    [MagicNumber("GCT0", 0x00)]
    public class GCT : ImageFormat
    {
        public string MagicNumber { get; private set; }
        public GCNTextureFormat Format { get; private set; }
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }
        public uint Flags { get; private set; }
        public uint ImageOffset { get; private set; }
        public uint PaletteOffset { get; private set; }
        public float Unknown0x18 { get; private set; }
        public float Unknown0x1C { get; private set; }
        public byte[] Padding { get; private set; }

        public byte[] PixelData { get; private set; }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            reader.Endianness = Endian.BigEndian;

            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            Format = (GCNTextureFormat)reader.ReadUInt32();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            Flags = reader.ReadUInt32();
            ImageOffset = reader.ReadUInt32();
            PaletteOffset = reader.ReadUInt32();
            Unknown0x18 = reader.ReadSingle();
            Unknown0x1C = reader.ReadSingle();
            Padding = reader.ReadBytes(0x20);

            PixelData = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
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

            imageBinary.InputEndianness = Endian.BigEndian;
            imageBinary.Width = Width;
            imageBinary.Height = Height;

            PixelDataFormat pixelDataFormat;
            byte[] normalizedData;

            if (Format == GCNTextureFormat.RGBA8)
            {
                normalizedData = new byte[PixelData.Length];
                for (int i = 0; i < normalizedData.Length; i += 64)
                {
                    for (int j = 0, k = 0; j < 32; j += 2, k += 4) normalizedData[i + k] = PixelData[i + j];    // A
                    for (int j = 1, k = 1; j < 32; j += 2, k += 4) normalizedData[i + k] = PixelData[i + j];    // R
                    for (int j = 32, k = 2; j < 64; j += 2, k += 4) normalizedData[i + k] = PixelData[i + j];   // G
                    for (int j = 33, k = 3; j < 64; j += 2, k += 4) normalizedData[i + k] = PixelData[i + j];   // B
                }
                pixelDataFormat = PixelDataFormat.FormatArgb8888;
            }
            else
            {
                normalizedData = PixelData;
                pixelDataFormat = GCN.GetPixelDataFormat(Format);
            }

            pixelDataFormat |= PixelDataFormat.PixelOrderingTiled;

            imageBinary.InputPixelFormat = pixelDataFormat;
            imageBinary.AddInputPixels(normalizedData);

            return imageBinary.GetBitmap();
        }
    }
}
