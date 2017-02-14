using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

using Scarlet.Drawing;
using Scarlet.IO;

using Scarlet.Platform.Nintendo;

namespace Scarlet.IO.ImageFormats
{
    // TODO: indexed formats, the oddballs and stuff, see Scarlet Platform/Nintendo/GCN

    [MagicNumber("GCNT", 0x00)]
    public class GCNT : ImageFormat
    {
        public string MagicNumber { get; private set; }
        public uint Unknown0x04 { get; private set; }
        public uint Unknown0x08 { get; private set; }
        public uint ImageDataSize { get; private set; }
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }
        public GCNTextureFormat PixelFormat { get; private set; }
        public GCNPaletteFormat PaletteFormat { get; private set; }
        public ushort Unknown0x16 { get; private set; }
        public uint Unknown0x18 { get; private set; }
        public uint Unknown0x1C { get; private set; }
        public string Comment { get; private set; }

        public byte[] PixelData { get; private set; }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            reader.Endianness = Endian.BigEndian;

            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            Unknown0x04 = reader.ReadUInt32();
            Unknown0x08 = reader.ReadUInt32();
            ImageDataSize = reader.ReadUInt32();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            PixelFormat = (GCNTextureFormat)reader.ReadByte();
            PaletteFormat = (GCNPaletteFormat)reader.ReadByte();
            Unknown0x16 = reader.ReadUInt16();
            Unknown0x18 = reader.ReadUInt32();
            Unknown0x1C = reader.ReadUInt32();
            Comment = Encoding.ASCII.GetString(reader.ReadBytes(0x20));

            PixelData = reader.ReadBytes((int)ImageDataSize);
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

            imageBinary.Width = Width;
            imageBinary.Height = Height;
            imageBinary.InputPixelFormat = GCN.GetPixelDataFormat(PixelFormat) | PixelDataFormat.PixelOrderingTiledGCN;
            imageBinary.InputEndianness = Endian.BigEndian;

            imageBinary.AddInputPixels(PixelData);

            return imageBinary.GetBitmap();
        }
    }
}
