using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Scarlet.IO.ContainerFormats
{
    internal class BasicFileInfo
    {
        public ushort Index { get; private set; }
        public uint StartAddress { get; private set; }
        public uint FileSize { get; private set; }

        public BasicFileInfo(BinaryReader reader)
        {
            Index = reader.ReadUInt16();
            StartAddress = reader.ReadUInt32();
            FileSize = reader.ReadUInt32();
        }
    }

    internal class NamedFileInfo
    {
        public ushort FileNameLength { get; private set; }
        public uint StartAddress { get; private set; }
        public uint FileSize { get; private set; }
        public string FileName { get; private set; }
        public byte NullByte { get; private set; }

        public NamedFileInfo(BinaryReader reader)
        {
            FileNameLength = reader.ReadUInt16();
            StartAddress = reader.ReadUInt32();
            FileSize = reader.ReadUInt32();
            FileName = Encoding.GetEncoding(932).GetString(reader.ReadBytes((int)FileNameLength));
            NullByte = reader.ReadByte();
        }
    }

    public class NSACEntry : ContainerElement
    {
        internal ushort Index { get; private set; }

        uint startAddress, fileSize;
        string filename;

        public NSACEntry(ushort index, uint startAddress, uint fileSize, string filename)
        {
            this.Index = index;
            this.startAddress = startAddress;
            this.fileSize = fileSize;
            this.filename = filename;
        }

        public override string GetName()
        {
            return filename;
        }

        public override Stream GetStream(Stream containerStream)
        {
            containerStream.Seek(startAddress, SeekOrigin.Begin);
            MemoryStream stream = new MemoryStream();
            FileFormat.CopyStream(containerStream, stream, (int)fileSize);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }
    }

    [MagicNumber("NSAC", 0x00)]
    public class NSAC : ContainerFormat
    {
        public string MagicNumber { get; private set; }
        public ushort Unknown0x04 { get; private set; } //revision?
        public ushort NumFiles { get; private set; }
        public uint ArchiveSize { get; private set; }
        public uint DataStartAddress { get; private set; }
        public uint BasicFileInfoSize { get; private set; }
        public uint NamedFileInfoSize { get; private set; }
        public uint Unknown0x18 { get; private set; } //padding?

        internal BasicFileInfo[] BasicFileInfos { get; private set; }
        internal NamedFileInfo[] NamedFileInfos { get; private set; }

        public NSACEntry[] Entries { get; private set; }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            Unknown0x04 = reader.ReadUInt16();
            NumFiles = reader.ReadUInt16();
            ArchiveSize = reader.ReadUInt32();
            DataStartAddress = reader.ReadUInt32();
            BasicFileInfoSize = reader.ReadUInt32();
            NamedFileInfoSize = reader.ReadUInt32();
            Unknown0x18 = reader.ReadUInt32();

            BasicFileInfos = new BasicFileInfo[NumFiles];
            for (int i = 0; i < BasicFileInfos.Length; i++) BasicFileInfos[i] = new BasicFileInfo(reader);

            NamedFileInfos = new NamedFileInfo[NumFiles];
            for (int i = 0; i < NamedFileInfos.Length; i++) NamedFileInfos[i] = new NamedFileInfo(reader);

            Entries = new NSACEntry[NumFiles];
            for (int i = 0; i < Entries.Length; i++)
                Entries[i] = new NSACEntry(BasicFileInfos[i].Index, NamedFileInfos[i].StartAddress, NamedFileInfos[i].FileSize, NamedFileInfos[i].FileName);
        }

        public override int GetElementCount()
        {
            return (int)NumFiles;
        }

        protected override ContainerElement GetElement(Stream containerStream, int elementIndex)
        {
            var element = Entries.First(x => x.Index == elementIndex);

            if (element == null)
                throw new IndexOutOfRangeException("Invalid file index specified");

            return element;
        }
    }
}
