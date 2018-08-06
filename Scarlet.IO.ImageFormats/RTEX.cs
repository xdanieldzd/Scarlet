using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.IO.Compression;

using Scarlet.Drawing;
using Scarlet.IO;

namespace Scarlet.IO.ImageFormats
{
	// TODO: split apart better, i.e. main header & "RZ" header to separate classes? ensure format & tiling are correct

	[MagicNumber("RTEX", 0x00)]
	public class RTEX : ImageFormat
	{
		// Main "RTEX" header?

		public string MagicNumber { get; private set; }     // "RTEX"
		public uint Unknown0x04 { get; private set; }       // Always zero?
		public ushort VirtualWidth { get; private set; }
		public ushort VirtualHeight { get; private set; }
		public ushort PhysicalWidth { get; private set; }
		public ushort PhysicalHeight { get; private set; }

		public uint ImageDataOffset { get; private set; }
		public uint ImageDataSize { get; private set; }
		public ushort MaybeFormat { get; private set; }
		public ushort MaybeTilingMode { get; private set; }
		public uint MaybeStride { get; private set; }

		// Compressed data "RZ" header?

		public string CompressedImageDataMagicNumber { get; private set; }  // "RZ"
		public ushort Unknown0x22 { get; private set; }
		public ushort Unknown0x24 { get; private set; }
		public ushort ZLibHeader { get; private set; }

		public byte[] PixelData { get; private set; }

		PixelDataFormat pixelDataFormat = PixelDataFormat.Undefined;

		protected override void OnOpen(EndianBinaryReader reader)
		{
			MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4), 0, 4);
			Unknown0x04 = reader.ReadUInt32();
			VirtualWidth = reader.ReadUInt16();
			VirtualHeight = reader.ReadUInt16();
			PhysicalWidth = reader.ReadUInt16();
			PhysicalHeight = reader.ReadUInt16();

			ImageDataOffset = reader.ReadUInt32();
			ImageDataSize = reader.ReadUInt32();
			MaybeFormat = reader.ReadUInt16();
			MaybeTilingMode = reader.ReadUInt16();
			MaybeStride = reader.ReadUInt32();

			CompressedImageDataMagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(2), 0, 2);
			Unknown0x22 = reader.ReadUInt16();
			Unknown0x24 = reader.ReadUInt16();
			ZLibHeader = reader.ReadUInt16();

			if (CompressedImageDataMagicNumber == "RZ")
			{
				reader.BaseStream.Seek(ImageDataOffset + 0x08, SeekOrigin.Begin);

				using (DeflateStream deflateStream = new DeflateStream(reader.BaseStream, CompressionMode.Decompress))
				{
					using (MemoryStream memoryStream = new MemoryStream())
					{
						deflateStream.CopyTo(memoryStream);
						PixelData = memoryStream.ToArray();
					}
				}
			}
			else
			{
				reader.BaseStream.Seek(ImageDataOffset, SeekOrigin.Begin);

				PixelData = reader.ReadBytes((int)ImageDataSize);
			}

			switch (MaybeFormat)
			{
				case 0x0002: pixelDataFormat = PixelDataFormat.FormatAbgr8888; break;
				case 0x000D: pixelDataFormat = PixelDataFormat.FormatBgra4444; break;
				case 0x0014: pixelDataFormat = PixelDataFormat.FormatBgr888; break;
				case 0x0033: pixelDataFormat = PixelDataFormat.FormatLuminanceAlpha88; break;
				case 0x0034: pixelDataFormat = PixelDataFormat.FormatLuminanceAlpha44; break;

				case 0x1014: pixelDataFormat = PixelDataFormat.FormatDXT1Rgba; break;   // TODO: RGB or RGBA?
				case 0x4002: pixelDataFormat = PixelDataFormat.FormatDXT5; break;

				default: throw new Exception($"Unknown pixel format 0x{MaybeFormat:X4}");
			}

			switch (MaybeTilingMode)
			{
				case 0x0000: /* nothing */ break;
				case 0x0008: break; // ??
				case 0x0020: pixelDataFormat |= PixelDataFormat.PixelOrderingTiled3DS; break;
				default: throw new Exception($"Unknown tiling mode 0x{MaybeTilingMode:X4}");
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
			ImageBinary imageBinary = new ImageBinary
			{
				PhysicalWidth = PhysicalWidth,
				PhysicalHeight = PhysicalHeight,
				Width = VirtualWidth,
				Height = VirtualHeight,
				InputPixelFormat = pixelDataFormat,
				InputEndianness = Endian.LittleEndian
			};
			imageBinary.AddInputPixels(PixelData);

			return imageBinary.GetBitmap(imageIndex, paletteIndex);
		}
	}
}
