using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Scarlet.IO.ContainerFormats
{
	// TODO: read and use additional data pointed to by filetype infos, verify *everything*...

	public class GARv5FileType
	{
		public uint NumFilesOfType { get; private set; }
		public uint Unknown0x04 { get; private set; }               // 0x00000004, 0x00000080, ...
		public int FirstFileOfType { get; private set; }
		public uint FileExtensionOffset { get; private set; }
		public int UnknownOffset0x10 { get; private set; }          // -1 or offset
		public uint Unknown0x14 { get; private set; }
		public uint Unknown0x18 { get; private set; }
		public uint Unknown0x1C { get; private set; }

		public string FileExtension { get; private set; }

		public GARv5FileType(EndianBinaryReader reader)
		{
			NumFilesOfType = reader.ReadUInt32();
			Unknown0x04 = reader.ReadUInt32();
			FirstFileOfType = reader.ReadInt32();
			FileExtensionOffset = reader.ReadUInt32();
			UnknownOffset0x10 = reader.ReadInt32();
			Unknown0x14 = reader.ReadUInt32();
			Unknown0x18 = reader.ReadUInt32();
			Unknown0x1C = reader.ReadUInt32();

			long position = reader.BaseStream.Position;

			reader.BaseStream.Seek(FileExtensionOffset, SeekOrigin.Begin);
			FileExtension = reader.ReadNullTerminatedString();

			reader.BaseStream.Seek(position, SeekOrigin.Begin);
		}
	}

	public class GARv5FileInfo : ContainerElement
	{
		public uint FileSize { get; private set; }
		public uint FileOffset { get; private set; }
		public uint FilenameOffset { get; private set; }
		public uint FullPathOffset { get; private set; }

		public string Filename { get; private set; }

		internal string CombinedPath { get; set; }

		public GARv5FileInfo(EndianBinaryReader reader)
		{
			FileSize = reader.ReadUInt32();
			FileOffset = reader.ReadUInt32();
			FilenameOffset = reader.ReadUInt32();
			FullPathOffset = reader.ReadUInt32();

			long position = reader.BaseStream.Position;

			reader.BaseStream.Seek(FullPathOffset != 0xFFFFFFFF ? FullPathOffset : FilenameOffset, SeekOrigin.Begin);
			Filename = reader.ReadNullTerminatedString();

			reader.BaseStream.Seek(position, SeekOrigin.Begin);
		}

		public override string GetName()
		{
			return CombinedPath;
		}

		public override Stream GetStream(Stream containerStream)
		{
			containerStream.Seek(FileOffset, SeekOrigin.Begin);
			MemoryStream stream = new MemoryStream();
			FileFormat.CopyStream(containerStream, stream, (int)FileSize);
			stream.Seek(0, SeekOrigin.Begin);
			return stream;
		}
	}

	[MagicNumber("GAR\x05", 0x00)]
	public class GARv5 : ContainerFormat
	{
		public string MagicNumber { get; private set; }
		public uint ArchiveSize { get; private set; }
		public ushort NumFileTypes { get; private set; }
		public ushort NumFiles { get; private set; }
		public uint FileTypesOffset { get; private set; }
		public uint FileInfoIndexOffset { get; private set; }
		public uint FileIndexOffset { get; private set; }
		public string CodenameString { get; private set; }

		public GARv5FileType[] FileTypes { get; private set; }
		public GARv5FileInfo[] FileInfos { get; private set; }

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
			FileTypes = new GARv5FileType[NumFileTypes];
			for (int i = 0; i < FileTypes.Length; i++) FileTypes[i] = new GARv5FileType(reader);

			reader.BaseStream.Seek(FileInfoIndexOffset, SeekOrigin.Begin);
			FileInfos = new GARv5FileInfo[NumFiles];
			for (int i = 0; i < FileInfos.Length; i++) FileInfos[i] = new GARv5FileInfo(reader);

			for (int i = 0; i < FileInfos.Length; i++)
			{
				GARv5FileInfo file = FileInfos[i];
				GARv5FileType type = FileTypes.FirstOrDefault(x => (i >= x.FirstFileOfType && i < (x.FirstFileOfType + x.NumFilesOfType)));

				string filename = (Path.GetExtension(file.Filename) != string.Empty ? file.Filename : $"{file.Filename}.{type.FileExtension}");
				string root = Path.GetPathRoot(file.Filename);

				if (root != string.Empty)
					file.CombinedPath = Path.Combine(root.Replace(":", string.Empty), filename.Replace(root, string.Empty));
				else
					file.CombinedPath = filename;
			}
		}

		public override int GetElementCount()
		{
			return NumFiles;
		}

		protected override ContainerElement GetElement(Stream containerStream, int elementIndex)
		{
			if (elementIndex < 0 || elementIndex >= FileInfos.Length) throw new IndexOutOfRangeException("Invalid file index specified");
			return FileInfos[elementIndex];
		}
	}
}
