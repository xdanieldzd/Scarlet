using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace Scarlet.IO.CompressionFormats
{
    [MagicNumber(new byte[] { 0x1F, 0x8B }, 0x00)]
    [FilenamePattern("^.*\\.gz$")]
    public class GZip : CompressionFormat
    {
        byte[] decompressed;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            using (GZipStream gzipStream = new GZipStream(reader.BaseStream, CompressionMode.Decompress))
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    gzipStream.CopyTo(memoryStream);
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
