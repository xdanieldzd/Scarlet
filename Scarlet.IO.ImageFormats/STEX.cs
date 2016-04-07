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
    [MagicNumber("STEX", 0x00)]
    [DefaultExtension(".stex")]
    public class STEX : ImageFormat
    {
        public string MagicNumber { get; private set; }
        public uint Unknown0x04 { get; private set; } /* TODO: sometimes one, mostly zero */
        public uint Constant3553 { get; private set; }
        public uint Width { get; private set; }
        public uint Height { get; private set; }
        public PicaDataType DataType { get; private set; }
        public PicaPixelFormat PixelFormat { get; private set; }
        public uint NumImageBytes { get; private set; } /* Unreliable! */

        /* Only in "xx80" STEXs */
        public uint ImageOffset { get; private set; }
        public uint Unknown0x24 { get; private set; }
        public string Name { get; private set; } /* Only in newer STEX? (ex. SMT4F) */

        public byte[] PixelData { get; private set; }

        ImageBinary imageBinary;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4), 0, 4);
            Unknown0x04 = reader.ReadUInt32();
            Constant3553 = reader.ReadUInt32();
            Width = reader.ReadUInt32();
            Height = reader.ReadUInt32();
            DataType = (PicaDataType)reader.ReadUInt32();
            PixelFormat = (PicaPixelFormat)reader.ReadUInt32();
            NumImageBytes = reader.ReadUInt32();

            /* Disclaimer: Hacky as hell! I almost want to hope that Atlus someday leaves their internal STEX creator tool inside one of these games, maybe that'll help with figuring this out <.< */

            /* ...now at offset 0x20, assume here's the pointer to image data */
            ImageOffset = reader.ReadUInt32();

            /* ...now assume said pointer is 0x80 */
            if (ImageOffset == 0x80)
            {
                /* Read "well-formed" STEX (but really, who knows how the header's supposed to be) */
                Unknown0x24 = reader.ReadUInt32();
                Name = Encoding.ASCII.GetString(reader.ReadBytes(0x58), 0, 0x58).TrimEnd('\0');

                /* ...but as image datasize is also unreliable, do some additional sanity checking on that, too! */
                reader.BaseStream.Seek(ImageOffset, SeekOrigin.Begin);
                PixelData = reader.ReadBytes((int)(NumImageBytes > reader.BaseStream.Length ? reader.BaseStream.Length - ImageOffset : NumImageBytes));
            }
            else /* ...otherwise... */
            {
                /* Seek back, then just assume image data starts right here at 0x20, and that the image is as many bytes long as are left in the file */
                reader.BaseStream.Seek(-4, SeekOrigin.Current);

                ImageOffset = (uint)reader.BaseStream.Position;
                Unknown0x24 = uint.MaxValue;
                Name = string.Empty;

                PixelData = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
            }

            /* Initialize ImageBinary */
            imageBinary = new ImageBinary();
            imageBinary.Width = (int)Width;
            imageBinary.Height = (int)Height;
            imageBinary.InputPixelFormat = N3DS.GetPixelDataFormat(DataType, PixelFormat);
            imageBinary.InputEndianness = Endian.LittleEndian;
            imageBinary.AddInputPixels(PixelData);
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
            return imageBinary.GetBitmap(imageIndex, paletteIndex);
        }
    }
}
