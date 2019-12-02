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

		public List<Tuple<PIC4ImageInfo, PIC4ImageInfo>> ImagePermutations { get; private set; }

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

			ImagePermutations = new List<Tuple<PIC4ImageInfo, PIC4ImageInfo>>();
			for (int i = 0; i < FacePartImages.Length; i++)
			{
				PIC4ImageInfo faceImage = FacePartImages[i].ImageInfos[1];
				for (int j = 5; j < 8; j++)
				{
					PIC4ImageInfo mouthImage = FacePartImages[i].ImageInfos[j];
					if (faceImage != null || mouthImage != null)
						ImagePermutations.Add(new Tuple<PIC4ImageInfo, PIC4ImageInfo>(faceImage, mouthImage));
				}
			}

			if (ImagePermutations.Count == 0)
				ImagePermutations.Add(new Tuple<PIC4ImageInfo, PIC4ImageInfo>(null, null));     // dummy if no face or mouth
		}

		public override int GetImageCount()
		{
			return ImagePermutations.Count;
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
					foreach (PIC4ImageInfo imageInfo in BaseImages.Select(x => x.ImageInfo))
					{
						using (Bitmap srcBitmap = GetBustupBitmap(imageInfo))
						{
							g.DrawImageUnscaled(srcBitmap, imageInfo.X, imageInfo.Y);
						}

						if (false)
						{
							foreach (PIC4UnknownInnerRectangles data in imageInfo.UnknownInnerRectangles)
								g.DrawRectangle(Pens.LawnGreen,
									imageInfo.X + data.X1, imageInfo.Y + data.Y1,
									data.X2 - data.X1, data.Y2 - data.Y1);

							foreach (PIC4UnknownOuterRectangle data in imageInfo.UnknownOuterRectangles)
								g.DrawRectangle(Pens.OrangeRed,
									imageInfo.X + data.X1, imageInfo.Y + data.Y1,
									data.X2 - data.X1, data.Y2 - data.Y1);
						}
					}

					if (ImagePermutations.Count > 0)
					{
						PIC4ImageInfo faceImageInfo = ImagePermutations[imageIndex].Item1;
						if (faceImageInfo != null)
						{
							using (Bitmap faceBitmap = GetBustupBitmap(faceImageInfo))
							{
								g.DrawImageUnscaled(faceBitmap, faceImageInfo.X, faceImageInfo.Y);
							}
						}

						PIC4ImageInfo mouthImageInfo = ImagePermutations[imageIndex].Item2;
						if (mouthImageInfo != null)
						{
							using (Bitmap mouthBitmap = GetBustupBitmap(mouthImageInfo))
							{
								g.DrawImageUnscaled(mouthBitmap, mouthImageInfo.X, mouthImageInfo.Y);
							}
						}
					}
				}

				return (Bitmap)destBitmap.Clone();
			}
		}

		private Bitmap GetBustupBitmap(PIC4ImageInfo imageInfo)
		{
			if (imageInfo != null)
			{
				ImageBinary imageBinary = new ImageBinary();

				imageBinary.PhysicalWidth = ((imageInfo.Width + 3) / 4) * 4;
				imageBinary.PhysicalHeight = imageInfo.Height;
				imageBinary.Width = imageInfo.Width;
				imageBinary.Height = imageInfo.Height;
				imageBinary.InputEndianness = Endian.LittleEndian;

				switch (imageInfo.ImageFormat)
				{
					case PIC4ImageFormat.Indexed:
						imageBinary.InputPixelFormat = PixelDataFormat.FormatIndexed8;
						imageBinary.InputPaletteFormat = PixelDataFormat.FormatArgb8888;

						byte[] paletteData = new byte[imageInfo.PaletteData.Length];
						Buffer.BlockCopy(imageInfo.PaletteData, 0, paletteData, 0, paletteData.Length);
						for (int i = 0; i < paletteData.Length; i += 4)
						{
							if (paletteData[i + 0] == 0 && paletteData[i + 1] == 0 && paletteData[i + 2] == 0)
								paletteData[i + 3] = 0;
						}

						imageBinary.AddInputPixels(imageInfo.PixelData);
						imageBinary.AddInputPalette(paletteData);
						break;

					case PIC4ImageFormat.IndexedWithAlphaMask:
						byte[] processedPixelData = new byte[imageBinary.PhysicalWidth * imageBinary.PhysicalHeight * 4];
						for (int p = 0, s = 0; p < processedPixelData.Length; p += 4, s++)
						{
							int colorOffset = (imageInfo.PixelData[s] * 4);
							processedPixelData[p + 0] = imageInfo.PaletteData[colorOffset + 0];
							processedPixelData[p + 1] = imageInfo.PaletteData[colorOffset + 1];
							processedPixelData[p + 2] = imageInfo.PaletteData[colorOffset + 2];
							processedPixelData[p + 3] = imageInfo.AlphaData[s];

							if (processedPixelData[p + 0] == 0x00 && processedPixelData[p + 1] == 0x00 && processedPixelData[p + 2] == 0x00)
								processedPixelData[p + 3] = 0x00;
						}

						imageBinary.InputPixelFormat = PixelDataFormat.FormatArgb8888;
						imageBinary.AddInputPixels(processedPixelData);
						break;
				}

				return imageBinary.GetBitmap();
			}
			else
				return new Bitmap(32, 32);
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
			Name = Encoding.ASCII.GetString(reader.ReadBytes(0x10)).TrimEnd('\0');
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
