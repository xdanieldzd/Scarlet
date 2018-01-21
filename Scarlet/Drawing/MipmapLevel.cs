using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scarlet.Drawing
{
    internal class MipmapLevel
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public byte[] PixelData { get; private set; }

        public MipmapLevel(int width, int height, byte[] data)
        {
            Width = width;
            Height = height;
            PixelData = data;
        }
    }
}
