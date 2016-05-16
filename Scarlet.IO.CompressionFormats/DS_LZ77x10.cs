using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Scarlet.IO.CompressionFormats
{
    // TODO: better way of detection? graceful exit on decompression failure?

    [MagicNumber(new byte[] { 0x10 }, 0x00)]
    [FilenamePattern("^.*\\.cmp$")]
    public class DS_LZ77x10 : CompressionFormat
    {
        uint magicAndUncompressedSize;

        public byte MagicNumber { get { return (byte)(magicAndUncompressedSize & 0xFF); } }
        public uint UncompressedSize { get { return (uint)(magicAndUncompressedSize >> 8); } }

        byte[] decompressed;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            magicAndUncompressedSize = reader.ReadUInt32();

            decompressed = Decompress(reader.ReadBytes((int)(reader.BaseStream.Length - 0x04)));
        }

        public override Stream GetDecompressedStream()
        {
            return new MemoryStream(decompressed);
        }

        public override string GetNameOrExtension()
        {
            return "bin";
        }

        private byte[] Decompress(byte[] compressed)
        {
            byte[] decompressed = new byte[UncompressedSize];

            uint inOffset = 0, outOffset = 0;
            ushort windowOffset;
            byte length, compFlags;

            while (outOffset < decompressed.Length)
            {
                compFlags = compressed[inOffset++];
                for (int i = 0; i < 8; i++)
                {
                    if ((compFlags & 0x80) != 0)
                    {
                        ushort data = (ushort)((compressed[inOffset] << 8) | compressed[inOffset + 1]);
                        inOffset += 2;

                        length = (byte)((data >> 12) + 3);
                        windowOffset = (ushort)((data & 0xFFF) + 1);
                        compFlags <<= 1;

                        uint startOffset = outOffset - windowOffset;
                        if (outOffset + length > decompressed.Length || startOffset + length > decompressed.Length) throw new IndexOutOfRangeException();
                        for (int j = 0; j < length; j++) decompressed[outOffset++] = decompressed[startOffset + j];
                    }
                    else
                    {
                        if (outOffset >= decompressed.Length || inOffset >= compressed.Length) return decompressed;
                        decompressed[outOffset++] = compressed[inOffset++];
                        compFlags <<= 1;
                    }
                }
            }

            return decompressed;
        }
    }
}
