using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Scarlet.IO.ContainerFormats
{
    internal class NARCSectionHeader
    {
        public string MagicNumber { get; private set; }
        public uint SectionSize { get; private set; }

        public NARCSectionHeader(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            SectionSize = reader.ReadUInt32();
        }
    }

    public class NARCFile : ContainerElement
    {
        public string Filename { get; private set; }
        public uint Offset { get; private set; }
        public uint FileSize { get; private set; }

        public NARCFile(string filename, uint offset, uint fileSize)
        {
            Filename = filename;
            Offset = offset;
            FileSize = fileSize;
        }

        public override string GetName()
        {
            return Filename;
        }

        public override Stream GetStream(Stream containerStream)
        {
            containerStream.Seek(Offset, SeekOrigin.Begin);
            MemoryStream stream = new MemoryStream();
            FileFormat.CopyStream(containerStream, stream, (int)FileSize);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
    }

    [MagicNumber("NARC", 0x00)]
    public class NARC : ContainerFormat
    {
        public string MagicNumber { get; private set; }
        public ushort Endianness { get; private set; }
        public ushort Constant { get; private set; }        // revision?
        public uint FileSize { get; private set; }
        public ushort HeaderSize { get; private set; }      // 0x10
        public ushort NumSections { get; private set; }

        public uint NumFiles { get; private set; }
        public Tuple<uint, uint>[] FileOffsets { get; private set; }

        public long DataStartOffset { get; private set; }

        public NARCFile[] Files { get; private set; }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            Endianness = reader.ReadUInt16();

            reader.Endianness = (Endianness == 0xFEFF ? Endian.BigEndian : Endian.LittleEndian);

            Constant = reader.ReadUInt16();
            FileSize = reader.ReadUInt32();
            HeaderSize = reader.ReadUInt16();
            NumSections = reader.ReadUInt16();

            for (int i = 0; i < NumSections; i++)
            {
                long sectionStart = reader.BaseStream.Position;

                NARCSectionHeader sectionHeader = new NARCSectionHeader(reader);
                switch (sectionHeader.MagicNumber)
                {
                    case "BTAF":
                        NumFiles = reader.ReadUInt32();
                        FileOffsets = new Tuple<uint, uint>[NumFiles];
                        for (int j = 0; j < FileOffsets.Length; j++) FileOffsets[j] = new Tuple<uint, uint>(reader.ReadUInt32(), reader.ReadUInt32());
                        break;

                    case "BTNF":
                        // TODO
                        break;

                    case "GMIF":
                        DataStartOffset = reader.BaseStream.Position;
                        break;
                }

                reader.BaseStream.Position = (sectionStart + sectionHeader.SectionSize);
            }

            Files = new NARCFile[NumFiles];
            for (int i = 0; i < Files.Length; i++)
                Files[i] = new NARCFile(i.ToString(), (uint)(DataStartOffset + FileOffsets[i].Item1), (FileOffsets[i].Item2 - FileOffsets[i].Item1));
        }

        public override int GetElementCount()
        {
            return Files.Length;
        }

        protected override ContainerElement GetElement(Stream containerStream, int elementIndex)
        {
            if (elementIndex < 0 || elementIndex >= Files.Length) throw new IndexOutOfRangeException("Invalid file index specified");
            return Files[elementIndex];
        }
    }
}
