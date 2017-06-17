using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Scarlet.IO.ContainerFormats
{
    // http://pastebin.com/Dw7KRdSE by Twili

    public class ZARv1FileType
    {
        public uint NumberOfFilesWithType { get; private set; }
        public uint FileNumberIndexOffset { get; private set; }
        public uint FileTypeNameOffset { get; private set; }
        public uint AssumedConstant { get; private set; }

        public string FileTypeName { get; private set; }
        public uint[] FileNumberIndex { get; private set; }

        public ZARv1FileType(EndianBinaryReader reader)
        {
            NumberOfFilesWithType = reader.ReadUInt32();
            FileNumberIndexOffset = reader.ReadUInt32();
            FileTypeNameOffset = reader.ReadUInt32();
            AssumedConstant = reader.ReadUInt32();

            long position = reader.BaseStream.Position;

            reader.BaseStream.Seek(FileNumberIndexOffset, SeekOrigin.Begin);
            FileNumberIndex = new uint[NumberOfFilesWithType];
            for (int i = 0; i < FileNumberIndex.Length; i++) FileNumberIndex[i] = reader.ReadUInt32();

            reader.BaseStream.Seek(FileTypeNameOffset, SeekOrigin.Begin);
            FileTypeName = reader.ReadNullTerminatedString();

            reader.BaseStream.Seek(position, SeekOrigin.Begin);
        }
    }

    public class ZARv1FileInfo
    {
        public uint FileSize { get; private set; }
        public uint FilenameOffset { get; private set; }

        public string Filename { get; private set; }

        public ZARv1FileInfo(EndianBinaryReader reader)
        {
            FileSize = reader.ReadUInt32();
            FilenameOffset = reader.ReadUInt32();

            long position = reader.BaseStream.Position;

            reader.BaseStream.Seek(FilenameOffset, SeekOrigin.Begin);
            Filename = reader.ReadNullTerminatedString();

            reader.BaseStream.Seek(position, SeekOrigin.Begin);
        }
    }

    public class ZARv1File : ContainerElement
    {
        public string Filename { get; private set; }
        public uint Offset { get; private set; }
        public uint FileSize { get; private set; }

        public ZARv1File(string filename, uint offset, uint fileSize)
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

    [MagicNumber("ZAR\x01", 0x00)]
    public class ZARv1 : ContainerFormat
    {
        public string MagicNumber { get; private set; }
        public uint ArchiveSize { get; private set; }
        public ushort NumFileTypes { get; private set; }
        public ushort NumFiles { get; private set; }
        public uint FileTypesOffset { get; private set; }
        public uint FileInfoIndexOffset { get; private set; }
        public uint FileIndexOffset { get; private set; }
        public string CodenameString { get; private set; }

        public ZARv1FileType[] FileTypes { get; private set; }
        public ZARv1FileInfo[] FileInfos { get; private set; }
        public uint[] FileOffsets { get; private set; }

        ZARv1File[] files;

        protected override void OnOpen(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));
            ArchiveSize = reader.ReadUInt32();
            NumFileTypes = reader.ReadUInt16();
            NumFiles = reader.ReadUInt16();
            FileTypesOffset = reader.ReadUInt32();
            FileInfoIndexOffset = reader.ReadUInt32();
            FileIndexOffset = reader.ReadUInt32();
            CodenameString = Encoding.ASCII.GetString(reader.ReadBytes(8)).TrimEnd('\0');

            reader.BaseStream.Seek(FileTypesOffset, SeekOrigin.Begin);
            FileTypes = new ZARv1FileType[NumFileTypes];
            for (int i = 0; i < FileTypes.Length; i++) FileTypes[i] = new ZARv1FileType(reader);

            reader.BaseStream.Seek(FileInfoIndexOffset, SeekOrigin.Begin);
            FileInfos = new ZARv1FileInfo[NumFiles];
            for (int i = 0; i < FileInfos.Length; i++) FileInfos[i] = new ZARv1FileInfo(reader);

            reader.BaseStream.Seek(FileIndexOffset, SeekOrigin.Begin);
            FileOffsets = new uint[NumFiles];
            for (int i = 0; i < FileOffsets.Length; i++) FileOffsets[i] = reader.ReadUInt32();

            files = new ZARv1File[NumFiles];
            for (int i = 0; i < files.Length; i++) files[i] = new ZARv1File(FileInfos[i].Filename, FileOffsets[i], FileInfos[i].FileSize);
        }

        public override int GetElementCount()
        {
            return NumFiles;
        }

        protected override ContainerElement GetElement(Stream containerStream, int elementIndex)
        {
            if (elementIndex < 0 || elementIndex >= files.Length) throw new IndexOutOfRangeException("Invalid file index specified");
            return files[elementIndex];
        }
    }
}
