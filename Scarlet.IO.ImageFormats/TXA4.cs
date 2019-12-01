using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using Scarlet.Drawing;
using Scarlet.IO.Compression;

namespace Scarlet.IO.ImageFormats
{
	[MagicNumber("TXA4", 0x00)]
	public class TXA4 : ImageFormat
	{
		public string MagicNumber { get; private set; }
		public uint Unknown0x04 { get; private set; }
		public uint FileSize { get; private set; }
		public uint Unknown0x0C { get; private set; }
		public uint NumTextures { get; private set; }
		public uint Unknown0x14 { get; private set; }
		public uint Unknown0x18 { get; private set; }
		public uint Unknown0x1C { get; private set; }

		public TXA4TextureInfo[] TextureInfos { get; private set; }

		protected override void OnOpen(EndianBinaryReader reader)
		{
			MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
			Unknown0x04 = reader.ReadUInt32();
			FileSize = reader.ReadUInt32();
			Unknown0x0C = reader.ReadUInt32();
			NumTextures = reader.ReadUInt32();
			Unknown0x14 = reader.ReadUInt32();
			Unknown0x18 = reader.ReadUInt32();
			Unknown0x1C = reader.ReadUInt32();

			TextureInfos = new TXA4TextureInfo[NumTextures];
			for (int i = 0; i < TextureInfos.Length; i++) TextureInfos[i] = new TXA4TextureInfo(reader);
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
			TXA4TextureInfo textureInfo = TextureInfos[imageIndex];

			ImageBinary imageBinary = new ImageBinary();

			imageBinary.Width = textureInfo.Width;
			imageBinary.Height = textureInfo.Height;

			imageBinary.InputPaletteFormat = PixelDataFormat.FormatArgb8888;
			imageBinary.InputPixelFormat = PixelDataFormat.FormatIndexed8;
			imageBinary.InputEndianness = Endian.LittleEndian;

			imageBinary.AddInputPalette(textureInfo.PaletteData);
			imageBinary.AddInputPixels(textureInfo.PixelData);

			return imageBinary.GetBitmap(0, 0);
		}
	}

	public class TXA4TextureInfo
	{
		public ushort InfoSize { get; private set; }
		public ushort ID { get; private set; }
		public ushort Width { get; private set; }
		public ushort Height { get; private set; }
		public uint DataOffset { get; private set; }
		public uint DataSize { get; private set; }
		public string Name { get; private set; }

		public byte[] PaletteData { get; private set; }
		public byte[] PixelData { get; private set; }

		public TXA4TextureInfo(EndianBinaryReader reader)
		{
			InfoSize = reader.ReadUInt16();
			ID = reader.ReadUInt16();
			Width = reader.ReadUInt16();
			Height = reader.ReadUInt16();
			DataOffset = reader.ReadUInt32();
			DataSize = reader.ReadUInt32();
			Name = Encoding.ASCII.GetString(reader.ReadBytes(InfoSize - 0x10)).TrimEnd('\0');

			long lastPosition = reader.BaseStream.Position;
			reader.BaseStream.Position = DataOffset;

			byte[] imageData = EGLZ77.Decompress(reader.ReadBytes((int)DataSize), 0x400 + (Width * Height));

			PaletteData = new byte[0x400];
			Buffer.BlockCopy(imageData, 0, PaletteData, 0, PaletteData.Length);
			PixelData = new byte[imageData.Length - PaletteData.Length];
			Buffer.BlockCopy(imageData, PaletteData.Length, PixelData, 0, PixelData.Length);

			reader.BaseStream.Position = lastPosition;
		}
	}
}
