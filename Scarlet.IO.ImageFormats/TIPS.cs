//#define ANITEST

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
    // TODO: figure out the rest of this stuff? also fix the really hacky & SLOW eye/mouth/etc stuff...

    // "E:\temp\stellvia\data0\ANIME\EV_AKR_01.TIPS" "E:\temp\stellvia\data0\CARD\CARD_001L.TIPS" "E:\temp\stellvia\data0\BUSTUP\MAIN\ARS_01_01.TIPS" "E:\temp\stellvia\data0\TITLE\TITLE.TIPS" -o "E:\temp\stellvia\output"

    [MagicNumber(new byte[] { 0x0F, 0xC5, 0xBD, 0x43 }, 4)]
    [FilenamePattern("^.*\\.tips$")]
    public class TIPS : ImageFormat
    {
        public uint Unknown0x00 { get; private set; }   // 40000000
        public uint Unknown0x04 { get; private set; }   // 0FC5BD43
        public uint FrameDataOffset { get; private set; }
        public uint PixelDataOffset { get; private set; }
        public ushort RawImageWidth { get; private set; }
        public ushort RawImageHeight { get; private set; }
        public ushort NumImageInformation { get; private set; }
        public byte BlockWidth { get; private set; }
        public byte BlockHeight { get; private set; }
        public ushort Unknown0x18 { get; private set; }
        public ushort Unknown0x1A { get; private set; }
        public uint Unknown0x1C { get; private set; }
        public uint Unknown0x20 { get; private set; }
        public uint Unknown0x24 { get; private set; }
        public uint Unknown0x28 { get; private set; }
        public uint Unknown0x2C { get; private set; }
        public uint Unknown0x30 { get; private set; }
        public uint Unknown0x34 { get; private set; }
        public uint Unknown0x38 { get; private set; }
        public uint Unknown0x3C { get; private set; }

        public TIPSImageInfo[] ImageInfos { get; private set; }
        public Dictionary<TIPSImageInfo, TIPSRectangleInfo[]> RectInfos { get; private set; }

        public List<int[]> ImagePermutations { get; private set; }

        public byte[] PixelData { get; private set; }

        Bitmap rawImage;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            /* Read header(?) */
            Unknown0x00 = reader.ReadUInt32();
            Unknown0x04 = reader.ReadUInt32();
            FrameDataOffset = reader.ReadUInt32();
            PixelDataOffset = reader.ReadUInt32();
            RawImageWidth = reader.ReadUInt16();
            RawImageHeight = reader.ReadUInt16();
            NumImageInformation = reader.ReadUInt16();
            BlockWidth = reader.ReadByte();
            BlockHeight = reader.ReadByte();
            Unknown0x18 = reader.ReadUInt16();
            Unknown0x1A = reader.ReadUInt16();
            Unknown0x1C = reader.ReadUInt32();
            Unknown0x20 = reader.ReadUInt32();
            Unknown0x24 = reader.ReadUInt32();
            Unknown0x28 = reader.ReadUInt32();
            Unknown0x2C = reader.ReadUInt32();
            Unknown0x30 = reader.ReadUInt32();
            Unknown0x34 = reader.ReadUInt32();
            Unknown0x38 = reader.ReadUInt32();
            Unknown0x3C = reader.ReadUInt32();

            /* Get image information */
            ImageInfos = new TIPSImageInfo[NumImageInformation];
            for (int i = 0; i < NumImageInformation; i++)
                ImageInfos[i] = new TIPSImageInfo(reader);

            /* Get rect information */
            RectInfos = new Dictionary<TIPSImageInfo, TIPSRectangleInfo[]>();
            for (int i = 0; i < NumImageInformation; i++)
            {
                TIPSImageInfo imageInfo = ImageInfos[i];

                long position = reader.BaseStream.Position;
                reader.BaseStream.Seek(imageInfo.RectOffset, SeekOrigin.Begin);

                RectInfos.Add(imageInfo, new TIPSRectangleInfo[imageInfo.NumRects]);
                for (int j = 0; j < imageInfo.NumRects; j++)
                    RectInfos[imageInfo][j] = new TIPSRectangleInfo(reader);

                reader.BaseStream.Seek(position, SeekOrigin.Begin);
            }

            /* Get all image permutations (eyes, mouths, etc) */
            ImagePermutations = new List<int[]>();
            int maxType = (int)(ImageInfos.Max(x => x.Index) + 1);
            int[] indices = new int[maxType];
            for (int t = maxType - 1; t >= 0; t--) GetImagePermutations(ref indices, t);

            /* Get pixel data */
            reader.BaseStream.Seek(PixelDataOffset, SeekOrigin.Begin);
            PixelData = reader.ReadBytes((int)(reader.BaseStream.Length - PixelDataOffset));

            /* Scale alpha */
            for (int i = 0; i < PixelData.Length; i += 4)
                PixelData[i + 3] = PS2.ScaleAlpha(PixelData[i + 3]);

            /* Create raw image bitmap */
            ImageBinary rawImageBinary = new ImageBinary();
            if (RawImageWidth != 0 && RawImageHeight != 0)
            {
                rawImageBinary.Width = RawImageWidth;
                rawImageBinary.Height = RawImageHeight;
                rawImageBinary.InputPixelFormat = PixelDataFormat.FormatAbgr8888;
                rawImageBinary.AddInputPixels(PixelData);
                rawImage = rawImageBinary.GetBitmap();
            }
        }

        private void GetImagePermutations(ref int[] indices, int t)
        {
            int numValues = ImageInfos.Where(x => x.Index == t).Count();
            for (int v = 0; v < numValues; v++)
            {
                int[] stored = new int[indices.Length];
                Array.Copy(indices, stored, indices.Length);
                ImagePermutations.Add(stored);
                indices[t]++;
            }

            if (t > 0 && indices[t - 1] != ImageInfos.Where(x => x.Index == t - 1).Count())
            {
                indices[t - 1]++;
                indices[t] = 0;
                GetImagePermutations(ref indices, t);
            }
        }

        private TIPSImageInfo[] GetImageInfos(params int[] indices)
        {
            if (indices.Length != ImageInfos.Max(x => x.Index) + 1) throw new Exception();

            TIPSImageInfo[] infos = new TIPSImageInfo[indices.Length];
            for (int i = 0; i < infos.Length; i++)
            {
                // TODO: hacky!
                infos[i] = ImageInfos.Where(x => x.Index == i).Skip(indices[i]).FirstOrDefault();
                if (infos[i] == null) infos[i] = ImageInfos.LastOrDefault(x => x.Index == i);
            }
            return infos;
        }

        public override int GetImageCount()
        {
            if (RawImageWidth == 0 || RawImageHeight == 0) return 0;
            else
            {
#if ANITEST
                return ImagePermutations.Count;
#else
                return ImageInfos.Length;
#endif
            }
        }

        public override int GetPaletteCount()
        {
            return 1;
        }

        protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
        {
#if ANITEST
            int[] indices = ImagePermutations[imageIndex];

            TIPSImageInfo[] infos = GetImageInfos(indices);
            TIPSImageInfo main = infos.Aggregate((agg, next) => (next != null && next.Height > agg.Height && next.Width > agg.Width) ? next : agg);

            Bitmap image = new Bitmap(main.Width, main.Height);
            using (Graphics g = Graphics.FromImage(image))
            {
                foreach (TIPSImageInfo info in infos.Where(x => x != null))
                {
                    TIPSRectangleInfo[] rectInfos = RectInfos[info];
                    for (int i = 0; i < rectInfos.Length; i++)
                    {
                        TIPSRectangleInfo rectInfo = rectInfos[i];
                        Rectangle sourceRect = new Rectangle(rectInfo.SourceX * BlockWidth, rectInfo.SourceY * BlockHeight, (int)rectInfo.SourceWidth * BlockWidth, BlockHeight);
                        g.DrawImage(rawImage, info.X + (rectInfo.TargetX * BlockWidth), info.Y + (rectInfo.TargetY * BlockHeight), sourceRect, GraphicsUnit.Pixel);
                    }
                }
            }
            return image;
#else
            TIPSImageInfo imageInfo = ImageInfos[imageIndex];
            TIPSRectangleInfo[] rectInfos = RectInfos[imageInfo];
            Bitmap image = new Bitmap(imageInfo.Width, imageInfo.Height);
            using (Graphics g = Graphics.FromImage(image))
            {
                for (int i = 0; i < rectInfos.Length; i++)
                {
                    TIPSRectangleInfo rectInfo = rectInfos[i];
                    Rectangle sourceRect = new Rectangle(rectInfo.SourceX * BlockWidth, rectInfo.SourceY * BlockHeight, (int)rectInfo.SourceWidth * BlockWidth, BlockHeight);
                    g.DrawImage(rawImage, rectInfo.TargetX * BlockWidth, rectInfo.TargetY * BlockHeight, sourceRect, GraphicsUnit.Pixel);
                }
            }
            return image;
#endif
        }
    }

    public class TIPSImageInfo
    {
        public uint Unknown0x00 { get; private set; }   // 40000000
        public uint Unknown0x04 { get; private set; }   // 0FC5BD43
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }
        public ushort X { get; private set; }
        public ushort Y { get; private set; }
        public uint RectOffset { get; private set; }
        public uint NumRects { get; private set; }
        public uint Unknown0x18 { get; private set; }
        public uint Unknown0x1C { get; private set; }
        public uint Index { get; private set; }
        public uint MaybeOtherIndex { get; private set; }
        public uint Unknown0x28 { get; private set; }
        public uint Unknown0x2C { get; private set; }
        public uint Unknown0x30 { get; private set; }
        public uint Unknown0x34 { get; private set; }
        public uint Unknown0x38 { get; private set; }
        public uint Unknown0x3C { get; private set; }

        public TIPSImageInfo(EndianBinaryReader reader)
        {
            Unknown0x00 = reader.ReadUInt32();
            Unknown0x04 = reader.ReadUInt32();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            X = reader.ReadUInt16();
            Y = reader.ReadUInt16();
            RectOffset = reader.ReadUInt32();
            NumRects = reader.ReadUInt32();
            Unknown0x18 = reader.ReadUInt32();
            Unknown0x1C = reader.ReadUInt32();
            Index = reader.ReadUInt32();
            MaybeOtherIndex = reader.ReadUInt32();
            Unknown0x28 = reader.ReadUInt32();
            Unknown0x2C = reader.ReadUInt32();
            Unknown0x30 = reader.ReadUInt32();
            Unknown0x34 = reader.ReadUInt32();
            Unknown0x38 = reader.ReadUInt32();
            Unknown0x3C = reader.ReadUInt32();
        }
    }

    public class TIPSRectangleInfo
    {
        /* In blocks, as per main header! */
        public byte TargetX { get; private set; }
        public byte TargetY { get; private set; }
        public byte SourceX { get; private set; }
        public byte SourceY { get; private set; }
        public uint SourceWidth { get; private set; }

        public TIPSRectangleInfo(EndianBinaryReader reader)
        {
            TargetX = reader.ReadByte();
            TargetY = reader.ReadByte();
            SourceX = reader.ReadByte();
            SourceY = reader.ReadByte();
            SourceWidth = reader.ReadUInt32();
        }
    }
}
