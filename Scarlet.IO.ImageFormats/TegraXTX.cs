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
    /* NvFD/"XTX"/Tegra X1 textures - https://github.com/aboood40091/XTX-Extractor/wiki */

    public enum TegraXTXPixelFormat : uint
    {
        R8 = 0x01,

        RG8 = 0x0D,

        RGBA8 = 0x25,

        RGBA8_SRGB = 0x38,
        RGBA4 = 0x39,

        RGB5A1 = 0x3B,
        RGB565 = 0x3C,
        RGB10A2 = 0x3D,

        DXT1 = 0x42,
        DXT3 = 0x43,
        DXT5 = 0x44,

        BC4U = 0x49,
        BC4S = 0x4A,
        BC5U = 0x4B,
        BC5S = 0x4C
    }

    public class TegraXTXFileHeader
    {
        public string MagicNumber { get; private set; }
        public uint HeaderSize { get; private set; }
        public uint MajorVersion { get; private set; }
        public uint MinorVersion { get; private set; }

        public TegraXTXFileHeader(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            HeaderSize = reader.ReadUInt32();
            MajorVersion = reader.ReadUInt32();
            MinorVersion = reader.ReadUInt32();
        }
    }

    public class TegraXTXBlockHeader
    {
        public string MagicNumber { get; private set; }
        public uint HeaderSize { get; private set; }
        public ulong DataSize { get; private set; }
        public ulong DataOffset { get; private set; }
        public uint BlockType { get; private set; }
        public uint GlobalBlockIndex { get; private set; }              // ???
        public uint IncreasingBlockTypeIndex { get; private set; }      // ???

        public TegraXTXBlockHeader(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            HeaderSize = reader.ReadUInt32();
            DataSize = reader.ReadUInt64();
            DataOffset = reader.ReadUInt64();
            BlockType = reader.ReadUInt32();
            GlobalBlockIndex = reader.ReadUInt32();
            IncreasingBlockTypeIndex = reader.ReadUInt32();
        }
    }

    public interface ITegraXTXBlock { }

    public class TegraXTXTextureInfo : ITegraXTXBlock
    {
        public TegraXTXBlockHeader BlockHeader { get; private set; }
        public ulong DataSize { get; private set; }
        public uint Alignment { get; private set; }
        public uint Width { get; private set; }
        public uint Height { get; private set; }
        public uint Depth { get; private set; }
        public uint Target { get; private set; }
        public TegraXTXPixelFormat Format { get; private set; }
        public uint NumMipmaps { get; private set; }
        public uint SliceSize { get; private set; }                     // ???
        public uint[] MipmapOffsets { get; private set; }               // 17 * 0x4 bytes
        public ulong PackagedTextureLayout { get; private set; }        // ???
        public uint Boolean { get; private set; }                       // ???

        public TegraXTXTextureInfo(TegraXTXBlockHeader header, EndianBinaryReader reader)
        {
            BlockHeader = header;
            DataSize = reader.ReadUInt64();
            Alignment = reader.ReadUInt32();
            Width = reader.ReadUInt32();
            Height = reader.ReadUInt32();
            Depth = reader.ReadUInt32();
            Target = reader.ReadUInt32();
            Format = (TegraXTXPixelFormat)reader.ReadUInt32();
            NumMipmaps = reader.ReadUInt32();
            SliceSize = reader.ReadUInt32();
            MipmapOffsets = new uint[17];
            for (int i = 0; i < MipmapOffsets.Length; i++) MipmapOffsets[i] = reader.ReadUInt32();
            PackagedTextureLayout = reader.ReadUInt64();
            Boolean = reader.ReadUInt32();
        }
    }

    public class TegraXTXTextureData : ITegraXTXBlock
    {
        public TegraXTXBlockHeader BlockHeader { get; private set; }
        public byte[] Data { get; private set; }

        public TegraXTXTextureData(TegraXTXBlockHeader header, EndianBinaryReader reader)
        {
            BlockHeader = header;
            Data = reader.ReadBytes((int)BlockHeader.DataSize);
        }
    }

    [MagicNumber("DFvN", 0x00)]
    public class TegraXTX : ImageFormat
    {
        Dictionary<TegraXTXPixelFormat, Tuple<PixelDataFormat, int>> formatMap = new Dictionary<TegraXTXPixelFormat, Tuple<PixelDataFormat, int>>()
        {
            { TegraXTXPixelFormat.R8, new Tuple<PixelDataFormat, int>(PixelDataFormat.FormatLuminance8, 8) },
            //{ TegraXTXPixelFormat.RG8, new Tuple<PixelDataFormat, int>(PixelDataFormat.Undefined, 16) },
            { TegraXTXPixelFormat.RGBA8, new Tuple<PixelDataFormat, int>(PixelDataFormat.FormatRgba8888, 32) },
            { TegraXTXPixelFormat.RGBA8_SRGB, new Tuple<PixelDataFormat, int>(PixelDataFormat.FormatRgba8888, 32) },        // verify me...?
            { TegraXTXPixelFormat.RGBA4, new Tuple<PixelDataFormat, int>(PixelDataFormat.FormatRgba4444, 16) },
            { TegraXTXPixelFormat.RGB5A1, new Tuple<PixelDataFormat, int>(PixelDataFormat.FormatRgba5551, 16) },
            { TegraXTXPixelFormat.RGB565, new Tuple<PixelDataFormat, int>(PixelDataFormat.FormatRgb565, 16) },
            //{ TegraXTXPixelFormat.RGB10A2, new Tuple<PixelDataFormat, int>(PixelDataFormat.Undefined, 32) },
            { TegraXTXPixelFormat.DXT1, new Tuple<PixelDataFormat, int>(PixelDataFormat.FormatDXT1Rgb, 4) },
            { TegraXTXPixelFormat.DXT3, new Tuple<PixelDataFormat, int>(PixelDataFormat.FormatDXT3, 8) },
            { TegraXTXPixelFormat.DXT5, new Tuple<PixelDataFormat, int>(PixelDataFormat.FormatDXT5, 8) },
            { TegraXTXPixelFormat.BC4U, new Tuple<PixelDataFormat, int>(PixelDataFormat.FormatRGTC1, 4) },
            { TegraXTXPixelFormat.BC4S, new Tuple<PixelDataFormat, int>(PixelDataFormat.FormatRGTC1_Signed, 4) },
            { TegraXTXPixelFormat.BC5U, new Tuple<PixelDataFormat, int>(PixelDataFormat.FormatRGTC2, 8) },
            { TegraXTXPixelFormat.BC5S, new Tuple<PixelDataFormat, int>(PixelDataFormat.FormatRGTC2_Signed, 8) },
        };

        public TegraXTXFileHeader FileHeader { get; private set; }
        public List<ITegraXTXBlock> Blocks { get; private set; }

        List<MipmapLevel> mipmapData;
        Tuple<PixelDataFormat, int> mappingInfo;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            /* Read header */
            FileHeader = new TegraXTXFileHeader(reader);

            /* Read blocks */
            Blocks = new List<ITegraXTXBlock>();
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                long blockHeaderPosition = reader.BaseStream.Position;
                TegraXTXBlockHeader blockHeader = new TegraXTXBlockHeader(reader);

                reader.BaseStream.Seek((blockHeaderPosition + (long)blockHeader.DataOffset), SeekOrigin.Begin);
                switch (blockHeader.BlockType)
                {
                    case 0x02: Blocks.Add(new TegraXTXTextureInfo(blockHeader, reader)); break;
                    case 0x03: Blocks.Add(new TegraXTXTextureData(blockHeader, reader)); break;
                }

                reader.BaseStream.Seek((blockHeaderPosition + blockHeader.HeaderSize + (long)blockHeader.DataSize), SeekOrigin.Begin);
            }

            if (Blocks.Count(x => x is TegraXTXTextureInfo) != 1) throw new Exception("Unexpected number of Tegra texture infos");
            if (Blocks.Count(x => x is TegraXTXTextureData) != 1) throw new Exception("Unexpected number of Tegra texture datas");

            /* Read mipmaps */
            var textureInfo = (Blocks.FirstOrDefault(x => x is TegraXTXTextureInfo) as TegraXTXTextureInfo);
            var textureData = (Blocks.FirstOrDefault(x => x is TegraXTXTextureData) as TegraXTXTextureData);

            if (!formatMap.ContainsKey(textureInfo.Format)) throw new Exception($"Unsupported Tegra pixel format {textureInfo.Format}");
            mappingInfo = formatMap[textureInfo.Format];

            using (EndianBinaryReader textureReader = new EndianBinaryReader(new MemoryStream(textureData.Data)))
            {
                mipmapData = new List<MipmapLevel>();
                for (int i = 0; i < textureInfo.NumMipmaps; i++)
                {
                    int mipWidth = (int)(textureInfo.Width >> i);
                    int mipHeight = (int)(textureInfo.Height >> i);
                    int mipDataSize = ((mappingInfo.Item2 * mipWidth) * mipHeight);
                    textureReader.BaseStream.Seek(textureInfo.MipmapOffsets[i], SeekOrigin.Begin);
                    byte[] mipPixelData = textureReader.ReadBytes(mipDataSize);
                    mipmapData.Add(new MipmapLevel(mipWidth, mipHeight, mipPixelData));
                }
            }
        }

        public override int GetImageCount()
        {
            return mipmapData.Count;
        }

        public override int GetPaletteCount()
        {
            return 0;
        }

        protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
        {
            ImageBinary imageBinary = new ImageBinary();
            imageBinary.Width = mipmapData[imageIndex].Width;
            imageBinary.Height = mipmapData[imageIndex].Height;
            imageBinary.InputPixelFormat = mappingInfo.Item1;       // TODO: unswizzle!
            imageBinary.InputEndianness = Endian.LittleEndian;
            imageBinary.AddInputPixels(mipmapData[imageIndex].PixelData);

            return imageBinary.GetBitmap(0, 0);
        }
    }
}
