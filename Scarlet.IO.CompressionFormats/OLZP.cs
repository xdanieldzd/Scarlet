using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace Scarlet.IO.CompressionFormats
{
	// TODO: replace unsafe, hacked up Ghidra pseudo-C with proper C#

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

			byte[] compressed = reader.ReadBytes((int)CompressedSize);
			decompressed = new byte[UncompressedSize];

			unsafe
			{
				GCHandle inHandle;
				inHandle = GCHandle.Alloc(compressed, GCHandleType.Pinned);
				byte* inBuffer = (byte*)inHandle.AddrOfPinnedObject().ToPointer();

				GCHandle outHandle;
				outHandle = GCHandle.Alloc(decompressed, GCHandleType.Pinned);
				byte* outBuffer = (byte*)outHandle.AddrOfPinnedObject().ToPointer();

				uint uVar1;
				uint uVar2;
				int iVar3 = compressed.Length;
				int iVar4;
				byte* pbVar5;
				byte* pbVar6;

				uVar1 = 2;
				pbVar6 = inBuffer;
				while (true)
				{
					uVar2 = (uint)*pbVar6;
					uVar1 = uVar1 >> 1;
					pbVar5 = pbVar6 + 1;
					iVar4 = iVar3;
					if (uVar1 == 1)
					{
						uVar1 = uVar2 | 0x100;
						uVar2 = (uint)*pbVar5;
						iVar4 = iVar3 + -1;
						pbVar5 = pbVar6 + 2;
					}
					iVar3 = iVar4 + -1;
					if (iVar3 < 0) break;
					if ((uVar1 & 1) == 0)
					{
						*outBuffer = (byte)uVar2;
						pbVar6 = pbVar5;
						outBuffer = outBuffer + 1;
					}
					else
					{
						pbVar6 = pbVar5 + 1;
						iVar3 = iVar4 + -2;
						iVar4 = (int)(((uint)*pbVar5 & 0x1f) + 3);
						pbVar5 = outBuffer + -(uVar2 | (uint)(*pbVar5 >> 5) << 8);
						while (((iVar4 = iVar4 + -1) >= 0) && (-1 < iVar4))
						{
							*outBuffer = *pbVar5;
							pbVar5 = pbVar5 + 1;
							outBuffer = outBuffer + 1;
						}
					}
				}

				inHandle.Free();
				outHandle.Free();
			}
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
