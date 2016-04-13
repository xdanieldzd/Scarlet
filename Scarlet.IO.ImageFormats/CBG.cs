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
    [MagicNumber("CBG\0", 0x00)]
    public class CBG : ImageFormat
    {
        public string MagicNumber { get; private set; }
        public uint Unknown0x04 { get; private set; }
        public uint ImageDataOffset { get; private set; }
        public uint Unknown0x0C { get; private set; }

        public uint Unknown0x10 { get; private set; }
        public uint Unknown0x14 { get; private set; }
        public uint Unknown0x18 { get; private set; }
        public uint Unknown0x1C { get; private set; }

        XGTL xgtlInstance;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            Unknown0x04 = reader.ReadUInt32();
            ImageDataOffset = reader.ReadUInt32();
            Unknown0x0C = reader.ReadUInt32();

            Unknown0x10 = reader.ReadUInt32();
            Unknown0x14 = reader.ReadUInt32();
            Unknown0x18 = reader.ReadUInt32();
            Unknown0x1C = reader.ReadUInt32();

            reader.BaseStream.Seek(ImageDataOffset, SeekOrigin.Begin);
            xgtlInstance = new XGTL();
            xgtlInstance.Open(reader);
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
            return xgtlInstance.GetBitmap();
        }
    }
}
