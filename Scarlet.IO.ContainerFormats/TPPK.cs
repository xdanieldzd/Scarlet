using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Scarlet.IO.ContainerFormats
{
    public class TPPKFile : ContainerElement
    {
        int index;

        public uint Hash { get; private set; }
        public uint Offset { get; private set; }
        public uint FileSize { get; private set; }

        public TPPKFile(int index, EndianBinaryReader reader)
        {
            this.index = index;

            Hash = reader.ReadUInt32();
            Offset = reader.ReadUInt32();
            FileSize = reader.ReadUInt32();
        }

        public override string GetName()
        {
            return string.Format("{0}_{1:X8}", index, Offset);
        }

        public override Stream GetStream(Stream containerStream)
        {
            containerStream.Seek(Offset + (0x0C * (index + 1)), SeekOrigin.Begin);
            MemoryStream stream = new MemoryStream();
            FileFormat.CopyStream(containerStream, stream, (int)FileSize);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
    }

    [MagicNumber("tppk", 0x00)]
    public class TPPK : ContainerFormat
    {
        public string MagicNumber { get; private set; }
        public uint Unknown0x04 { get; private set; }
        public uint NumFiles { get; private set; }

        public TPPKFile[] Files { get; private set; }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            Unknown0x04 = reader.ReadUInt32();
            NumFiles = reader.ReadUInt32();

            Files = new TPPKFile[NumFiles];
            for (int i = 0; i < Files.Length; i++) Files[i] = new TPPKFile(i, reader);
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
