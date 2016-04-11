using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Scarlet.IO.ImageFormats
{
    public abstract class ImageFormat : FileFormat
    {
        protected ImageFormat() : base() { }

        public abstract int GetImageCount();
        public abstract int GetPaletteCount();

        public virtual string GetImageName(int imageIndex)
        {
            return null;
        }

        public Bitmap GetBitmap()
        {
            return OnGetBitmap(0, 0);
        }

        public Bitmap GetBitmap(int imageIndex, int paletteIndex)
        {
            if (imageIndex < 0 || (GetImageCount() != 0 && imageIndex >= GetImageCount())) throw new IndexOutOfRangeException("Image index out of range");
            if (paletteIndex < 0 || (GetPaletteCount() != 0 && paletteIndex >= GetPaletteCount())) throw new IndexOutOfRangeException("Palette index out of range");

            return OnGetBitmap(imageIndex, paletteIndex);
        }

        public IEnumerable<Bitmap> GetBitmaps(int paletteIndex)
        {
            if (paletteIndex < 0 || (GetPaletteCount() != 0 && paletteIndex >= GetPaletteCount())) throw new IndexOutOfRangeException("Palette index out of range");

            List<Bitmap> images = new List<Bitmap>();
            for (int i = 0; i < GetImageCount(); i++) images.Add(OnGetBitmap(i, paletteIndex));
            return images;
        }

        protected abstract Bitmap OnGetBitmap(int imageIndex, int paletteIndex);
    }
}
