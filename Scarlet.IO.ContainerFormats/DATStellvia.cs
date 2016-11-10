using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Scarlet.IO.ContainerFormats
{
    public class DATStellviaFile : ContainerElement
    {
        DATStellvia parentContainer;

        public uint SectorNumber { get; private set; }
        public uint DataSize { get; private set; }
        public string FilePath { get; private set; }

        public DATStellviaFile(EndianBinaryReader reader, DATStellvia container)
        {
            parentContainer = container;

            SectorNumber = reader.ReadUInt32();
            DataSize = reader.ReadUInt32();
            FilePath = Encoding.GetEncoding(932).GetString(reader.ReadBytes(0x38)).TrimEnd('\0');
        }

        public override string GetName()
        {
            return FilePath;
        }

        public override Stream GetStream(Stream containerStream)
        {
            containerStream.Seek(SectorNumber * 0x800, SeekOrigin.Begin);
            MemoryStream stream = new MemoryStream();
            FileFormat.CopyStream(containerStream, stream, (int)DataSize);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
    }

    // TODO: horrible horrible pattern, but, uh, not much else I can do, really...
    [FilenamePattern("^.*data\\d+\\.dat$")]
    public class DATStellvia : ContainerFormat
    {
        public DATStellviaFile[] Files { get; private set; }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            reader.Endianness = Endian.LittleEndian;

            List<DATStellviaFile> files = new List<DATStellviaFile>();
            uint check = uint.MaxValue;
            while (check != 0)
            {
                check = reader.ReadUInt32();
                reader.BaseStream.Seek(-4, SeekOrigin.Current);
                if (check == 0) break;
                files.Add(new DATStellviaFile(reader, this));
            }
            Files = files.ToArray();
        }

        public override int GetElementCount()
        {
            return (int)Files.Length;
        }

        protected override ContainerElement GetElement(Stream containerStream, int elementIndex)
        {
            if (elementIndex < 0 || elementIndex >= Files.Length) throw new IndexOutOfRangeException("Invalid file index specified");
            return Files[elementIndex];
        }
    }
}
