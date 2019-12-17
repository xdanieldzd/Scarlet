using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

namespace Scarlet.IO.ImageFormats
{
	// TODO: seems weird, not sure if correct

	[MagicNumber("TXPL", 0x00)]
	public class TXPL : ImageFormat
	{
		public string MagicNumber { get; private set; }
		public uint HeaderSize { get; private set; }
		public uint SubHeaderDataSize { get; private set; }
		public TXPLImageInfo[] ImageInfos { get; private set; }
		public uint FileSize { get; private set; }

		public TXPLSubHeader SubHeader { get; private set; }

		public List<Bitmap> BaseImages { get; private set; }
		public List<Bitmap> ElementImages { get; private set; }

		protected override void OnOpen(EndianBinaryReader reader)
		{
			MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
			HeaderSize = reader.ReadUInt32();
			SubHeaderDataSize = reader.ReadUInt32();

			long lastPosition = reader.BaseStream.Position;
			reader.BaseStream.Position = HeaderSize;
			SubHeader = new TXPLSubHeader(reader);
			reader.BaseStream.Position = lastPosition;

			ImageInfos = new TXPLImageInfo[SubHeader.NumImageInfos];
			for (int i = 0; i < ImageInfos.Length; i++) ImageInfos[i] = new TXPLImageInfo(reader);
			FileSize = reader.ReadUInt32();

			BaseImages = new List<Bitmap>();

			GXT gxtInstance = new GXT();
			for (int i = 0; i < ImageInfos.Length; i++)
			{
				reader.BaseStream.Position = ImageInfos[i].ImageDataOffset;

				using (MemoryStream gxtStream = new MemoryStream())
				{
					reader.BaseStream.CopyTo(gxtStream);
					gxtStream.Position = 0;

					gxtInstance.Open(gxtStream);
					if (gxtInstance.TextureInfos.Length != 1) throw new Exception($"Unimplemented GXT image in TXPL; GXT has more than 1 TextureInfos ({gxtInstance.TextureInfos.Length})");
					BaseImages.Add(gxtInstance.GetBitmap());
				}
			}

			ElementImages = new List<Bitmap>();
			for (int i = 0; i < SubHeader.Rectangles.Length; i++)
			{
				TXPLRectangle rectangle = SubHeader.Rectangles[i];

				if (rectangle.Unknown0x00 != 0x00)
				{
					using (Bitmap bitmap = new Bitmap(rectangle.Width, rectangle.Height))
					{
						using (Graphics g = Graphics.FromImage(bitmap))
						{
							g.DrawImage(
								BaseImages[rectangle.ImageIndex],
								new Rectangle(0, 0, bitmap.Width, bitmap.Height),
								rectangle.SourceX, rectangle.SourceY, bitmap.Width, bitmap.Height,
								GraphicsUnit.Pixel);
						}
						ElementImages.Add((Bitmap)bitmap.Clone());
					}
				}
				else
					ElementImages.Add(new Bitmap(32, 32));
			}
		}

		public override int GetImageCount()
		{
			return (BaseImages.Count + ElementImages.Count);
		}

		public override int GetPaletteCount()
		{
			return 0;
		}

		protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
		{
			return (imageIndex < BaseImages.Count ? BaseImages[imageIndex] : ElementImages[imageIndex - BaseImages.Count]);
		}
	}

	public class TXPLSubHeader
	{
		public uint ImageWidth { get; private set; }
		public uint ImageHeight { get; private set; }
		public uint NumImageInfos { get; private set; }
		public uint NumRectangles { get; private set; }

		public TXPLRectangle[] Rectangles { get; private set; }

		public TXPLSubHeader(EndianBinaryReader reader)
		{
			ImageWidth = reader.ReadUInt32();
			ImageHeight = reader.ReadUInt32();
			NumImageInfos = reader.ReadUInt32();
			NumRectangles = reader.ReadUInt32();

			Rectangles = new TXPLRectangle[NumRectangles];
			for (int i = 0; i < Rectangles.Length; i++) Rectangles[i] = new TXPLRectangle(reader);
		}
	}

	public class TXPLImageInfo
	{
		public uint ImageDataOffset { get; private set; }
		public ushort Unknown0x04 { get; private set; }
		public ushort Unknown0x06 { get; private set; }

		public TXPLImageInfo(EndianBinaryReader reader)
		{
			ImageDataOffset = reader.ReadUInt32();
			Unknown0x04 = reader.ReadUInt16();
			Unknown0x06 = reader.ReadUInt16();
		}
	}

	public class TXPLRectangle
	{
		public ushort Unknown0x00 { get; private set; }
		public ushort ImageIndex { get; private set; }
		public ushort SourceX { get; private set; }
		public ushort SourceY { get; private set; }
		public ushort Width { get; private set; }
		public ushort Height { get; private set; }

		public TXPLRectangle(EndianBinaryReader reader)
		{
			Unknown0x00 = reader.ReadUInt16();
			ImageIndex = reader.ReadUInt16();
			SourceX = reader.ReadUInt16();
			SourceY = reader.ReadUInt16();
			Width = reader.ReadUInt16();
			Height = reader.ReadUInt16();
		}
	}
}
