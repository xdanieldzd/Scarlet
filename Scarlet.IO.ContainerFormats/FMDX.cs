using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Scarlet.IO.ContainerFormats
{
    public class FMDXFile : ContainerElement
    {
        public string FilePath { get; private set; }
        public uint Offset { get; private set; }
        public uint FileSize { get; private set; }
        public uint Unknown0x88 { get; private set; }
        public uint Unknown0x8C { get; private set; }

        public FMDXFile(EndianBinaryReader reader)
        {
            FilePath = Encoding.ASCII.GetString(reader.ReadBytes(0x80)).TrimEnd('\0');
            Offset = reader.ReadUInt32();
            FileSize = reader.ReadUInt32();
            Unknown0x88 = reader.ReadUInt32();
            Unknown0x8C = reader.ReadUInt32();
        }

        public override string GetName()
        {
            return FilePath;
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

    [MagicNumber("FMDX", 0x00)]
    public class FMDX : ContainerFormat
    {
        public string MagicNumber { get; private set; }
        public uint FileDataSize { get; private set; }
        public uint NumFiles { get; private set; }
        public uint Unknown0x0C { get; private set; }
        public byte[] Unknown0x10 { get; private set; }

        public FMDXFile[] Files { get; private set; }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            FileDataSize = reader.ReadUInt32();
            NumFiles = reader.ReadUInt32();
            Unknown0x0C = reader.ReadUInt32();
            Unknown0x10 = reader.ReadBytes(0x44);

            Files = new FMDXFile[NumFiles];
            for (int i = 0; i < Files.Length; i++) Files[i] = new FMDXFile(reader);
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
