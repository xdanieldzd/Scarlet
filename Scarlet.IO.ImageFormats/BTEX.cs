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
    // TODO: verify; does format support multiple GNF images?; cleanup...

    [MagicNumber("SEDBbtex", 0x00)]
    [MagicNumber("BTEX", 0x100)]
    public class BTEX : ImageFormat
    {
        public string MagicNumber1 { get; private set; }
        public uint Unknown0x08 { get; private set; }
        public uint Unknown0x0C { get; private set; }   // num images?
        public uint FileSize { get; private set; }
        public uint[] Unknown0x14 { get; private set; }

        public string MagicNumber2 { get; private set; }
        public uint SubHeaderSize { get; private set; }
        public uint GnfDataSize { get; private set; }
        public uint[] Unknown0x10C { get; private set; }
        public string ImageName { get; private set; }

        public GNF GnfInstance { get; private set; }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            MagicNumber1 = Encoding.ASCII.GetString(reader.ReadBytes(8), 0, 8);
            Unknown0x08 = reader.ReadUInt32();
            Unknown0x0C = reader.ReadUInt32();
            FileSize = reader.ReadUInt32();
            Unknown0x14 = new uint[0x3B];
            for (int i = 0; i < Unknown0x14.Length; i++) Unknown0x14[i] = reader.ReadUInt32();

            MagicNumber2 = Encoding.ASCII.GetString(reader.ReadBytes(4), 0, 4);
            SubHeaderSize = reader.ReadUInt32();
            GnfDataSize = reader.ReadUInt32();
            Unknown0x10C = new uint[0x13];
            for (int i = 0; i < Unknown0x10C.Length; i++) Unknown0x10C[i] = reader.ReadUInt32();
            ImageName = Encoding.ASCII.GetString(reader.ReadBytes(0xA8), 0, 0xA8).TrimEnd('\0');

            if (FileSize != reader.BaseStream.Length)
                throw new Exception("BTEX size mismatch");

            using (MemoryStream stream = new MemoryStream(reader.ReadBytes((int)GnfDataSize)))
            {
                GnfInstance = new GNF();
                GnfInstance.Open(stream);
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
            return GnfInstance?.GetBitmap(imageIndex, paletteIndex);
        }
    }
}
