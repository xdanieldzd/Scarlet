//#define DUMPFACEPARTS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

using Scarlet.Drawing;

namespace Scarlet.IO.ImageFormats
{
	// TODO: partially shared with PIC4, ex. main header?

	[MagicNumber("BUP4", 0x00)]
	public class BUP4 : ImageFormat
	{
		public string MagicNumber { get; private set; }
		public uint Unknown0x04 { get; private set; }
		public uint FileSize { get; private set; }
		public ushort Unknown0x0C { get; private set; }
		public ushort Unknown0x0E { get; private set; }
		public ushort Width { get; private set; }
		public ushort Height { get; private set; }

		public uint Unknown0x14 { get; private set; }
		public uint NumBaseImages { get; private set; }
		public uint NumFacePartImages { get; private set; }
		public uint Unknonw0x20 { get; private set; }
		public uint MaybeChecksum { get; private set; }

		public uint MaybePadding1 { get; private set; }     // always zero?
		public uint MaybePadding2 { get; private set; }     // ""
		public BUP4BaseImage[] BaseImages { get; private set; }
		public uint MaybePadding3 { get; private set; }     // always zero?
		public uint MaybePadding4 { get; private set; }     // ""

		public BUP4FacePartImages[] FacePartImages { get; private set; }

#if DUMPFACEPARTS
		public List<PIC4ImageInfo> FacePartsTemp { get; private set; }
#endif

		protected override void OnOpen(EndianBinaryReader reader)
		{
			MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
			Unknown0x04 = reader.ReadUInt32();
			FileSize = reader.ReadUInt32();
			Unknown0x0C = reader.ReadUInt16();
			Unknown0x0E = reader.ReadUInt16();
			Width = reader.ReadUInt16();
			Height = reader.ReadUInt16();

			Unknown0x14 = reader.ReadUInt32();
			NumBaseImages = reader.ReadUInt32();
			NumFacePartImages = reader.ReadUInt32();
			Unknonw0x20 = reader.ReadUInt32();
			MaybeChecksum = reader.ReadUInt32();

			MaybePadding1 = reader.ReadUInt32();
			MaybePadding2 = reader.ReadUInt32();
			BaseImages = new BUP4BaseImage[NumBaseImages];
			for (int i = 0; i < BaseImages.Length; i++) BaseImages[i] = new BUP4BaseImage(reader);
			MaybePadding3 = reader.ReadUInt32();
			MaybePadding4 = reader.ReadUInt32();

			FacePartImages = new BUP4FacePartImages[NumFacePartImages];
			for (int i = 0; i < FacePartImages.Length; i++) FacePartImages[i] = new BUP4FacePartImages(reader);

#if DUMPFACEPARTS
			FacePartsTemp = new List<PIC4ImageInfo>();
			foreach (BUP4FacePartImages data in FacePartImages)
				FacePartsTemp.AddRange(data.ImageInfos);
#endif
		}

		public override int GetImageCount()
		{
#if !DUMPFACEPARTS
			return 1;
#else
			return FacePartsTemp.Count;
#endif
		}

		public override int GetPaletteCount()
		{
			return 0;
		}

		protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
		{
#if !DUMPFACEPARTS
			using (Bitmap destBitmap = new Bitmap(Width, Height))
			{
				using (Graphics g = Graphics.FromImage(destBitmap))
				{
					foreach (PIC4ImageInfo imageInfo in BaseImages.Select(x => x.ImageInfo))
					{
						ImageBinary imageBinary = new ImageBinary();

						imageBinary.PhysicalWidth = ((imageInfo.Width + 3) / 4) * 4;
						imageBinary.PhysicalHeight = imageInfo.Height;
						imageBinary.Width = imageInfo.Width;
						imageBinary.Height = imageInfo.Height;

						imageBinary.InputPaletteFormat = PixelDataFormat.FormatArgb8888;
						imageBinary.InputPixelFormat = PixelDataFormat.FormatIndexed8;
						imageBinary.InputEndianness = Endian.LittleEndian;

						imageBinary.AddInputPalette(imageInfo.PaletteData);
						imageBinary.AddInputPixels(imageInfo.PixelData);

						using (Bitmap srcBitmap = imageBinary.GetBitmap())
						{
							g.DrawImageUnscaled(srcBitmap, imageInfo.X, imageInfo.Y);
						}
					}
				}

				return (Bitmap)destBitmap.Clone();
			}
#else
			PIC4ImageInfo imageInfo = FacePartsTemp[imageIndex];

			if (imageInfo != null)
			{
				ImageBinary imageBinary = new ImageBinary();

				imageBinary.PhysicalWidth = ((imageInfo.Width + 3) / 4) * 4;
				imageBinary.PhysicalHeight = imageInfo.Height;
				imageBinary.Width = imageInfo.Width;
				imageBinary.Height = imageInfo.Height;

				imageBinary.InputPaletteFormat = PixelDataFormat.FormatArgb8888;
				imageBinary.InputPixelFormat = PixelDataFormat.FormatIndexed8;
				imageBinary.InputEndianness = Endian.LittleEndian;

				imageBinary.AddInputPalette(imageInfo.PaletteData);
				imageBinary.AddInputPixels(imageInfo.PixelData);

				return imageBinary.GetBitmap();
			}
			else
				return new Bitmap(32, 32);
#endif
		}
	}

	public class BUP4BaseImage
	{
		public uint ImageInfoOffset { get; private set; }

		public PIC4ImageInfo ImageInfo { get; private set; }

		public BUP4BaseImage(EndianBinaryReader reader)
		{
			ImageInfoOffset = reader.ReadUInt32();

			long lastPosition = reader.BaseStream.Position;
			reader.BaseStream.Position = ImageInfoOffset;
			ImageInfo = new PIC4ImageInfo(reader);
			reader.BaseStream.Position = lastPosition;
		}
	}

	public class BUP4FacePartImages
	{
		// #0-3 == faces; if not null, always #1?
		// #4-7 == mouths; if not null, always #5-7?

		public string Name { get; private set; }
		public uint[] ImageInfoOffsets { get; private set; }

		public PIC4ImageInfo[] ImageInfos { get; private set; }

		public BUP4FacePartImages(EndianBinaryReader reader)
		{
			Name = Encoding.ASCII.GetString(reader.ReadBytes(0x10));
			ImageInfoOffsets = new uint[8];
			for (int i = 0; i < ImageInfoOffsets.Length; i++) ImageInfoOffsets[i] = reader.ReadUInt32();

			ImageInfos = new PIC4ImageInfo[8];
			for (int i = 0; i < ImageInfoOffsets.Length; i++)
			{
				if (ImageInfoOffsets[i] == 0x00) continue;

				long lastPosition = reader.BaseStream.Position;
				reader.BaseStream.Position = ImageInfoOffsets[i];
				ImageInfos[i] = new PIC4ImageInfo(reader);
				reader.BaseStream.Position = lastPosition;
			}
		}
	}
}
