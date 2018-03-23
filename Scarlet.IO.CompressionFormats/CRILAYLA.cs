using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Scarlet.IO.CompressionFormats
{
    // adapted from https://github.com/esperknight/CriPakTools

    [MagicNumber("CRILAYLA", 0x00)]
    public class CRILAYLA : CompressionFormat
    {
        public int UncompressedSize { get; private set; }
        public int UncompressedHeaderOffset { get; private set; }
        byte[] decompressed;
        string extension;

        public override Stream GetDecompressedStream()
        {
            return new MemoryStream(decompressed);
        }

        public override string GetNameOrExtension()
        {
            return extension;
        }

        private ushort GetNextBits(byte[] input, ref int offsetP, ref byte bitPoolP, ref int bitsLeftP, int bitCount)
        {
            ushort outBits = 0;
            int numBitsProduced = 0;
            int bitsThisRound;

            while (numBitsProduced < bitCount)
            {
                if (bitsLeftP == 0)
                {
                    bitPoolP = input[offsetP];
                    bitsLeftP = 8;
                    offsetP--;
                }

                if (bitsLeftP > (bitCount - numBitsProduced))
                    bitsThisRound = bitCount - numBitsProduced;
                else
                    bitsThisRound = bitsLeftP;

                outBits <<= bitsThisRound;

                outBits |= (ushort)((ushort)(bitPoolP >> (bitsLeftP - bitsThisRound)) & ((1 << bitsThisRound) - 1));

                bitsLeftP -= bitsThisRound;
                numBitsProduced += bitsThisRound;
            }

            return outBits;
        }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            byte[] compressed = new byte[reader.BaseStream.Length];

            reader.Read(compressed, 0, compressed.Length);

            if (Encoding.ASCII.GetString(compressed, 0, 8) != "CRILAYLA")
                throw new NotImplementedException("Invalid CRILAYLA compressed data");

            reader.BaseStream.Seek(8, SeekOrigin.Begin);

            UncompressedSize = reader.ReadInt32();
            UncompressedHeaderOffset = reader.ReadInt32();
            decompressed = new byte[UncompressedSize + 0x100];

            Array.Copy(compressed, UncompressedHeaderOffset + 0x10, decompressed, 0, 0x100);

            int inputEnd = compressed.Length - 0x100 - 1;
            int inputOffset = inputEnd;
            int outputEnd = 0x100 + UncompressedSize - 1;
            byte bitPool = 0;
            int bitsLeft = 0, bytesOutput = 0;
            int[] vleLens = new int[4] { 2, 3, 5, 8 };

            while (bytesOutput < UncompressedSize)
            {
                if (GetNextBits(compressed, ref inputOffset, ref bitPool, ref bitsLeft, 1) > 0)
                {
                    int backreferenceOffset = outputEnd - bytesOutput + GetNextBits(compressed, ref inputOffset, ref bitPool, ref bitsLeft, 13) + 3;
                    int backreferenceLength = 3;
                    int vleLevel;

                    for (vleLevel = 0; vleLevel < vleLens.Length; vleLevel++)
                    {
                        int this_level = GetNextBits(compressed, ref inputOffset, ref bitPool, ref bitsLeft, vleLens[vleLevel]);
                        backreferenceLength += this_level;
                        if (this_level != ((1 << vleLens[vleLevel]) - 1)) break;
                    }

                    if (vleLevel == vleLens.Length)
                    {
                        int this_level;
                        do
                        {
                            this_level = GetNextBits(compressed, ref inputOffset, ref bitPool, ref bitsLeft, 8);
                            backreferenceLength += this_level;
                        } while (this_level == 255);
                    }

                    for (int i = 0; i < backreferenceLength; i++)
                    {
                        decompressed[outputEnd - bytesOutput] = decompressed[backreferenceOffset--];
                        bytesOutput++;
                    }
                }
                else
                {
                    // verbatim byte
                    decompressed[outputEnd - bytesOutput] = (byte)GetNextBits(compressed, ref inputOffset, ref bitPool, ref bitsLeft, 8);
                    bytesOutput++;
                }
            }

            /* keep the original extension */
            if (reader.BaseStream is FileStream)
                extension = Path.GetFileName((reader.BaseStream as FileStream).Name).TrimStart('.');
            else
                extension = "bin";
        }
    }
}