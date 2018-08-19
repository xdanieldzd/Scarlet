using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

using Scarlet.Drawing;

namespace Scarlet.IO.ImageFormats
{
	// Capcom TEX (Monster Hunter World)

	// TODO: better disambiguation, if possible - Capcom's multiple, incompatible, indiscernible, TEX formats suck!

	[MagicNumber(new byte[] { 0x54, 0x45, 0x58, 0x00, 0x10, 0x00, 0x00, 0x00 }, 0x00)]
	public class CapcomTEXMHW : ImageFormat
	{
		public string MagicNumber { get; private set; }
		public uint Unknown0x04 { get; private set; }       // revision? offset of header? using it as part of magic number because Capcom!
		public uint Unknown0x08 { get; private set; }       // padding?
		public uint Unknown0x0C { get; private set; }       // padding?

		public uint Unknown0x10 { get; private set; }       // count of something? or always 0x02?
		public uint NumMipmaps { get; private set; }
		public uint BaseWidth { get; private set; }
		public uint BaseHeight { get; private set; }

		public uint MaybePixelOrderingMode { get; private set; }    // pixel ordering? 0x01 == swizzled, 0x06 == linear?
		public uint PixelFormat { get; private set; }
		public uint Unknown0x28 { get; private set; }       // count or always 0x01?
		public uint Unknown0x2C { get; private set; }       // zero?

		public uint Unknown0x30 { get; private set; }       // zero?
		public uint Unknown0x34 { get; private set; }       // zero?
		public uint Unknown0x38 { get; private set; }       // count or always 0x0D?
		public uint Unknown0x3C { get; private set; }       // zero?

		public uint Unknown0x40 { get; private set; }       // zero?
		public uint Unknown0x44 { get; private set; }       // zero?
		public uint Unknown0x48 { get; private set; }       // zero?
		public uint Unknown0x4C { get; private set; }       // zero?

		public int Unknown0x50 { get; private set; }        // -1?
		public int Unknown0x54 { get; private set; }        // -1?
		public int Unknown0x58 { get; private set; }        // -1?
		public int Unknown0x5C { get; private set; }        // -1?

		public int Unknown0x60 { get; private set; }        // -1?
		public int Unknown0x64 { get; private set; }        // -1?
		public int Unknown0x68 { get; private set; }        // -1?
		public int Unknown0x6C { get; private set; }        // -1?

		public int Unknown0x70 { get; private set; }        // -1?
		public int Unknown0x74 { get; private set; }        // -1?
		public uint Unknown0x78 { get; private set; }       // same as 0x18/width?
		public ushort Unknown0x7C { get; private set; }     // same as 0x18/width?
		public ushort Unknown0x7E { get; private set; }     // same as 0x18/width?

		public uint Unknown0x80 { get; private set; }       // zero?
		public uint Unknown0x84 { get; private set; }       // zero?
		public ushort Unknown0x88 { get; private set; }     // same as 0x18/width?
		public ushort Unknown0x8A { get; private set; }     // same as 0x18/width?
		public uint Unknown0x8C { get; private set; }       // zero?

		public uint Unknown0x90 { get; private set; }       // zero?
		public ushort Unknown0x94 { get; private set; }     // same as 0x18/width?
		public ushort Unknown0x96 { get; private set; }     // same as 0x18/width?
		public uint Unknown0x98 { get; private set; }       // zero?
		public uint Unknown0x9C { get; private set; }       // zero?

		public uint Unknown0xA0 { get; private set; }       // zero?
		public uint Unknown0xA4 { get; private set; }       // zero?
		public uint Unknown0xA8 { get; private set; }       // zero?
		public uint Unknown0xAC { get; private set; }       // zero?

		public uint Unknown0xB0 { get; private set; }       // zero?
		public uint Unknown0xB4 { get; private set; }       // zero?

		public Tuple<uint, uint>[] MipmapInfos { get; private set; }

		PixelDataFormat pixelDataFormat = PixelDataFormat.Undefined;
		List<MipmapLevel> mipmapData;

		protected override void OnOpen(EndianBinaryReader reader)
		{
			MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
			Unknown0x04 = reader.ReadUInt32();
			Unknown0x08 = reader.ReadUInt32();
			Unknown0x0C = reader.ReadUInt32();

			Unknown0x10 = reader.ReadUInt32();
			NumMipmaps = reader.ReadUInt32();
			BaseWidth = reader.ReadUInt32();
			BaseHeight = reader.ReadUInt32();

			MaybePixelOrderingMode = reader.ReadUInt32();
			PixelFormat = reader.ReadUInt32();
			Unknown0x28 = reader.ReadUInt32();
			Unknown0x2C = reader.ReadUInt32();

			Unknown0x30 = reader.ReadUInt32();
			Unknown0x34 = reader.ReadUInt32();
			Unknown0x38 = reader.ReadUInt32();
			Unknown0x3C = reader.ReadUInt32();

			Unknown0x40 = reader.ReadUInt32();
			Unknown0x44 = reader.ReadUInt32();
			Unknown0x48 = reader.ReadUInt32();
			Unknown0x4C = reader.ReadUInt32();

			Unknown0x50 = reader.ReadInt32();
			Unknown0x54 = reader.ReadInt32();
			Unknown0x58 = reader.ReadInt32();
			Unknown0x5C = reader.ReadInt32();

			Unknown0x60 = reader.ReadInt32();
			Unknown0x64 = reader.ReadInt32();
			Unknown0x68 = reader.ReadInt32();
			Unknown0x6C = reader.ReadInt32();

			Unknown0x70 = reader.ReadInt32();
			Unknown0x74 = reader.ReadInt32();
			Unknown0x78 = reader.ReadUInt32();
			Unknown0x7C = reader.ReadUInt16();
			Unknown0x7E = reader.ReadUInt16();

			Unknown0x80 = reader.ReadUInt32();
			Unknown0x84 = reader.ReadUInt32();
			Unknown0x88 = reader.ReadUInt16();
			Unknown0x8A = reader.ReadUInt16();
			Unknown0x8C = reader.ReadUInt32();

			Unknown0x90 = reader.ReadUInt32();
			Unknown0x94 = reader.ReadUInt16();
			Unknown0x96 = reader.ReadUInt16();
			Unknown0x98 = reader.ReadUInt32();
			Unknown0x9C = reader.ReadUInt32();

			Unknown0xA0 = reader.ReadUInt32();
			Unknown0xA4 = reader.ReadUInt32();
			Unknown0xA8 = reader.ReadUInt32();
			Unknown0xAC = reader.ReadUInt32();

			Unknown0xB0 = reader.ReadUInt32();
			Unknown0xB4 = reader.ReadUInt32();

			MipmapInfos = new Tuple<uint, uint>[NumMipmaps];
			for (int i = 0; i < NumMipmaps; i++)
				MipmapInfos[i] = new Tuple<uint, uint>(reader.ReadUInt32(), reader.ReadUInt32());

			switch (PixelFormat)
			{
				case 0x07: pixelDataFormat = PixelDataFormat.FormatRgba8888; break;
				case 0x16: pixelDataFormat = PixelDataFormat.FormatDXT1Rgb; break;
				case 0x17: pixelDataFormat = PixelDataFormat.FormatDXT1Rgba; break;
				case 0x18: pixelDataFormat = PixelDataFormat.FormatRGTC1; break;
				case 0x1A: pixelDataFormat = PixelDataFormat.FormatRGTC2; break;
				case 0x1C: pixelDataFormat = PixelDataFormat.FormatBPTC_Float; break;
				case 0x1E: pixelDataFormat = PixelDataFormat.FormatBPTC; break;
				case 0x1F: pixelDataFormat = PixelDataFormat.FormatBPTC; break;

				default: throw new Exception($"MHW TEX format 0x{PixelFormat:X}");
			}

			switch (MaybePixelOrderingMode)
			{
				case 0x01: pixelDataFormat |= PixelDataFormat.PixelOrderingTiled3DS; break;
				case 0x06: pixelDataFormat |= PixelDataFormat.PixelOrderingLinear; break;

				default: throw new Exception($"MHW TEX pixel order 0x{MaybePixelOrderingMode:X}");
			}

			mipmapData = new List<MipmapLevel>();
			for (int i = 0; i < NumMipmaps; i++)
			{
				reader.BaseStream.Seek(MipmapInfos[i].Item1, SeekOrigin.Begin);

				int mipWidth = (int)Math.Max(1, (BaseWidth >> i));
				int mipHeight = (int)Math.Max(1, (BaseHeight >> i));
				int mipDataSize = (int)((i < NumMipmaps - 1) ? (MipmapInfos[i + 1].Item1 - MipmapInfos[i].Item1) : (reader.BaseStream.Length - MipmapInfos[i].Item1));
				byte[] mipPixelData = reader.ReadBytes(mipDataSize);

				mipmapData.Add(new MipmapLevel(mipWidth, mipHeight, mipPixelData));
			}
		}

		public override int GetImageCount()
		{
			return (int)NumMipmaps;
		}

		public override int GetPaletteCount()
		{
			return 0;
		}

		protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
		{
			ImageBinary imageBinary = new ImageBinary
			{
				Width = mipmapData[imageIndex].Width,
				Height = mipmapData[imageIndex].Height,
				InputPixelFormat = pixelDataFormat
			};
			imageBinary.AddInputPixels(mipmapData[imageIndex].PixelData);

			return imageBinary.GetBitmap(0, 0);
		}
	}
}
