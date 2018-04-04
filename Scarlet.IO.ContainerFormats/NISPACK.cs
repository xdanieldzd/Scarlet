using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Scarlet.IO.ContainerFormats
{
    public class NISPACKFile : ContainerElement
    {
        public string Filename { get; private set; }
        public uint Offset { get; private set; }
        public uint FileSize { get; private set; }
        public uint Unknown0x28 { get; private set; }

        public NISPACKFile(EndianBinaryReader reader)
        {
            Filename = Encoding.GetEncoding("SJIS").GetString(reader.ReadBytes(0x20)).TrimEnd('\0');
            Offset = reader.ReadUInt32();
            FileSize = reader.ReadUInt32();
            Unknown0x28 = reader.ReadUInt32();
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

    [MagicNumber("NISPACK\0", 0x00)]
    public class NISPACK : ContainerFormat
    {
        public string MagicNumber { get; private set; }
        public uint BigEndianFlag { get; private set; }
        public uint NumFiles { get; private set; }
        public NISPACKFile[] Files { get; private set; }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(8));
            BigEndianFlag = reader.ReadUInt32();

            reader.Endianness = (BigEndianFlag == 1 ? Endian.BigEndian : Endian.LittleEndian);

            NumFiles = reader.ReadUInt32();

            Files = new NISPACKFile[NumFiles];
            for (int i = 0; i < Files.Length; i++) Files[i] = new NISPACKFile(reader);
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
