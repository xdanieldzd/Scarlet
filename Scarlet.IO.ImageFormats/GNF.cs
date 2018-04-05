using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;

using Scarlet.Drawing;
using Scarlet.IO;
using Scarlet.Platform.Sony;

namespace Scarlet.IO.ImageFormats
{
    public enum GnfDataFormat : byte
    {
        FormatInvalid = 0x0,
        Format8 = 0x1,
        Format16 = 0x2,
        Format8_8 = 0x3,
        Format32 = 0x4,
        Format16_16 = 0x5,
        Format10_11_11 = 0x6,
        Format11_11_10 = 0x7,
        Format10_10_10_2 = 0x8,
        Format2_10_10_10 = 0x9,
        Format8_8_8_8 = 0xa,
        Format32_32 = 0xb,
        Format16_16_16_16 = 0xc,
        Format32_32_32 = 0xd,
        Format32_32_32_32 = 0xe,
        FormatReserved_15 = 0xf,
        Format5_6_5 = 0x10,
        Format1_5_5_5 = 0x11,
        Format5_5_5_1 = 0x12,
        Format4_4_4_4 = 0x13,
        Format8_24 = 0x14,
        Format24_8 = 0x15,
        FormatX24_8_32 = 0x16,
        FormatReserved_23 = 0x17,
        FormatReserved_24 = 0x18,
        FormatReserved_25 = 0x19,
        FormatReserved_26 = 0x1a,
        FormatReserved_27 = 0x1b,
        FormatReserved_28 = 0x1c,
        FormatReserved_29 = 0x1d,
        FormatReserved_30 = 0x1e,
        FormatReserved_31 = 0x1f,
        FormatGB_GR = 0x20,
        FormatBG_RG = 0x21,
        Format5_9_9_9 = 0x22,
        FormatBC1 = 0x23,
        FormatBC2 = 0x24,
        FormatBC3 = 0x25,
        FormatBC4 = 0x26,
        FormatBC5 = 0x27,
        FormatBC6 = 0x28,
        FormatBC7 = 0x29,
        FormatReserved_42 = 0x2a,
        FormatReserved_43 = 0x2b,
        FormatFMask8_S2_F1 = 0x2c,
        FormatFMask8_S4_F1 = 0x2d,
        FormatFMask8_S8_F1 = 0x2e,
        FormatFMask8_S2_F2 = 0x2f,
        FormatFMask8_S4_F2 = 0x30,
        FormatFMask8_S4_F4 = 0x31,
        FormatFMask16_S16_F1 = 0x32,
        FormatFMask16_S8_F2 = 0x33,
        FormatFMask32_S16_F2 = 0x34,
        FormatFMask32_S8_F4 = 0x35,
        FormatFMask32_S8_F8 = 0x36,
        FormatFMask64_S16_F4 = 0x37,
        FormatFMask64_S16_F8 = 0x38,
        Format4_4 = 0x39,
        Format6_5_5 = 0x3a,
        Format1 = 0x3b,
        Format1_Reversed = 0x3c,
        Format32_AS_8 = 0x3d,
        Format32_AS_8_8 = 0x3e,
        Format32_AS_32_32_32_32 = 0x3f
    }

    public enum GnfNumFormat
    {
        FormatUNorm = 0x0,
        FormatSNorm = 0x1,
        FormatUScaled = 0x2,
        FormatSScaled = 0x3,
        FormatUInt = 0x4,
        FormatSInt = 0x5,
        FormatSNorm_OGL = 0x6,
        FormatFloat = 0x7,
        FormatReserved_8 = 0x8,
        FormatSRGB = 0x9,
        FormatUBNorm = 0xa,
        FormatUBNorm_OGL = 0xb,
        FormatUBInt = 0xc,
        FormatUBScaled = 0xd,
        FormatReserved_14 = 0xe,
        FormatReserved_15 = 0xf
    }

    public enum GnfSqSel : byte
    {
        Sel0 = 0x0,
        Sel1 = 0x1,
        SelReserved_0 = 0x2,
        SelReserved_1 = 0x3,
        SelX = 0x4,
        SelY = 0x5,
        SelZ = 0x6,
        SelW = 0x7
    }

    [MagicNumber("GNF ", 0x00)]
    public class GNF : ImageFormat
    {
        public string MagicNumber { get; private set; }     /* 'GNF ' */
        public uint Unknown0x04 { get; private set; }
        public uint Unknown0x08 { get; private set; }
        public uint FileSize { get; private set; }
        public uint Unknown0x10 { get; private set; }       /* might be ImageInformation0? */
        public uint ImageInformation1 { get; private set; }
        public uint ImageInformation2 { get; private set; }
        public uint ImageInformation3 { get; private set; }
        public uint ImageInformation4 { get; private set; }
        public uint Unknown0x24 { get; private set; }       /* ImageInformation5? */
        public uint Unknown0x28 { get; private set; }       /* ImageInformation6? */
        public uint DataSize { get; private set; }
        public string UserMagicNumber { get; private set; } /* sometimes 'USER' */
        public uint[] Unknown0x34 { get; private set; }     /* 0xCC bytes, 0x33 uints */
        public byte[] PixelData { get; private set; }

        /* Extracted from image infos */
        GnfDataFormat dataFormat;
        GnfNumFormat numFormat;
        int width, height, depth, pitch;
        GnfSqSel destX, destY, destZ, destW;

        ImageBinary imageBinary;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4), 0, 4);
            Unknown0x04 = reader.ReadUInt32();
            Unknown0x08 = reader.ReadUInt32();
            FileSize = reader.ReadUInt32();
            Unknown0x10 = reader.ReadUInt32();
            ImageInformation1 = reader.ReadUInt32();
            ImageInformation2 = reader.ReadUInt32();
            ImageInformation3 = reader.ReadUInt32();
            ImageInformation4 = reader.ReadUInt32();
            Unknown0x24 = reader.ReadUInt32();
            Unknown0x28 = reader.ReadUInt32();
            DataSize = reader.ReadUInt32();
            UserMagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4), 0, 4);
            Unknown0x34 = new uint[0x33];
            for (int i = 0; i < Unknown0x34.Length; i++) Unknown0x34[i] = reader.ReadUInt32();
            PixelData = reader.ReadBytes((int)DataSize);

            /* Extract formats and dimensions (thanks bonaire.rai!) */
            dataFormat = (GnfDataFormat)ExtractData(ImageInformation1, 20, 25);
            numFormat = (GnfNumFormat)ExtractData(ImageInformation1, 26, 29);
            width = (int)(ExtractData(ImageInformation2, 0, 13) + 1);
            height = (int)(ExtractData(ImageInformation2, 14, 27) + 1);
            depth = (int)(ExtractData(ImageInformation4, 0, 12));
            pitch = (int)(ExtractData(ImageInformation4, 13, 26) + 1);
            destX = (GnfSqSel)ExtractData(ImageInformation3, 0, 2);
            destY = (GnfSqSel)ExtractData(ImageInformation3, 3, 5);
            destZ = (GnfSqSel)ExtractData(ImageInformation3, 6, 8);
            destW = (GnfSqSel)ExtractData(ImageInformation3, 9, 11);

            /* Figure out channel order from destX/Y/Z/W values */
            PixelDataFormat channelOrder;
            if (destX == GnfSqSel.SelX && destY == GnfSqSel.SelY && destZ == GnfSqSel.SelZ && destW == GnfSqSel.SelW)
                channelOrder = PixelDataFormat.ChannelsAbgr;
            else if (destX == GnfSqSel.SelZ && destY == GnfSqSel.SelY && destZ == GnfSqSel.SelX && destW == GnfSqSel.SelW)
                channelOrder = PixelDataFormat.ChannelsArgb;
            else
                throw new Exception($"Unhandled GNF channel destinations (X={destX}, Y={destY}, Z={destZ}, W={destW})");

            /* Initialize ImageBinary */
            imageBinary = new ImageBinary();
            imageBinary.Width = width;
            imageBinary.Height = height;
            imageBinary.PhysicalWidth = pitch;
            imageBinary.PhysicalHeight = height;

            byte[] preparedPixelData = PixelData;

            // TODO: stupid formats Scarlet can't support yet, like stuff with 16bits per channel, verify BC6, Format32...
            if (dataFormat == GnfDataFormat.Format16_16_16_16)
            {
                preparedPixelData = new byte[PixelData.Length / 2];
                for (int i = 0, j = 0; i < PixelData.Length; i += 2, j++)
                {
                    preparedPixelData[j] = PixelData[i];
                }
            }
            else if (dataFormat == GnfDataFormat.Format32)
            {
                preparedPixelData = new byte[PixelData.Length / 4];
                for (int i = 0, j = 0; i < PixelData.Length; i += 4, j++)
                {
                    float value = BitConverter.ToSingle(PixelData, i);
                    if (numFormat == GnfNumFormat.FormatFloat)
                    {
                        preparedPixelData[j] = (byte)(value + 128.0f);
                    }
                    else
                    {
                        preparedPixelData[j] = (byte)(value * 255);
                    }
                }
            }
            else if (dataFormat == GnfDataFormat.Format32_32_32_32)
            {
                preparedPixelData = new byte[PixelData.Length / 4];
                for (int i = 0, j = 0; i < PixelData.Length; i += 4, j++)
                {
                    float value = BitConverter.ToSingle(PixelData, i);
                    preparedPixelData[j] = (byte)(value * 255);
                }
            }

            switch (dataFormat)
            {
                case GnfDataFormat.Format8_8_8_8: imageBinary.InputPixelFormat = (PixelDataFormat.Bpp32 | channelOrder | PixelDataFormat.RedBits8 | PixelDataFormat.GreenBits8 | PixelDataFormat.BlueBits8 | PixelDataFormat.AlphaBits8); break;
                case GnfDataFormat.FormatBC1: imageBinary.InputPixelFormat = PixelDataFormat.FormatDXT1Rgba; break;
                case GnfDataFormat.FormatBC2: imageBinary.InputPixelFormat = PixelDataFormat.FormatDXT3; break;
                case GnfDataFormat.FormatBC3: imageBinary.InputPixelFormat = PixelDataFormat.FormatDXT5; break;
                case GnfDataFormat.FormatBC4: imageBinary.InputPixelFormat = (numFormat == GnfNumFormat.FormatSNorm ? PixelDataFormat.FormatRGTC1_Signed : PixelDataFormat.FormatRGTC1); break;
                case GnfDataFormat.FormatBC5: imageBinary.InputPixelFormat = (numFormat == GnfNumFormat.FormatSNorm ? PixelDataFormat.FormatRGTC2_Signed : PixelDataFormat.FormatRGTC2); break;
                //case GnfDataFormat.FormatBC6: imageBinary.InputPixelFormat = PixelDataFormat.FormatBPTC_Float;/*(numFormat == GnfNumFormat.FormatSNorm ? PixelDataFormat.FormatBPTC_SignedFloat : PixelDataFormat.FormatBPTC_Float);*/ break;   // TODO: fixme!!
                case GnfDataFormat.FormatBC7: imageBinary.InputPixelFormat = PixelDataFormat.FormatBPTC; break;

                // TODO
                //case GnfDataFormat.Format16_16_16_16: imageBinary.InputPixelFormat = PixelDataFormat.FormatAbgr8888; break;
                //case GnfDataFormat.Format32: imageBinary.InputPixelFormat = PixelDataFormat.FormatLuminance8; break;
                //case GnfDataFormat.Format32_32_32_32: imageBinary.InputPixelFormat = PixelDataFormat.FormatAbgr8888; break;

                // WRONG
                //case GnfDataFormat.Format8: imageBinary.InputPixelFormat = PixelDataFormat.FormatLuminance8; break;
                //case GnfDataFormat.Format8_8: imageBinary.InputPixelFormat = PixelDataFormat.FormatLuminance8; break;

                default: throw new Exception($"Unimplemented GNF data format {dataFormat}");
            }

            imageBinary.InputPixelFormat |= PixelDataFormat.PixelOrderingTiled3DS;

            imageBinary.AddInputPixels(preparedPixelData);
        }

        private uint ExtractData(uint val, int first, int last)
        {
            uint mask = (((uint)(1 << ((last + 1) - first)) - 1) << first);
            return ((val & mask) >> first);
        }

        public override int GetImageCount()
        {
            return 1;
        }

        public override int GetPaletteCount()
        {
            return 0;
        }

        protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
        {
            return imageBinary.GetBitmap(imageIndex, paletteIndex);
        }
    }
}
