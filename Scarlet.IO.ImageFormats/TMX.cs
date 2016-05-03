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
    /* Better palette conversion based on information from... */
    /* http://forums.pcsx2.net/Thread-TMX-file-format-in-Persona-3-4 */
    /* http://forum.xentax.com/viewtopic.php?f=18&t=2922&start=0 */

    [MagicNumber("TMX0", 0x08)]
    [FilenamePattern("^.*\\.tmx")]
    public class TMX : ImageFormat
    {
        /* TODO: clean up, move any common code to Platform.Sony.PS2, etc. */

        public ushort Unknown0x00 { get; private set; }
        public ushort ID { get; private set; }
        public uint FileSize { get; private set; }
        public string MagicNumber { get; private set; }
        public uint Unknown0x0C { get; private set; }
        public byte Unknown0x10 { get; private set; }
        public PS2PixelFormat PaletteFormat { get; private set; }
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }
        public PS2PixelFormat PixelFormat { get; private set; }
        public byte MipmapCount { get; private set; }
        public byte MipmapKValue { get; private set; }
        public byte MipmapLValue { get; private set; }
        public ushort Unknown4 { get; private set; }
        public PS2WrapMode TextureWrap { get; private set; }
        public uint TextureID { get; private set; }
        public uint CLUTID { get; private set; }
        public string Comment { get; private set; }

        public byte[] PaletteData { get; private set; }
        public byte[] PixelData { get; private set; }

        ImageBinary imageBinary;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            Unknown0x00 = reader.ReadUInt16();
            ID = reader.ReadUInt16();
            FileSize = reader.ReadUInt32();
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4), 0, 4);
            Unknown0x0C = reader.ReadUInt32();
            Unknown0x10 = reader.ReadByte();
            PaletteFormat = (PS2PixelFormat)reader.ReadByte();
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            PixelFormat = (PS2PixelFormat)reader.ReadByte();
            MipmapCount = reader.ReadByte();
            MipmapKValue = reader.ReadByte();
            MipmapLValue = reader.ReadByte();
            TextureWrap = (PS2WrapMode)reader.ReadUInt16();
            TextureID = reader.ReadUInt32();
            CLUTID = reader.ReadUInt32();
            Comment = Encoding.ASCII.GetString(reader.ReadBytes(0x1C), 0, 0x1C);

            if (PS2.IsFormatIndexed(PixelFormat))
                PaletteData = PS2.ReadPaletteData(reader, PixelFormat, PaletteFormat);

            PixelData = reader.ReadBytes(CalculatePixelDataSize(Width, Height, PixelFormat));

            imageBinary = new ImageBinary();
            imageBinary.Width = (int)Width;
            imageBinary.Height = (int)Height;
            imageBinary.InputPaletteFormat = PS2.GetPixelDataFormat(PaletteFormat);
            imageBinary.InputPixelFormat = PS2.GetPixelDataFormat(PixelFormat);
            imageBinary.InputEndianness = Endian.LittleEndian;

            imageBinary.AddInputPalette(PaletteData);
            imageBinary.AddInputPixels(PixelData);
        }

        private int CalculatePixelDataSize(int width, int height, PS2PixelFormat format)
        {
            switch (format)
            {
                case PS2PixelFormat.PSMCT32: return ((width * height) * 4);
                case PS2PixelFormat.PSMCT24: return ((width * height) * 3);
                case PS2PixelFormat.PSMCT16: return ((width * height) * 2);
                case PS2PixelFormat.PSMT8: return (width * height);
                case PS2PixelFormat.PSMT4: return ((width * height) / 2);
                default: throw new Exception("Unknown PS2 pixel format");
            }
        }

        public override int GetImageCount()
        {
            return 1;
        }

        public override int GetPaletteCount()
        {
            return 1;
        }

        protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
        {
            return imageBinary.GetBitmap(imageIndex, paletteIndex);
        }
    }
}
