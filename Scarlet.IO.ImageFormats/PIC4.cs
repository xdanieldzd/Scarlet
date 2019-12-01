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
		public uint Unknown0x1C { get; private set; }

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
			Unknown0x1C = reader.ReadUInt32();

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

						imageBinary.InputPaletteFormat = PixelDataFormat.FormatArgb8888;
						imageBinary.InputPixelFormat = PixelDataFormat.FormatIndexed8;
						imageBinary.InputEndianness = Endian.LittleEndian;

						imageBinary.AddInputPalette(rectangleInfo.ImageInfo.PaletteData);
						imageBinary.AddInputPixels(rectangleInfo.ImageInfo.PixelData);

						using (Bitmap srcBitmap = imageBinary.GetBitmap())
						{
							g.DrawImageUnscaled(srcBitmap, rectangleInfo.X, rectangleInfo.Y);
						}

						if (false)
						{
							foreach (PIC4UnknownInnerRectangles data in rectangleInfo.ImageInfo.UnknownInnerRectangles)
								g.DrawRectangle(Pens.LawnGreen,
									rectangleInfo.X + data.X1, rectangleInfo.Y + data.Y1,
									data.X2 - data.X1, data.Y2 - data.Y1);

							foreach (PIC4UnknownOuterRectangle data in rectangleInfo.ImageInfo.UnknownOuterRectangles)
								g.DrawRectangle(Pens.OrangeRed,
									rectangleInfo.X + data.X1, rectangleInfo.Y + data.Y1,
									data.X2 - data.X1 + 1, data.Y2 - data.Y1 + 1);
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
		public ushort Unknown0x00 { get; private set; }                                         // 0x03 on regular images, 0x02 on images w/ alpha issues & inner/outer rects (see below)?
		public ushort NumUnknownInnerRectangles { get; private set; }
		public ushort NumUnknownOuterRectangles { get; private set; }
		public ushort NumUnknownData3 { get; private set; }
		public ushort Unknown0x08 { get; private set; }
		public ushort Unknown0x0A { get; private set; }
		public ushort Width { get; private set; }
		public ushort Height { get; private set; }
		public uint CompressedDataSize { get; private set; }

		public PIC4UnknownInnerRectangles[] UnknownInnerRectangles { get; private set; }        // TODO: Purpose? Covers "inner" parts of images, see ex. FILES.psarc\ADV\picture\EVCG17.pic
		public PIC4UnknownOuterRectangle[] UnknownOuterRectangles { get; private set; }         // TODO: Same, but covers "outer" parts of images
		public PIC4UnknownData3[] UnknownData3s { get; private set; }

		public byte[] PaletteData { get; private set; }
		public byte[] PixelData { get; private set; }

		public PIC4ImageInfo(EndianBinaryReader reader)
		{
			Unknown0x00 = reader.ReadUInt16();
			NumUnknownInnerRectangles = reader.ReadUInt16();
			NumUnknownOuterRectangles = reader.ReadUInt16();
			NumUnknownData3 = reader.ReadUInt16();
			Unknown0x08 = reader.ReadUInt16();
			Unknown0x0A = reader.ReadUInt16();
			Width = reader.ReadUInt16();
			Height = reader.ReadUInt16();
			CompressedDataSize = reader.ReadUInt32();

			UnknownInnerRectangles = new PIC4UnknownInnerRectangles[NumUnknownInnerRectangles];
			for (int i = 0; i < UnknownInnerRectangles.Length; i++) UnknownInnerRectangles[i] = new PIC4UnknownInnerRectangles(reader);

			UnknownOuterRectangles = new PIC4UnknownOuterRectangle[NumUnknownOuterRectangles];
			for (int i = 0; i < UnknownOuterRectangles.Length; i++) UnknownOuterRectangles[i] = new PIC4UnknownOuterRectangle(reader);

			UnknownData3s = new PIC4UnknownData3[NumUnknownData3];
			for (int i = 0; i < UnknownData3s.Length; i++) UnknownData3s[i] = new PIC4UnknownData3(reader);

			int roundedWidth = ((Width + 3) / 4) * 4;
			int decompressedSize = 0x400 + ((roundedWidth * Height));

			byte[] imageData;
			if (CompressedDataSize == 0x00)
				imageData = reader.ReadBytes(decompressedSize);
			else
				imageData = EGLZ77.Decompress(reader.ReadBytes((int)CompressedDataSize), decompressedSize);

			PaletteData = new byte[0x400];
			Buffer.BlockCopy(imageData, 0, PaletteData, 0, PaletteData.Length);
			PixelData = new byte[imageData.Length - PaletteData.Length];
			Buffer.BlockCopy(imageData, PaletteData.Length, PixelData, 0, PixelData.Length);

			if (false)
			{
				if (reader.BaseStream is FileStream fs)
				{
					if (fs.Name.EndsWith("ITEM12a.pic") && CompressedDataSize == 0x465C)
					{
						//File.WriteAllBytes(@"D:\Temp\Konosuba Vita\____test____.dec", imageData);
					}

					if (fs.Name.EndsWith("EVCG17.pic"))
					{
						for (int i = 0; i < 256 * 4; i += 4)
						{
							var a = PaletteData[i + 3];
							var c = i / 4;
							if (a != 255)
							{
								if (PixelData.Any(x => x == (byte)c))
								{
									bool tmp = false;
								}
							}
						}
					}
				}
			}
		}

	}

	public class PIC4UnknownInnerRectangles
	{
		public ushort X1 { get; private set; }
		public ushort Y1 { get; private set; }
		public ushort X2 { get; private set; }
		public ushort Y2 { get; private set; }

		public PIC4UnknownInnerRectangles(EndianBinaryReader reader)
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
