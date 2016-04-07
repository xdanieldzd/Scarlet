using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Scarlet.IO.CompressionFormats
{
    [DefaultExtension(".lzs")]
    public class NISLZS : CompressionFormat
    {
        public string FileExtension { get; private set; }
        public uint UncompressedSize { get; private set; }
        public uint CompressedSize { get; private set; }
        public byte CompressionFlag { get; private set; }
        public byte Unknown0x0D { get; private set; }
        public byte Unknown0x0E { get; private set; }
        public byte Unknown0x0F { get; private set; }

        byte[] decompressed;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            FileExtension = Encoding.ASCII.GetString(reader.ReadBytes(4)).TrimEnd('\0');
            UncompressedSize = reader.ReadUInt32();
            CompressedSize = reader.ReadUInt32();
            CompressionFlag = reader.ReadByte();
            Unknown0x0D = reader.ReadByte();
            Unknown0x0E = reader.ReadByte();
            Unknown0x0F = reader.ReadByte();

            decompressed = Decompress(reader.ReadBytes((int)(CompressedSize - 0x0C)));
        }

        public override Stream GetDecompressedStream()
        {
            return new MemoryStream(decompressed);
        }

        public override string GetNameOrExtension()
        {
            return FileExtension;
        }

        private byte[] Decompress(byte[] compressed)
        {
            byte[] decompressed = new byte[UncompressedSize];

            int inOffset = 0, outOffset = 0;

            while (outOffset < UncompressedSize)
            {
                byte data = compressed[inOffset++];
                if (data != CompressionFlag)
                {
                    decompressed[outOffset++] = data;
                }
                else
                {
                    byte distance = compressed[inOffset];
                    if (distance == CompressionFlag)
                    {
                        decompressed[outOffset++] = CompressionFlag;
                        inOffset++;
                    }
                    else
                    {
                        if (distance > CompressionFlag) distance--;

                        byte length = compressed[inOffset + 1];
                        for (int i = 0; i < length; i++) decompressed[outOffset + i] = decompressed[(outOffset - distance) + i];

                        inOffset += 2;
                        outOffset += length;
                    }
                }
            }

            return decompressed;
        }
    }
}
