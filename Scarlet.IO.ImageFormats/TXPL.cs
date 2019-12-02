using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

using Scarlet.Drawing;

namespace Scarlet.IO.ImageFormats
{
	[MagicNumber("TXPL", 0x00)]
	public class TXPL : ImageFormat
	{
		public string MagicNumber { get; private set; }
		public uint Unknown1Offset { get; private set; }
		public uint Unknown2Offset { get; private set; }

		public List<TXPLUnknownData1> UnknownData1s { get; private set; }

		List<GXT> gxtInstances;

		protected override void OnOpen(EndianBinaryReader reader)
		{
			MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
			Unknown1Offset = reader.ReadUInt32();
			Unknown2Offset = reader.ReadUInt32();

			UnknownData1s = new List<TXPLUnknownData1>();
			while (true)
			{
				TXPLUnknownData1 unknownData = new TXPLUnknownData1(reader);
				if ((unknownData.Unknown0x04 == 0 && unknownData.Unknown0x06 == 0) || (unknownData.ImageDataOffset == (uint)reader.BaseStream.Length)) break;
				UnknownData1s.Add(unknownData);
			}

			gxtInstances = new List<GXT>();
			for (int i = 0; i < UnknownData1s.Count; i++)
			{
				reader.BaseStream.Position = UnknownData1s[i].ImageDataOffset;

				using (MemoryStream gxtStream = new MemoryStream())
				{
					reader.BaseStream.CopyTo(gxtStream);
					gxtStream.Position = 0;

					GXT gxtInstance = new GXT();
					gxtInstance.Open(gxtStream);

					if (gxtInstance.TextureInfos.Length != 1) throw new Exception($"Unimplemented GXT image in TXPL; GXT has more than 1 TextureInfos ({gxtInstance.TextureInfos.Length})");

					gxtInstances.Add(gxtInstance);
				}
			}
		}

		public override int GetImageCount()
		{
			return gxtInstances.Count;
		}

		public override int GetPaletteCount()
		{
			return 0;
		}

		protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
		{
			return gxtInstances[imageIndex].GetBitmap();
		}
	}

	public class TXPLUnknownData1
	{
		public uint ImageDataOffset { get; private set; }
		public ushort Unknown0x04 { get; private set; }
		public ushort Unknown0x06 { get; private set; }

		public TXPLUnknownData1(EndianBinaryReader reader)
		{
			ImageDataOffset = reader.ReadUInt32();
			Unknown0x04 = reader.ReadUInt16();
			Unknown0x06 = reader.ReadUInt16();
		}
	}
}
