using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Scarlet.IO.ContainerFormats
{
    // https://github.com/ShimmerFairy/MM3D/blob/master/src/gar.cpp

    public class GARv2FileType : ZARv1FileType
    {
        public GARv2FileType(EndianBinaryReader reader) : base(reader) { }
    }

    public class GARv2FileInfo
    {
        public uint FileSize { get; private set; }
        public uint FilenameOffset { get; private set; }
        public uint FullPathOffset { get; private set; }

        public string Filename { get; private set; }

        public GARv2FileInfo(EndianBinaryReader reader)
        {
            FileSize = reader.ReadUInt32();
            FilenameOffset = reader.ReadUInt32();
            FullPathOffset = reader.ReadUInt32();

            long position = reader.BaseStream.Position;

            reader.BaseStream.Seek(FullPathOffset, SeekOrigin.Begin);
            Filename = reader.ReadNullTerminatedString();

            reader.BaseStream.Seek(position, SeekOrigin.Begin);
        }
    }

    public class GARv2File : ZARv1File
    {
        public GARv2File(string filename, uint offset, uint fileSize) : base(filename, offset, fileSize) { }
    }

    [MagicNumber("GAR\x02", 0x00)]
    public class GARv2 : ContainerFormat
    {
        public string MagicNumber { get; private set; }
        public uint ArchiveSize { get; private set; }
        public ushort NumFileTypes { get; private set; }
        public ushort NumFiles { get; private set; }
        public uint FileTypesOffset { get; private set; }
        public uint FileInfoIndexOffset { get; private set; }
        public uint FileIndexOffset { get; private set; }
        public string CodenameString { get; private set; }

        public GARv2FileType[] FileTypes { get; private set; }
        public GARv2FileInfo[] FileInfos { get; private set; }
        public uint[] FileOffsets { get; private set; }

        GARv2File[] files;

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
            FileTypes = new GARv2FileType[NumFileTypes];
            for (int i = 0; i < FileTypes.Length; i++) FileTypes[i] = new GARv2FileType(reader);

            reader.BaseStream.Seek(FileInfoIndexOffset, SeekOrigin.Begin);
            FileInfos = new GARv2FileInfo[NumFiles];
            for (int i = 0; i < FileInfos.Length; i++) FileInfos[i] = new GARv2FileInfo(reader);

            reader.BaseStream.Seek(FileIndexOffset, SeekOrigin.Begin);
            FileOffsets = new uint[NumFiles];
            for (int i = 0; i < FileOffsets.Length; i++) FileOffsets[i] = reader.ReadUInt32();

            files = new GARv2File[NumFiles];
            for (int i = 0; i < files.Length; i++) files[i] = new GARv2File(FileInfos[i].Filename, FileOffsets[i], FileInfos[i].FileSize);
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
