using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Scarlet.IO.CompressionFormats
{
    public abstract class CompressionFormat : FileFormat
    {
        protected CompressionFormat() : base() { }

        public abstract Stream GetDecompressedStream();

        // TODO: make this part better?
        public abstract string GetNameOrExtension();
    }
}
