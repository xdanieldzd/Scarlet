using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace Scarlet.IO.CompressionFormats
{
    [MagicNumber(new byte[] { 0x78, 0x9C }, 0x00)]
    public class DEFLATE : CompressionFormat
    {
        byte[] decompressed;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            reader.BaseStream.Seek(2, SeekOrigin.Current);
            using (DeflateStream deflateStream = new DeflateStream(reader.BaseStream, CompressionMode.Decompress))
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    deflateStream.CopyTo(memoryStream);
                    decompressed = memoryStream.ToArray();
                }
            }
        }

        public override Stream GetDecompressedStream()
        {
            return new MemoryStream(decompressed);
        }

        public override string GetNameOrExtension()
        {
            return string.Empty;
        }
    }
}
