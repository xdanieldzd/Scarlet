using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Scarlet.IO.ContainerFormats
{
    public class P5BustupBINFile : ContainerElement
    {
        public string Filename { get; private set; }
        public uint FileSize { get; private set; }

        long offset;

        public P5BustupBINFile(EndianBinaryReader reader)
        {
            Filename = Encoding.ASCII.GetString(reader.ReadBytes(0x20)).TrimEnd('\0').TrimEnd(' ');
            FileSize = reader.ReadUInt32();

            offset = reader.BaseStream.Position;
        }

        public override string GetName()
        {
            return Filename;
        }

        public override Stream GetStream(Stream containerStream)
        {
            containerStream.Seek(offset, SeekOrigin.Begin);
            MemoryStream stream = new MemoryStream();
            FileFormat.CopyStream(containerStream, stream, (int)FileSize);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
    }

    // TODO pretty bad pattern, but I'm not sure if I can make this any better? no magic numbers or anything
    [FilenamePattern(@"^b.*\.bin$")]
    [FilenamePattern(@"^b.*\.dds2$")]
    public class P5BustupBIN : ContainerFormat
    {
        public uint NumFiles { get; private set; }

        public P5BustupBINFile[] Files { get; private set; }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            reader.Endianness = Endian.BigEndian;

            NumFiles = reader.ReadUInt32();

            Files = new P5BustupBINFile[NumFiles];
            for (int i = 0; i < Files.Length; i++)
            {
                Files[i] = new P5BustupBINFile(reader);
                reader.BaseStream.Position += Files[i].FileSize;
            }
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
