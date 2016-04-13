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
    [MagicNumber("XGTL", 0x00)]
    public class XGTL : ImageFormat
    {
        public string MagicNumber { get; private set; }
        public uint FileSize { get; private set; }
        public uint Width { get; private set; }
        public uint Height { get; private set; }

        public uint SegmentWidth { get; private set; }
        public uint SegmentHeight { get; private set; }
        public uint SegmentCountHorizontal { get; private set; }
        public uint SegmentCountVertical { get; private set; }

        public uint ImageDataOffset { get; private set; }
        public uint ImageDataSize { get; private set; }
        public uint Unknown0x28 { get; private set; }
        public uint Unknown0x2C { get; private set; }

        TXG[,] segments;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            long startPosition = reader.BaseStream.Position;

            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            FileSize = reader.ReadUInt32();
            Width = reader.ReadUInt32();
            Height = reader.ReadUInt32();

            SegmentWidth = reader.ReadUInt32();
            SegmentHeight = reader.ReadUInt32();
            SegmentCountHorizontal = reader.ReadUInt32();
            SegmentCountVertical = reader.ReadUInt32();

            ImageDataOffset = reader.ReadUInt32();
            ImageDataSize = reader.ReadUInt32();
            Unknown0x28 = reader.ReadUInt32();
            Unknown0x2C = reader.ReadUInt32();

            reader.BaseStream.Seek(startPosition + ImageDataOffset, SeekOrigin.Begin);

            segments = new TXG[SegmentCountHorizontal, SegmentCountVertical];
            for (int y = 0; y < SegmentCountVertical; y++)
            {
                for (int x = 0; x < SegmentCountHorizontal; x++)
                {
                    using (MemoryStream stream = new MemoryStream(reader.ReadBytes((int)ImageDataSize)))
                    {
                        TXG txgInstance = new TXG();
                        txgInstance.Open(stream);
                        segments[x, y] = txgInstance;
                    }
                }
            }
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
            Bitmap image = new Bitmap((int)Width, (int)Height);

            using (Graphics g = Graphics.FromImage(image))
            {
                for (int y = 0; y < SegmentCountVertical; y++)
                {
                    for (int x = 0; x < SegmentCountHorizontal; x++)
                    {
                        using (Bitmap segmentBitmap = segments[x, y].GetBitmap())
                        {
                            g.DrawImageUnscaled(segmentBitmap, (int)(x * SegmentWidth), (int)(y * SegmentHeight));
                        }
                    }
                }
            }

            return image;
        }
    }
}
