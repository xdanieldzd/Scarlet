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
    [MagicNumber("GXT", 0x00)]
    [DefaultExtension(".gxt")]
    public class GXT : ImageFormat
    {
        public SceGxtHeader Header { get; private set; }
        public SceGxtTextureInfo[] TextureInfos { get; private set; }

        public BUVChunk BUVChunk { get; private set; }

        public byte[][] P4Palettes { get; private set; }
        public byte[][] P8Palettes { get; private set; }

        public byte[][] PixelData { get; private set; }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            Header = new SceGxtHeader(reader);

            Func<EndianBinaryReader, SceGxtTextureInfo> textureInfoGeneratorFunc;
            switch (Header.Version)
            {
                case 0x10000003: textureInfoGeneratorFunc = new Func<EndianBinaryReader, SceGxtTextureInfo>((r) => { return new SceGxtTextureInfoV301(r); }); break;
                case 0x10000002: textureInfoGeneratorFunc = new Func<EndianBinaryReader, SceGxtTextureInfo>((r) => { return new SceGxtTextureInfoV201(r); }); break;
                case 0x10000001: textureInfoGeneratorFunc = new Func<EndianBinaryReader, SceGxtTextureInfo>((r) => { return new SceGxtTextureInfoV101(r); }); break;
                default: throw new Exception("GXT version not implemented");
            }

            TextureInfos = new SceGxtTextureInfo[Header.NumTextures];
            for (int i = 0; i < TextureInfos.Length; i++)
                TextureInfos[i] = textureInfoGeneratorFunc(reader);

            // TODO: any other way to detect these?
            if (Encoding.ASCII.GetString(reader.ReadBytes(4)) == BUVChunk.ExpectedMagicNumber)
            {
                reader.BaseStream.Seek(-4, SeekOrigin.Current);
                BUVChunk = new BUVChunk(reader);
            }

            ReadAllBasePalettes(reader);
            ReadAllTextures(reader);
        }

        private void ReadAllBasePalettes(EndianBinaryReader reader)
        {
            long paletteOffset = (Header.TextureDataOffset + Header.TextureDataSize) - (((Header.NumP8Palettes * 256) * 4) + ((Header.NumP4Palettes * 16) * 4));
            reader.BaseStream.Seek(paletteOffset, SeekOrigin.Begin);

            P4Palettes = new byte[Header.NumP4Palettes][];
            for (int i = 0; i < P4Palettes.Length; i++) P4Palettes[i] = reader.ReadBytes(16 * 4);

            P8Palettes = new byte[Header.NumP8Palettes][];
            for (int i = 0; i < P8Palettes.Length; i++) P8Palettes[i] = reader.ReadBytes(256 * 4);
        }

        private void ReadAllTextures(EndianBinaryReader reader)
        {
            PixelData = new byte[Header.NumTextures][];
            for (int i = 0; i < TextureInfos.Length; i++)
            {
                SceGxtTextureInfo info = TextureInfos[i];

                reader.BaseStream.Seek(info.DataOffset, SeekOrigin.Begin);
                PixelData[i] = reader.ReadBytes((int)info.DataSize);
            }
        }

        private Bitmap CreateBitmap(int infoIdx, int forcePaletteIdx = -1)
        {
            SceGxtTextureInfo info = TextureInfos[infoIdx];

            ImageBinary imageBinary = new ImageBinary();

            imageBinary.Width = info.GetWidth();
            imageBinary.Height = info.GetHeight();
            imageBinary.InputPixelFormat = PSVita.GetPixelDataFormat(info.GetTextureFormat());
            imageBinary.InputEndianness = Endian.LittleEndian;
            imageBinary.AddInputPixels(PixelData[infoIdx]);

            SceGxmTextureBaseFormat textureBaseFormat = info.GetTextureBaseFormat();

            if (textureBaseFormat != SceGxmTextureBaseFormat.PVRT2BPP && textureBaseFormat != SceGxmTextureBaseFormat.PVRT4BPP)
            {
                SceGxmTextureType textureType = info.GetTextureType();
                switch (textureType)
                {
                    case SceGxmTextureType.Linear:
                        // Nothing to be done!
                        break;

                    case SceGxmTextureType.Tiled:
                        // TODO: verify me!
                        imageBinary.InputPixelFormat |= PixelDataFormat.PostProcessUntile_3DS;
                        break;

                    case SceGxmTextureType.Swizzled:
                    case SceGxmTextureType.Cube:
                        // TODO: is cube really the same as swizzled? seems that way from CS' *env* files...
                        imageBinary.InputPixelFormat |= PixelDataFormat.PostProcessUnswizzle_Vita;
                        break;
                }
            }

            if (textureBaseFormat == SceGxmTextureBaseFormat.P4 || textureBaseFormat == SceGxmTextureBaseFormat.P8)
            {
                // TODO: implement alternate palette formats as in GXTConvert

                imageBinary.InputPaletteFormat = PixelDataFormat.FormatAbgr8888;

                if (textureBaseFormat == SceGxmTextureBaseFormat.P4)
                    foreach (byte[] paletteData in P4Palettes)
                        imageBinary.AddInputPalette(paletteData);
                else if (textureBaseFormat == SceGxmTextureBaseFormat.P8)
                    foreach (byte[] paletteData in P8Palettes)
                        imageBinary.AddInputPalette(paletteData);
            }

            return imageBinary.GetBitmap(0, forcePaletteIdx != -1 ? forcePaletteIdx : info.PaletteIndex);
        }

        public override int GetImageCount()
        {
            return TextureInfos.Length;
        }

        public override int GetPaletteCount()
        {
            return 0;
        }

        protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
        {
            return CreateBitmap(imageIndex, -1);
        }

        public IEnumerable<Bitmap> GetBUVBitmaps()
        {
            if (BUVChunk == null) return null;

            List<Bitmap> bitmaps = new List<Bitmap>();

            foreach (BUVEntry entry in BUVChunk.Entries)
            {
                using (Bitmap sourceImage = CreateBitmap(0, entry.PaletteIndex))
                {
                    bitmaps.Add(sourceImage.Clone(new Rectangle(entry.X, entry.Y, entry.Width, entry.Height), sourceImage.PixelFormat));
                }
            }

            return bitmaps;
        }
    }
}
