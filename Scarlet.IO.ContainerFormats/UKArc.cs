using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Scarlet.IO.ContainerFormats
{
    public class UKArcFile : ContainerElement
    {
        public string FilePath { get; private set; }
        public uint FileSize { get; private set; }
        public uint Unknown0x44 { get; private set; }
        public uint Offset { get; private set; }

        public UKArcFile(EndianBinaryReader reader)
        {
            FilePath = Encoding.ASCII.GetString(reader.ReadBytes(0x40)).TrimEnd('\0');
            FileSize = reader.ReadUInt32();
            Unknown0x44 = reader.ReadUInt32();
            Offset = reader.ReadUInt32();
        }

        public override string GetName()
        {
            return FilePath;
        }

        public override Stream GetStream(Stream containerStream)
        {
            containerStream.Seek(Offset + 0x10, SeekOrigin.Begin);
            MemoryStream stream = new MemoryStream();
            FileFormat.CopyStream(containerStream, stream, (int)FileSize);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
    }

    [MagicNumber("UKArc\0\0\0", 0x00)]
    public class UKArc : ContainerFormat
    {
        public string MagicNumber { get; private set; }
        public ushort Unknown0x08 { get; private set; }
        public ushort EndianMarker { get; private set; }
        public uint NumFiles { get; private set; }
        public UKArcFile[] Files { get; private set; }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(8));
            Unknown0x08 = reader.ReadUInt16();
            EndianMarker = reader.ReadUInt16();

            reader.Endianness = (EndianMarker == 0x1234 ? Endian.LittleEndian : Endian.BigEndian);

            NumFiles = reader.ReadUInt32();

            Files = new UKArcFile[NumFiles];
            for (int i = 0; i < Files.Length; i++) Files[i] = new UKArcFile(reader);
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
