using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

using Scarlet.Drawing;
using Scarlet.IO.Compression;

namespace Scarlet.IO.ImageFormats
{
	[MagicNumber("MSK3", 0x00)]
	public class MSK3 : ImageFormat
	{
		public string MagicNumber { get; private set; }
		public uint Unknown0x04 { get; private set; }
		public uint FileSize { get; private set; }
		public ushort Width { get; private set; }
		public ushort Height { get; private set; }
		public uint CompressedDataSize { get; private set; }
		public uint MaybePadding1 { get; private set; }     // always zero?
		public uint MaybePadding2 { get; private set; }     // ""
		public uint MaybePadding3 { get; private set; }     // ""

		public byte[] PixelData { get; private set; }

		protected override void OnOpen(EndianBinaryReader reader)
		{
			MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
			Unknown0x04 = reader.ReadUInt32();
			FileSize = reader.ReadUInt32();
			Width = reader.ReadUInt16();
			Height = reader.ReadUInt16();
			CompressedDataSize = reader.ReadUInt32();
			MaybePadding1 = reader.ReadUInt32();
			MaybePadding2 = reader.ReadUInt32();
			MaybePadding3 = reader.ReadUInt32();

			PixelData = EGLZ77.Decompress(reader.ReadBytes((int)CompressedDataSize), (Width * Height));
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
			ImageBinary imageBinary = new ImageBinary();

			imageBinary.Width = Width;
			imageBinary.Height = Height;
			imageBinary.InputEndianness = Endian.LittleEndian;

			imageBinary.InputPixelFormat = PixelDataFormat.FormatLuminance8;        // TODO: not sure if Alpha8 or Luminance8
			imageBinary.AddInputPixels(PixelData);

			return imageBinary.GetBitmap();
		}
	}
}
