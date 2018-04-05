using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Scarlet.Drawing;

namespace ScarletUnitTests
{
    // TODO: more and better unit tests?

    [TestClass]
    public class ConstantsTests
    {
        [TestMethod]
        public void TestPixelDataFormatEnumeration()
        {
            ulong[] maskValues = new ulong[11];
            maskValues[0] = (ulong)PixelDataFormat.MaskBpp;
            maskValues[1] = (ulong)PixelDataFormat.MaskChannels;
            maskValues[2] = (ulong)PixelDataFormat.MaskRedBits;
            maskValues[3] = (ulong)PixelDataFormat.MaskGreenBits;
            maskValues[4] = (ulong)PixelDataFormat.MaskBlueBits;
            maskValues[5] = (ulong)PixelDataFormat.MaskAlphaBits;
            maskValues[6] = (ulong)PixelDataFormat.MaskSpecial;
            maskValues[7] = (ulong)PixelDataFormat.MaskPixelOrdering;
            maskValues[8] = (ulong)PixelDataFormat.MaskFilter;
            maskValues[9] = (ulong)PixelDataFormat.MaskForceChannel;
            maskValues[10] = (ulong)PixelDataFormat.MaskReserved;

            ulong expectedSum = 0;
            for (int i = 0; i < maskValues.Length; i++) expectedSum ^= maskValues[i];
            Assert.AreEqual(ulong.MaxValue, expectedSum);
        }
    }
}
