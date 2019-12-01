using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scarlet.IO.Compression
{
	/* Konosuba/Entergram PSVita */

	internal static class EGLZ77
	{
		// TODO: *very* similar to DS LZ77 type 0x10; merge code?
		public static byte[] Decompress(byte[] compressed, int decompressedSize)
		{
			uint inOffset = 0, outOffset = 0;
			ushort windowOffset;
			byte length, compFlags;

			byte[] decompressed = new byte[decompressedSize];

			while (inOffset < compressed.Length)
			{
				compFlags = compressed[inOffset++];

				for (int i = 0; i < 8; i++)
				{
					if ((compFlags & 0x01) != 0)
					{
						if ((inOffset + 1) < compressed.Length)
						{
							ushort data = (ushort)(compressed[inOffset] << 8 | compressed[inOffset + 1]);
							inOffset += 2;

							length = (byte)((data >> 12) + 3);
							windowOffset = (ushort)((data & 0xFFF) + 1);

							uint startOffset = outOffset - windowOffset;
							for (int j = 0; j < length; j++)
								if (outOffset < decompressed.Length && startOffset + j < decompressed.Length)
									decompressed[outOffset++] = decompressed[startOffset + j];
						}
					}
					else
					{
						if (outOffset < decompressed.Length && inOffset < compressed.Length)
							decompressed[outOffset++] = compressed[inOffset++];
					}

					compFlags >>= 1;
				}
			}

			return decompressed;
		}
	}
}
