using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Scarlet.IO.ContainerFormats
{
    public class DATAkibaFile : ContainerElement
    {
        DATAkiba parentContainer;

        public uint Unknown0x00 { get; private set; }           // TODO: hash...?
        public uint DataOffset { get; private set; }
        public uint DataSize { get; private set; }
        public uint Unknown0x0C { get; private set; }           // TODO: always zero?
        public uint FilePathOffset { get; private set; }
        public uint FilePathLength { get; private set; }

        public string FilePath { get; private set; }

        public DATAkibaFile(EndianBinaryReader reader, DATAkiba container)
        {
            parentContainer = container;

            Unknown0x00 = reader.ReadUInt32();
            DataOffset = reader.ReadUInt32();
            DataSize = reader.ReadUInt32();
            Unknown0x0C = reader.ReadUInt32();
            FilePathOffset = reader.ReadUInt32();
            FilePathLength = reader.ReadUInt32();

            long position = reader.BaseStream.Position;
            reader.BaseStream.Seek(parentContainer.DataOffset + FilePathOffset, SeekOrigin.Begin);
            FilePath = Encoding.ASCII.GetString(reader.ReadBytes((int)FilePathLength)).TrimEnd('\0');
            reader.BaseStream.Seek(position, SeekOrigin.Begin);
        }

        public override string GetName()
        {
            return FilePath;
        }

        public override Stream GetStream(Stream containerStream)
        {
            containerStream.Seek(parentContainer.DataOffset + DataOffset, SeekOrigin.Begin);
            MemoryStream stream = new MemoryStream();
            FileFormat.CopyStream(containerStream, stream, (int)DataSize);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
    }

    [MagicNumber(new byte[] { 0xFA, 0xDE, 0xBA, 0xBE }, 0x00)]
    public class DATAkiba : ContainerFormat
    {
        public uint MagicNumber { get; private set; }
        public uint NumFiles { get; private set; }
        public uint NumFilesDuplicate { get; private set; }     // TODO: verify
        public uint DataOffset { get; private set; }
        public uint DataSize { get; private set; }

        public DATAkibaFile[] Files { get; private set; }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            reader.Endianness = Endian.BigEndian;

            MagicNumber = reader.ReadUInt32();
            NumFiles = reader.ReadUInt32();
            NumFilesDuplicate = reader.ReadUInt32();
            DataOffset = reader.ReadUInt32();
            DataSize = reader.ReadUInt32();

            Files = new DATAkibaFile[NumFiles];
            for (int i = 0; i < Files.Length; i++) Files[i] = new DATAkibaFile(reader, this);
        }

        public override int GetElementCount()
        {
            return (int)NumFiles;
        }

        protected override ContainerElement GetElement(Stream containerStream, int elementIndex)
        {
            if (elementIndex < 0 || elementIndex >= Files.Length) throw new IndexOutOfRangeException("Invalid file index specified");
            return Files[elementIndex];
        }
    }
}
