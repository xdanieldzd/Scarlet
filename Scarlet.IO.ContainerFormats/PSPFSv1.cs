using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Scarlet.IO.ContainerFormats
{
    public class PSPFSv1File : ContainerElement
    {
        public string Filename { get; private set; }
        public uint FileSize { get; private set; }
        public uint Offset { get; private set; }

        public PSPFSv1File(EndianBinaryReader reader)
        {
            Filename = Encoding.ASCII.GetString(reader.ReadBytes(0x18)).TrimEnd('\0');
            FileSize = reader.ReadUInt32();
            Offset = reader.ReadUInt32();
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

    [MagicNumber("PSPFS_V1", 0x00)]
    public class PSPFSv1 : ContainerFormat
    {
        public string MagicNumber { get; private set; }
        public uint NumFiles { get; private set; }
        public uint Unknown0x0C { get; private set; }
        public PSPFSv1File[] Files { get; private set; }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(8));
            NumFiles = reader.ReadUInt32();
            Unknown0x0C = reader.ReadUInt32();

            Files = new PSPFSv1File[NumFiles];
            for (int i = 0; i < Files.Length; i++) Files[i] = new PSPFSv1File(reader);
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
