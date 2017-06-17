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
    // TODO: general improvements, make less hacky

    public class CTXBTexture
    {
        public uint DataLength { get; private set; }
        public ushort Unknown04 { get; private set; }
        public ushort Unknown06 { get; private set; }
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }
        public PicaPixelFormat PixelFormat { get; private set; }
        public PicaDataType DataType { get; private set; }
        public uint DataOffset { get; private set; }
        public string Name { get; private set; }

        public CTXBTexture(CTXB parent, BinaryReader reader)
        {
            DataLength = reader.ReadUInt32();
            Unknown04 = reader.ReadUInt16();
            Unknown06 = reader.ReadUInt16();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            PixelFormat = (PicaPixelFormat)reader.ReadUInt16();
            DataType = (PicaDataType)reader.ReadUInt16();
            DataOffset = reader.ReadUInt32();
            Name = Encoding.ASCII.GetString(reader.ReadBytes(16), 0, 16).TrimEnd('\0');
        }
    }

    [MagicNumber("ctxb", 0x00)]
    [FilenamePattern("^.*\\.ctxb$")]
    public class CTXB : ImageFormat
    {
        /* ctxb */
        public string MagicNumber { get; private set; }
        public uint FileSize { get; private set; }
        public uint NumberOfChunks { get; private set; }
        public uint Unknown1 { get; private set; }
        public uint TexChunkOffset { get; private set; }
        public uint TextureDataOffset { get; private set; }

        /* tex */
        public string TexChunkTag { get; private set; }
        public uint TexChunkSize { get; private set; }
        public uint TextureCount { get; private set; }
        public CTXBTexture[] Textures { get; private set; }

        byte[][] pixelData;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4), 0, 4);
            FileSize = reader.ReadUInt32();
            NumberOfChunks = reader.ReadUInt32();
            Unknown1 = reader.ReadUInt32();
            TexChunkOffset = reader.ReadUInt32();
            TextureDataOffset = reader.ReadUInt32();

            reader.BaseStream.Seek(TexChunkOffset, SeekOrigin.Begin);

            TexChunkTag = Encoding.ASCII.GetString(reader.ReadBytes(4), 0, 4);
            TexChunkSize = reader.ReadUInt32();
            TextureCount = reader.ReadUInt32();

            if (TexChunkTag != "tex ") throw new Exception("CTXB parsing error");

            Textures = new CTXBTexture[TextureCount];
            pixelData = new byte[TextureCount][];

            for (int i = 0; i < Textures.Length; i++)
            {
                reader.BaseStream.Seek(TexChunkOffset + 0xC + (i * 0x24), SeekOrigin.Begin);
                Textures[i] = new CTXBTexture(this, reader);

                reader.BaseStream.Seek(TextureDataOffset + Textures[i].DataOffset, SeekOrigin.Begin);
                pixelData[i] = reader.ReadBytes((int)Textures[i].DataLength);
            }
        }

        public override int GetImageCount()
        {
            return (int)TextureCount;
        }

        public override int GetPaletteCount()
        {
            return 0;
        }

        protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
        {
            CTXBTexture texture = Textures[imageIndex];

            PicaPixelFormat pixelFormat = texture.PixelFormat;
            PicaDataType dataType = ((pixelFormat == PicaPixelFormat.ETC1RGB8NativeDMP || pixelFormat == PicaPixelFormat.ETC1AlphaRGB8A4NativeDMP) ? PicaDataType.UnsignedByte : texture.DataType);

            ImageBinary imageBinary = new ImageBinary();
            imageBinary.Width = texture.Width;
            imageBinary.Height = texture.Height;
            imageBinary.InputPixelFormat = N3DS.GetPixelDataFormat(dataType, pixelFormat);
            imageBinary.InputEndianness = Endian.LittleEndian;
            imageBinary.AddInputPixels(pixelData[imageIndex]);

            return imageBinary.GetBitmap(imageIndex, paletteIndex);
        }
    }
}
