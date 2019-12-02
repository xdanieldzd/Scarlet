using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Scarlet.IO.Compression;

namespace Scarlet.IO.CompressionFormats
{
	// TODO: not verified!

	[MagicNumber("OLZP", 0x00)]
	public class OLZP : CompressionFormat
	{
		public string MagicNumber { get; private set; }
		public uint UncompressedSize { get; private set; }
		public uint CompressedSize { get; private set; }

		byte[] decompressed;

		protected override void OnOpen(EndianBinaryReader reader)
		{
			MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
			UncompressedSize = reader.ReadUInt32();
			CompressedSize = reader.ReadUInt32();

			decompressed = EGLZ77.Decompress(reader.ReadBytes((int)CompressedSize), (int)UncompressedSize);
		}

		public override Stream GetDecompressedStream()
		{
			return new MemoryStream(decompressed);
		}

		public override string GetNameOrExtension()
		{
			return "bin";
		}
	}
}
