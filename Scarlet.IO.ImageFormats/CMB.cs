using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

using Scarlet.Drawing;
using Scarlet.IO;
using Scarlet.Platform.Nintendo;

namespace Scarlet.IO.ImageFormats
{
	// NOTE: hacky, highly incomplete, basic dumper for textures from CMB models; DOES NOT handle anything model-related, only tex chunks

	[MagicNumber("cmb ", 0x00)]
	public class CMB : ImageFormat
	{
		/* cmb */
		public string MagicNumber { get; private set; }
		public uint FileSize { get; private set; }
		public uint Revision { get; private set; }      // not sure if revision; ?? == OoT3D, 0x0A == MM3D, 0x0F == LM3D
		public uint Unknown0x0C { get; private set; }
		public string ModelName { get; private set; }
		public uint Unknown0x20 { get; private set; }
		public uint[] ChunkOffsets { get; private set; }
		public uint TextureDataOffset { get; private set; }
		public uint UnknownZero { get; private set; }

		/* tex */
		const string expectedTexChunkTag = "tex ";

		public string TexChunkTag { get; private set; }
		public uint TexChunkSize { get; private set; }
		public uint TextureCount { get; private set; }
		public CTXBTexture[] Textures { get; private set; }

		byte[][] pixelData;

		protected override void OnOpen(EndianBinaryReader reader)
		{
			/* cmb */
			MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4), 0, 4);
			FileSize = reader.ReadUInt32();
			Revision = reader.ReadUInt32();
			Unknown0x0C = reader.ReadUInt32();
			ModelName = Encoding.ASCII.GetString(reader.ReadBytes(16), 0, 16);
			Unknown0x20 = reader.ReadUInt32();

			if (Revision != 0x0F) throw new Exception($"Unhandled CMB revision 0x{Revision:X2}");

			ChunkOffsets = new uint[8];
			for (int i = 0; i < ChunkOffsets.Length; i++) ChunkOffsets[i] = reader.ReadUInt32();
			TextureDataOffset = reader.ReadUInt32();
			UnknownZero = reader.ReadUInt32();

			/* tex */
			uint texChunkOffset = ChunkOffsets[3];

			reader.BaseStream.Seek(texChunkOffset, SeekOrigin.Begin);

			TexChunkTag = Encoding.ASCII.GetString(reader.ReadBytes(4), 0, 4);
			TexChunkSize = reader.ReadUInt32();
			TextureCount = reader.ReadUInt32();

			if (TexChunkTag != expectedTexChunkTag) throw new Exception($"Unexpected data in CMB; wanted '{expectedTexChunkTag}', got '{TexChunkTag}'");

			Textures = new CTXBTexture[TextureCount];
			pixelData = new byte[TextureCount][];

			for (int i = 0; i < Textures.Length; i++)
			{
				reader.BaseStream.Seek(texChunkOffset + 0xC + (i * 0x24), SeekOrigin.Begin);
				Textures[i] = new CTXBTexture(reader);

				reader.BaseStream.Seek(TextureDataOffset + Textures[i].DataOffset, SeekOrigin.Begin);
				pixelData[i] = reader.ReadBytes((int)Textures[i].DataLength);
			}
		}

		public override int GetImageCount()
		{
			return (int)TextureCount;
		}

		public override int GetPaletteCount()
		{
			return 0;
		}

		protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
		{
			CTXBTexture texture = Textures[imageIndex];

			PicaPixelFormat pixelFormat = texture.PixelFormat;
			PicaDataType dataType = ((pixelFormat == PicaPixelFormat.ETC1RGB8NativeDMP || pixelFormat == PicaPixelFormat.ETC1AlphaRGB8A4NativeDMP) ? PicaDataType.UnsignedByte : texture.DataType);

			ImageBinary imageBinary = new ImageBinary();
			imageBinary.Width = texture.Width;
			imageBinary.Height = texture.Height;
			imageBinary.InputPixelFormat = N3DS.GetPixelDataFormat(dataType, pixelFormat);
			imageBinary.InputEndianness = Endian.LittleEndian;
			imageBinary.AddInputPixels(pixelData[imageIndex]);

			return imageBinary.GetBitmap(0, 0);
		}
	}
}
