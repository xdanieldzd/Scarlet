using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Scarlet.IO.CompressionFormats
{
	// Xkeeper's YKCMP_V1 decompression script: https://gist.github.com/Xkeeper0/d1ef62e5464e8bbfa655b556a78af1ac

	// TODO: errrrr not working? <.<




	[MagicNumber("YKCMP_V1", 0x00)]
	public class YKCMPv1 : CompressionFormat
	{
		public string MagicNumber { get; private set; }
		public uint Unknown0x08 { get; private set; }
		public uint CompressedSize { get; private set; }
		public uint UncompressedSize { get; private set; }

		byte[] decompressed;

		protected override void OnOpen(EndianBinaryReader reader)
		{
			MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(8));
			Unknown0x08 = reader.ReadUInt32();
			CompressedSize = reader.ReadUInt32();
			UncompressedSize = reader.ReadUInt32();

			decompressed = Decompress(reader.ReadBytes((int)(CompressedSize - 0x14)));
		}

		public override Stream GetDecompressedStream()
		{
			return new MemoryStream(decompressed);
		}

		public override string GetNameOrExtension()
		{
			return string.Empty;
		}

		private byte[] Decompress(byte[] compressed)
		{
			byte[] decompressed = new byte[UncompressedSize];

			int inOffset = 0, outOffset = 0;

			//Console.WriteLine("begin decompress...");

			while (outOffset < UncompressedSize)
			{
				byte data = compressed[inOffset++];
				ushort length, lookback;

				if (inOffset >= compressed.Length) break;

				if ((data & 0x80) == 0x00)
				{
					length = data;
					lookback = 0;

					Buffer.BlockCopy(compressed, inOffset, decompressed, outOffset, length);

					inOffset += length;
					outOffset += length;
				}
				else
				{
					if ((data & 0x40) == 0x00)
					{
						length = (ushort)(((data >> 4) & 0x03) + 1);
						lookback = length;
					}
					else /*if ((data & 0x20) == 0x00)*/
					{
						length = (ushort)((data & 0x1F) + 2);
						lookback = compressed[inOffset++];
					}

					Buffer.BlockCopy(decompressed, (byte)(outOffset - lookback - 1), decompressed, outOffset, length);

					inOffset += length;
					outOffset += length;
				}




				/*
				if ((data & 0x80) == 0x00)
				{
					// Direct copy
					byte length = (byte)(data & 0x7F);

					Console.WriteLine($"CP: {data:X2}       --> outOffset:{outOffset:X8}, length:{length:X3}");

					for (int i = 0; i < length; i++)
						decompressed[outOffset++] = compressed[inOffset++];
				}
				else
				{
					if (data < 0xC0)
					{
						// One-byte lookbehind
						byte length = (byte)((data & 0x70) >> 4);
						byte back = (byte)(data & 0x0F);

						Console.WriteLine($"2B: {data:X2}       --> outOffset:{outOffset:X8}, length:{length:X3}, back:{back:X3}");

						CopyBack(compressed, decompressed, outOffset, length, back);

						outOffset += length;
					}
					else if (data < 0xE0)
					{
						// Two-byte lookbehind
						byte temp1 = compressed[inOffset++];

						byte length = (byte)(data & 0x3F);
						byte back = temp1;

						Console.WriteLine($"2B: {data:X2} {temp1:X2}    --> outOffset:{outOffset:X8}, length:{length:X3}, back:{back:X3}");

						CopyBack(compressed, decompressed, outOffset, length, back);

						outOffset += length;
					}
					else
					{
						// Three-byte lookbehind
						byte temp1 = compressed[inOffset++];
						byte temp2 = compressed[inOffset++];

						ushort length = (ushort)((data & 0x1F) << 4);
						length |= (byte)((temp1 & 0xF0) >> 4);

						ushort back = (ushort)((temp1 & 0xF) << 8);
						back |= temp2;

						Console.WriteLine($"3B: {data:X2} {temp1:X2} {temp2:X2} --> outOffset:{outOffset:X8}, length:{length:X3}, back:{back:X3}");

						CopyBack(compressed, decompressed, outOffset, length, back);

						outOffset += length;
					}
				}*/
			}

			return decompressed;
		}

		private void CopyBack(byte[] compressed, byte[] decompressed, int outOffset, ushort length, ushort back)
		{
			for (int i = 0; i < length; i++)
				decompressed[outOffset + i] = decompressed[(outOffset + i) - back];
		}
	}
}
