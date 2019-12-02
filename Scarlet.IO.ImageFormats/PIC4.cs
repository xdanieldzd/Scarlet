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
	// TODO: a better solution to the alpha mask nonsense?

	public enum PIC4ImageFormat
	{
		// TODO: any other possible values?
		IndexedWithAlphaMask = 0x0002,
		Indexed = 0x0003
	}

	[MagicNumber("PIC4", 0x00)]
	public class PIC4 : ImageFormat
	{
		public string MagicNumber { get; private set; }
		public uint Unknown0x04 { get; private set; }
		public uint FileSize { get; private set; }
		public ushort Unknown0x0C { get; private set; }     // similar/related to width? ex. 1/2 of width, but sometimes zero?
		public ushort Unknown0x0E { get; private set; }     // ""
		public ushort Width { get; private set; }
		public ushort Height { get; private set; }

		public uint Unknown0x14 { get; private set; }
		public uint NumRectangles { get; private set; }
		public uint MaybeChecksum { get; private set; }     // CRC32? over what range?

		public PIC4RectangleInfo[] RectangleInfos { get; private set; }

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
			NumRectangles = reader.ReadUInt32();
			MaybeChecksum = reader.ReadUInt32();

			RectangleInfos = new PIC4RectangleInfo[NumRectangles];
			for (int i = 0; i < RectangleInfos.Length; i++) RectangleInfos[i] = new PIC4RectangleInfo(reader);
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
			using (Bitmap destBitmap = new Bitmap(Width, Height))
			{
				using (Graphics g = Graphics.FromImage(destBitmap))
				{
					for (int i = 0; i < RectangleInfos.Length; i++)
					{
						PIC4RectangleInfo rectangleInfo = RectangleInfos[i];

						ImageBinary imageBinary = new ImageBinary();

						imageBinary.PhysicalWidth = ((rectangleInfo.ImageInfo.Width + 3) / 4) * 4;
						imageBinary.PhysicalHeight = rectangleInfo.ImageInfo.Height;
						imageBinary.Width = rectangleInfo.ImageInfo.Width;
						imageBinary.Height = rectangleInfo.ImageInfo.Height;
						imageBinary.InputEndianness = Endian.LittleEndian;

						switch (rectangleInfo.ImageInfo.ImageFormat)
						{
							case PIC4ImageFormat.Indexed:
								imageBinary.InputPixelFormat = PixelDataFormat.FormatIndexed8;
								imageBinary.InputPaletteFormat = PixelDataFormat.FormatArgb8888;
								imageBinary.AddInputPixels(rectangleInfo.ImageInfo.PixelData);
								imageBinary.AddInputPalette(rectangleInfo.ImageInfo.PaletteData);
								break;

							case PIC4ImageFormat.IndexedWithAlphaMask:
								byte[] processedPixelData = new byte[imageBinary.PhysicalWidth * imageBinary.PhysicalHeight * 4];
								for (int p = 0, s = 0; p < processedPixelData.Length; p += 4, s++)
								{
									int colorOffset = (rectangleInfo.ImageInfo.PixelData[s] * 4);
									processedPixelData[p + 0] = rectangleInfo.ImageInfo.PaletteData[colorOffset + 0];
									processedPixelData[p + 1] = rectangleInfo.ImageInfo.PaletteData[colorOffset + 1];
									processedPixelData[p + 2] = rectangleInfo.ImageInfo.PaletteData[colorOffset + 2];
									processedPixelData[p + 3] = rectangleInfo.ImageInfo.AlphaData[s];
								}
								imageBinary.InputPixelFormat = PixelDataFormat.FormatArgb8888;
								imageBinary.AddInputPixels(processedPixelData);
								break;
						}

						using (Bitmap srcBitmap = imageBinary.GetBitmap())
						{
							g.DrawImageUnscaled(srcBitmap,
								rectangleInfo.X + rectangleInfo.ImageInfo.X,
								rectangleInfo.Y + rectangleInfo.ImageInfo.Y);
						}

						if (false)
						{
							foreach (PIC4UnknownInnerRectangle data in rectangleInfo.ImageInfo.UnknownInnerRectangles)
								g.DrawRectangle(Pens.LawnGreen,
									rectangleInfo.X + rectangleInfo.ImageInfo.X + data.X1, rectangleInfo.Y + rectangleInfo.ImageInfo.Y + data.Y1,
									data.X2 - data.X1, data.Y2 - data.Y1);

							foreach (PIC4UnknownOuterRectangle data in rectangleInfo.ImageInfo.UnknownOuterRectangles)
								g.DrawRectangle(Pens.OrangeRed,
									rectangleInfo.X + rectangleInfo.ImageInfo.X + data.X1, rectangleInfo.Y + rectangleInfo.ImageInfo.Y + data.Y1,
									data.X2 - data.X1, data.Y2 - data.Y1);
						}
					}
				}

				return (Bitmap)destBitmap.Clone();
			}
		}
	}

	public class PIC4RectangleInfo
	{
		public ushort X { get; private set; }
		public ushort Y { get; private set; }
		public uint ImageInfoOffset { get; private set; }

		public PIC4ImageInfo ImageInfo { get; private set; }

		public PIC4RectangleInfo(EndianBinaryReader reader)
		{
			X = reader.ReadUInt16();
			Y = reader.ReadUInt16();
			ImageInfoOffset = reader.ReadUInt32();

			long lastPosition = reader.BaseStream.Position;
			reader.BaseStream.Position = ImageInfoOffset;
			ImageInfo = new PIC4ImageInfo(reader);
			reader.BaseStream.Position = lastPosition;
		}
	}

	public class PIC4ImageInfo
	{
		public PIC4ImageFormat ImageFormat { get; private set; }
		public ushort NumUnknownInnerRectangles { get; private set; }
		public ushort NumUnknownOuterRectangles { get; private set; }
		public ushort NumUnknownData3 { get; private set; }
		public ushort X { get; private set; }
		public ushort Y { get; private set; }
		public ushort Width { get; private set; }
		public ushort Height { get; private set; }
		public uint CompressedDataSize { get; private set; }

		public PIC4UnknownInnerRectangle[] UnknownInnerRectangles { get; private set; }        // TODO: Purpose? Covers "inner" parts of images, see ex. FILES.psarc\ADV\picture\EVCG17.pic
		public PIC4UnknownOuterRectangle[] UnknownOuterRectangles { get; private set; }         // TODO: Same, but covers "outer" parts of images
		public PIC4UnknownData3[] UnknownData3s { get; private set; }

		public byte[] PaletteData { get; private set; }
		public byte[] PixelData { get; private set; }
		public byte[] AlphaData { get; private set; }

		public PIC4ImageInfo(EndianBinaryReader reader)
		{
			ImageFormat = (PIC4ImageFormat)reader.ReadUInt16();
			NumUnknownInnerRectangles = reader.ReadUInt16();
			NumUnknownOuterRectangles = reader.ReadUInt16();
			NumUnknownData3 = reader.ReadUInt16();
			X = reader.ReadUInt16();
			Y = reader.ReadUInt16();
			Width = reader.ReadUInt16();
			Height = reader.ReadUInt16();
			CompressedDataSize = reader.ReadUInt32();

			if (!Enum.IsDefined(typeof(PIC4ImageFormat), ImageFormat)) throw new Exception("Unknown PIC4 image format");

			UnknownInnerRectangles = new PIC4UnknownInnerRectangle[NumUnknownInnerRectangles];
			for (int i = 0; i < UnknownInnerRectangles.Length; i++) UnknownInnerRectangles[i] = new PIC4UnknownInnerRectangle(reader);

			UnknownOuterRectangles = new PIC4UnknownOuterRectangle[NumUnknownOuterRectangles];
			for (int i = 0; i < UnknownOuterRectangles.Length; i++) UnknownOuterRectangles[i] = new PIC4UnknownOuterRectangle(reader);

			UnknownData3s = new PIC4UnknownData3[NumUnknownData3];
			for (int i = 0; i < UnknownData3s.Length; i++) UnknownData3s[i] = new PIC4UnknownData3(reader);

			int roundedWidth = ((Width + 3) / 4) * 4;

			int paletteSize = 0x400;
			int imageSize = (roundedWidth * Height);
			int alphaSize = (ImageFormat == PIC4ImageFormat.IndexedWithAlphaMask ? imageSize : 0);
			int decompressedSize = (paletteSize + imageSize + alphaSize);

			byte[] imageData;
			if (CompressedDataSize == 0x00)
				imageData = reader.ReadBytes(decompressedSize);
			else
				imageData = EGLZ77.Decompress(reader.ReadBytes((int)CompressedDataSize), decompressedSize);

			int copyPosition = 0;
			PaletteData = new byte[paletteSize];
			Buffer.BlockCopy(imageData, copyPosition, PaletteData, 0, PaletteData.Length);

			copyPosition += paletteSize;
			PixelData = new byte[imageSize];
			Buffer.BlockCopy(imageData, copyPosition, PixelData, 0, PixelData.Length);

			copyPosition += imageSize;
			AlphaData = new byte[alphaSize];
			Buffer.BlockCopy(imageData, copyPosition, AlphaData, 0, AlphaData.Length);

			if (false)
			{
				if (reader.BaseStream is FileStream fs)
				{
					if (fs.Name.EndsWith("00KAZ_C00.bup") && CompressedDataSize == 0x2E2C)
					{
						File.WriteAllBytes(@"D:\Temp\Konosuba Vita\____test____.dec", imageData);
					}
				}
			}
		}
	}

	public class PIC4UnknownInnerRectangle
	{
		public ushort X1 { get; private set; }
		public ushort Y1 { get; private set; }
		public ushort X2 { get; private set; }
		public ushort Y2 { get; private set; }

		public PIC4UnknownInnerRectangle(EndianBinaryReader reader)
		{
			X1 = reader.ReadUInt16();
			Y1 = reader.ReadUInt16();
			X2 = reader.ReadUInt16();
			Y2 = reader.ReadUInt16();
		}
	}

	public class PIC4UnknownOuterRectangle
	{
		public ushort X1 { get; private set; }
		public ushort Y1 { get; private set; }
		public ushort X2 { get; private set; }
		public ushort Y2 { get; private set; }

		public PIC4UnknownOuterRectangle(EndianBinaryReader reader)
		{
			X1 = reader.ReadUInt16();
			Y1 = reader.ReadUInt16();
			X2 = reader.ReadUInt16();
			Y2 = reader.ReadUInt16();
		}
	}

	public class PIC4UnknownData3
	{
		public ushort Unknown0x00 { get; private set; }

		public PIC4UnknownData3(EndianBinaryReader reader)
		{
			Unknown0x00 = reader.ReadUInt16();
		}
	}
}
