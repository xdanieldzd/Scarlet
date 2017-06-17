using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Scarlet.IO.CompressionFormats
{
    // https://github.com/ShimmerFairy/MM3D/blob/master/src/lzs.cpp

    [MagicNumber("LzS\x01", 0x00)]
    [FilenamePattern("^.*\\.lzs$")]
    public class GrezzoLZS : CompressionFormat
    {
        public string MagicNumber { get; private set; }
        public uint Unknown0x04 { get; private set; }
        public uint UncompressedSize { get; private set; }
        public uint CompressedSize { get; private set; }

        byte[] decompressed;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            Unknown0x04 = reader.ReadUInt32();
            UncompressedSize = reader.ReadUInt32();
            CompressedSize = reader.ReadUInt32();

            decompressed = Decompress(reader.ReadBytes((int)CompressedSize));
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
            List<byte> outdata = new List<byte>();
            byte[] BUFFER = new byte[4096];
            for (int i = 0; i < BUFFER.Length; i++) BUFFER[i] = 0;
            byte flags8 = 0;
            ushort writeidx = 0xFEE;
            ushort readidx = 0;
            uint fidx = 0;

            while (fidx < compressed.Length)
            {
                flags8 = compressed[fidx];
                fidx++;

                for (int i = 0; i < 8; i++)
                {
                    if ((flags8 & 1) != 0)
                    {
                        outdata.Add(compressed[fidx]);
                        BUFFER[writeidx] = compressed[fidx];
                        writeidx++; writeidx %= 4096;
                        fidx++;
                    }
                    else
                    {
                        readidx = compressed[fidx];
                        fidx++;
                        readidx |= (ushort)((compressed[fidx] & 0xF0) << 4);
                        for (int j = 0; j < (compressed[fidx] & 0x0F) + 3; j++)
                        {
                            outdata.Add(BUFFER[readidx]);
                            BUFFER[writeidx] = BUFFER[readidx];
                            readidx++; readidx %= 4096;
                            writeidx++; writeidx %= 4096;
                        }
                        fidx++;
                    }
                    flags8 >>= 1;
                    if (fidx >= compressed.Length) break;
                }
            }

            if (UncompressedSize != outdata.Count)
                throw new Exception(string.Format("Size mismatch: got {0} bytes after decompression, expected {1}.\n", outdata.Count, UncompressedSize));

            return outdata.ToArray();
        }
    }
}
