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
	// TODO: Disgaea 1 Switch, ex. SUBDATA\nsaveicon, SUBDATA\save(001:018); TX2s w/o palette, unknown/compressed pixel format, etc...

	[FilenamePattern("^.*\\.tx2$")]
	public class TX2 : ImageFormat
	{
		public ushort ImageWidth { get; private set; }
		public ushort ImageHeight { get; private set; }
		public ushort ColorCount { get; private set; }
		public ushort Unknown0x06 { get; private set; }
		public ushort PaletteWidth { get; private set; }
		public ushort PaletteHeight { get; private set; }
		public uint Padding { get; private set; }

		public byte[][] PaletteData { get; private set; }
		public byte[] PixelData { get; private set; }

		ImageBinary imageBinary;

		protected override void OnOpen(EndianBinaryReader reader)
		{
			ImageWidth = reader.ReadUInt16();
			ImageHeight = reader.ReadUInt16();
			ColorCount = reader.ReadUInt16();
			Unknown0x06 = reader.ReadUInt16();
			PaletteWidth = reader.ReadUInt16();
			PaletteHeight = reader.ReadUInt16();
			Padding = reader.ReadUInt32();

			PaletteData = new byte[PaletteHeight][];
			if (PaletteHeight > 0)
			{
				for (int py = 0; py < PaletteHeight; py++)
					PaletteData[py] = PS2.ReadPaletteData(reader, (ColorCount == 256 ? PS2PixelFormat.PSMT8 : PS2PixelFormat.PSMT4), PS2PixelFormat.PSMCT32);
			}
			else
				throw new Exception("Cannot convert TX2 without palette data!");

			PixelData = reader.ReadBytes((ImageWidth * ImageHeight) / (ColorCount == 256 ? 1 : 2));

			/* Initialize ImageBinary */
			imageBinary = new ImageBinary();
			imageBinary.Width = ImageWidth;
			imageBinary.Height = ImageHeight;
			imageBinary.InputPaletteFormat = PixelDataFormat.FormatBgra8888;
			imageBinary.InputPixelFormat = (ColorCount == 256 ? PixelDataFormat.FormatIndexed8 : PixelDataFormat.FormatIndexed4);
			imageBinary.InputEndianness = Endian.LittleEndian;

			foreach (byte[] palette in PaletteData) imageBinary.AddInputPalette(palette);
			imageBinary.AddInputPixels(PixelData);
		}

		public override int GetImageCount()
		{
			return 1;
		}

		public override int GetPaletteCount()
		{
			return PaletteData.Length;
		}

		protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
		{
			return imageBinary.GetBitmap(imageIndex, paletteIndex);
		}
	}
}
