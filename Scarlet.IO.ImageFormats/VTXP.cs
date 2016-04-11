using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

using Scarlet.Drawing;
using Scarlet.IO;

using Scarlet.Platform.Sony;

namespace Scarlet.IO.ImageFormats
{
    public enum VtxpTextureType
    {
        Linear = 0x00,
        Swizzled = 0x02
    }

    public class VtxpImageHeader
    {
        public uint FilenameOffset { get; private set; }
        public uint ImageDataSize { get; private set; }
        public uint PaletteOffset { get; private set; }
        public uint PixelDataOffset { get; private set; }
        public SceGxmTextureFormat TextureFormat { get; private set; }
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }
        public byte Unknown0x18 { get; private set; }
        public VtxpTextureType TextureType { get; private set; }
        public byte[] Padding { get; private set; }

        public string Filename { get; private set; }
        public byte[] PixelData { get; private set; }
        public byte[] PaletteData { get; private set; }

        public VtxpImageHeader(EndianBinaryReader reader)
        {
            FilenameOffset = reader.ReadUInt32();
            ImageDataSize = reader.ReadUInt32();
            PaletteOffset = reader.ReadUInt32();
            PixelDataOffset = reader.ReadUInt32();
            TextureFormat = (SceGxmTextureFormat)reader.ReadUInt32();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            Unknown0x18 = reader.ReadByte();
            TextureType = (VtxpTextureType)reader.ReadByte();
            Padding = reader.ReadBytes(6);

            long lastPosition = reader.BaseStream.Position;

            reader.BaseStream.Seek(FilenameOffset, SeekOrigin.Begin);
            Filename = FileFormat.ReadNullTermString(reader.BaseStream);

            if (PaletteOffset != 0)
            {
                reader.BaseStream.Seek(PaletteOffset, SeekOrigin.Begin);
                PaletteData = reader.ReadBytes((int)(PixelDataOffset - PaletteOffset));
            }

            reader.BaseStream.Seek(PixelDataOffset, SeekOrigin.Begin);
            PixelData = reader.ReadBytes((int)(ImageDataSize - (PaletteData != null ? PaletteData.Length : 0)));

            reader.BaseStream.Seek(lastPosition, SeekOrigin.Begin);
        }
    }

    [MagicNumber("VTXP", 0x00)]
    public class VTXP : ImageFormat
    {
        public string MagicNumber { get; private set; }
        public uint Unknown0x04 { get; private set; }
        public uint NumImages { get; private set; }
        public uint UnkTablePointer { get; private set; }
        public byte[] Padding { get; private set; }

        public VtxpImageHeader[] ImageHeaders { get; private set; }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            Unknown0x04 = reader.ReadUInt32();
            NumImages = reader.ReadUInt32();
            UnkTablePointer = reader.ReadUInt32();
            Padding = reader.ReadBytes(16);

            ImageHeaders = new VtxpImageHeader[NumImages];
            for (int i = 0; i < ImageHeaders.Length; i++) ImageHeaders[i] = new VtxpImageHeader(reader);
        }

        public override int GetImageCount()
        {
            return (int)NumImages;
        }

        public override int GetPaletteCount()
        {
            return 1;
        }

        public override string GetImageName(int imageIndex)
        {
            return ImageHeaders[imageIndex].Filename;
        }

        protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
        {
            // TODO: allow Scarlet to use filenames stored inside image files for extraction, if applicable!

            VtxpImageHeader imageHeader = ImageHeaders[imageIndex];

            bool isIndexed = (imageHeader.PaletteOffset != 0);

            ImageBinary imageBinary = new ImageBinary();

            imageBinary.Width = imageHeader.Width;
            imageBinary.Height = imageHeader.Height;
            imageBinary.InputEndianness = Endian.LittleEndian;

            if (isIndexed)
            {
                imageBinary.InputPaletteFormat = PSVita.GetPaletteFormat(imageHeader.TextureFormat);
                imageBinary.AddInputPalette(imageHeader.PaletteData);
            }

            imageBinary.InputPixelFormat = PSVita.GetPixelDataFormat(imageHeader.TextureFormat);
            imageBinary.AddInputPixels(imageHeader.PixelData);

            if (imageHeader.TextureType == VtxpTextureType.Swizzled)
                imageBinary.InputPixelFormat |= PixelDataFormat.PostProcessUnswizzle_Vita;

            return imageBinary.GetBitmap();
        }
    }
}
