using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

using Scarlet.Drawing;
using Scarlet.IO;

namespace Scarlet.IO.ImageFormats
{
    // TODO: eve-ry-thing

    [MagicNumber("XGTL", 0x30)]
    public class CSU : XGTL
    {
        protected override void OnOpen(EndianBinaryReader reader)
        {
            reader.BaseStream.Seek(0x30, SeekOrigin.Begin);
            base.OnOpen(reader);
        }
    }
}
