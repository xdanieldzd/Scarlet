using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

/* adapted from https://github.com/esperknight/CriPakTools */

namespace Scarlet.IO.ContainerFormats
{
    class Tools
    {
        public static byte[] GetData(BinaryReader br, long offset, int size)
        {
            byte[] result = null;
            long backup = br.BaseStream.Position;
            br.BaseStream.Seek(offset, SeekOrigin.Begin);
            result = br.ReadBytes(size);
            br.BaseStream.Seek(backup, SeekOrigin.Begin);
            return result;
        }

        public static string ReadCString(BinaryReader br, int maxLength = -1, long offset = -1, Encoding enc = null)
        {
            int max;
            if (maxLength == -1)
                max = 255;
            else
                max = maxLength;

            long basePos = br.BaseStream.Position;
            byte bTemp = 0;
            int i = 0;
            string result = "";

            if (offset > -1)
            {
                br.BaseStream.Seek(offset, SeekOrigin.Begin);
            }

            do
            {
                bTemp = br.ReadByte();
                if (bTemp == 0)
                    break;
                i += 1;
            } while (i < max);

            if (maxLength == -1)
                max = i + 1;
            else
                max = maxLength;

            if (offset > -1)
            {
                br.BaseStream.Seek(offset, SeekOrigin.Begin);

                if (enc == null)
                    result = Encoding.ASCII.GetString(br.ReadBytes(i));
                else
                    result = enc.GetString(br.ReadBytes(i));

                br.BaseStream.Seek(basePos, SeekOrigin.Begin);
            }
            else
            {
                br.BaseStream.Seek(basePos, SeekOrigin.Begin);
                if (enc == null)
                    result = Encoding.ASCII.GetString(br.ReadBytes(i));
                else
                    result = enc.GetString(br.ReadBytes(i));

                br.BaseStream.Seek(basePos + max, SeekOrigin.Begin);
            }

            return result;
        }
    }

    public class CPKEntry : ContainerElement
    {
        public object DirName { get; set; }
        public object FileName { get; set; }

        public object FileSize { get; set; }
        public long FileSizePos { get; set; }
        public Type FileSizeType { get; set; }

        public object ExtractSize { get; set; } // int
        public long ExtractSizePos { get; set; }
        public Type ExtractSizeType { get; set; }

        public ulong FileOffset { get; set; }
        public long FileOffsetPos { get; set; }
        public Type FileOffsetType { get; set; }


        public ulong Offset { get; set; }
        public object ID { get; set; } // int
        public object UserString { get; set; } // string
        public ulong UpdateDateTime { get; set; }
        public object LocalDir { get; set; } // string
        public string TOCName { get; set; }

        public bool Encrypted { get; set; }

        public string FileType { get; set; }

        public override Stream GetStream(Stream containerStream)
        {
            containerStream.Seek((long)FileOffset, SeekOrigin.Begin);
            MemoryStream stream = new MemoryStream();
            FileFormat.CopyStream(containerStream, stream, Int32.Parse(FileSize.ToString()));
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public override string GetName()
        {
            return String.Format("{0}{1}", DirName != null ? DirName.ToString() + "/" : "", FileName.ToString());
        }
    }

    [MagicNumber("CPK ", 0x00)]
    public class CPK : ContainerFormat
    {
        public List<CPKEntry> FileTable = new List<CPKEntry>();
        public CPKEntry[] AllFiles;
        public Dictionary<string, object> CPKData;
        public UTF UTF;

        public UTF Files;

        public bool IsUtfEncrypted { get; set; } = false;
        public int Unk1 { get; set; }
        public long UTFSize { get; set; }
        public byte[] UTFPacket { get; set; }

        public byte[] CPKPacket { get; set; }
        public byte[] TOCPacket { get; set; }
        public byte[] ITOCPacket { get; set; }
        public byte[] ETOCPacket { get; set; }
        public byte[] GTOCPacket { get; set; }

        public ulong TocOffset, EtocOffset, ItocOffset, GtocOffset, ContentOffset;

        public override int GetElementCount()
        {
            return AllFiles.Length;
        }

        protected override ContainerElement GetElement(Stream containerStream, int elementIndex)
        {
            return AllFiles[elementIndex];
        }

        public byte[] DecryptUTF(byte[] input)
        {
            byte[] result = new byte[input.Length];

            int m, t;
            byte d;

            m = 0x0000655f;
            t = 0x00004115;

            for (int i = 0; i < input.Length; i++)
            {
                d = input[i];
                d = (byte)(d ^ (byte)(m & 0xff));
                result[i] = d;
                m *= t;
            }

            return result;
        }

        void ReadUTFData(EndianBinaryReader reader)
        {
            IsUtfEncrypted = false;
            reader.Endianness = Endian.LittleEndian;

            Unk1 = reader.ReadInt32();
            UTFSize = reader.ReadInt64();
            UTFPacket = reader.ReadBytes((int)UTFSize);

            //if (UTFPacket[0] != 0x40 && UTFPacket[1] != 0x55 && UTFPacket[2] != 0x54 && UTFPacket[3] != 0x46) //@UTF
            if (Encoding.ASCII.GetString(UTFPacket, 0, 4) != "@UTF")
            {
                UTFPacket = DecryptUTF(UTFPacket);
                IsUtfEncrypted = true;
            }

            reader.Endianness = Endian.BigEndian;
        }

        public bool ReadTOC(EndianBinaryReader br, ulong tocOffset, ulong contentOffset)
        {
            ulong addOffset = 0;

            if (contentOffset < 0)
                addOffset = tocOffset;
            else
            {
                if (tocOffset < 0)
                    addOffset = contentOffset;
                else
                {
                    if (contentOffset < tocOffset)
                        addOffset = contentOffset;
                    else
                        addOffset = tocOffset;
                }
            }

            br.BaseStream.Seek((long)tocOffset, SeekOrigin.Begin);

            if (Tools.ReadCString(br, 4) != "TOC ")
            {
                br.Close();
                return false;
            }

            ReadUTFData(br);

            // Store unencrypted TOC
            TOCPacket = UTFPacket;

            CPKEntry tocEntry = FileTable.Where(x => x.FileName.ToString() == "TOC_HDR").Single();
            tocEntry.Encrypted = IsUtfEncrypted;
            tocEntry.FileSize = TOCPacket.Length;

            MemoryStream ms = new MemoryStream(UTFPacket);
            EndianBinaryReader utfr = new EndianBinaryReader(ms, Endian.BigEndian);

            Files = new UTF();
            if (!Files.ReadUTF(utfr))
            {
                br.Close();
                return false;
            }

            utfr.Close();
            ms.Close();

            CPKEntry temp;
            for (int i = 0; i < Files.NumRows; i++)
            {
                temp = new CPKEntry();

                temp.TOCName = "TOC";

                temp.DirName = GetColumnData(Files, i, "DirName");
                temp.FileName = GetColumnData(Files, i, "FileName");

                temp.FileSize = GetColumnData(Files, i, "FileSize");
                temp.FileSizePos = GetColumnPostion(Files, i, "FileSize");
                temp.FileSizeType = GetColumnType(Files, i, "FileSize");

                temp.ExtractSize = GetColumnData(Files, i, "ExtractSize");
                temp.ExtractSizePos = GetColumnPostion(Files, i, "ExtractSize");
                temp.ExtractSizeType = GetColumnType(Files, i, "ExtractSize");

                temp.FileOffset = ((ulong)GetColumnData(Files, i, "FileOffset") + (ulong)addOffset);
                temp.FileOffsetPos = GetColumnPostion(Files, i, "FileOffset");
                temp.FileOffsetType = GetColumnType(Files, i, "FileOffset");

                temp.FileType = "FILE";

                temp.Offset = addOffset;

                temp.ID = GetColumnData(Files, i, "ID");
                temp.UserString = GetColumnData(Files, i, "UserString");

                FileTable.Add(temp);
            }
            Files = null;

            return true;
        }

        public bool ReadETOC(EndianBinaryReader br, ulong startoffset)
        {
            br.BaseStream.Seek((long)startoffset, SeekOrigin.Begin);

            if (Tools.ReadCString(br, 4) != "ETOC")
            {
                br.Close();
                return false;
            }

            ReadUTFData(br);

            ETOCPacket = UTFPacket;

            CPKEntry etocEntry = FileTable.Where(x => x.FileName.ToString() == "ETOC_HDR").Single();
            etocEntry.Encrypted = IsUtfEncrypted;
            etocEntry.FileSize = ETOCPacket.Length;

            MemoryStream ms = new MemoryStream(UTFPacket);
            EndianBinaryReader utfr = new EndianBinaryReader(ms, Endian.BigEndian);

            Files = new UTF();
            if (!Files.ReadUTF(utfr))
            {
                br.Close();
                return false;
            }

            utfr.Close();
            ms.Close();

            List<CPKEntry> fileEntries = FileTable.Where(x => x.FileType == "FILE").ToList();

            for (int i = 0; i < fileEntries.Count; i++)
            {
                FileTable[i].LocalDir = GetColumnData(Files, i, "LocalDir");
                FileTable[i].UpdateDateTime = (ulong)GetColumnData(Files, i, "UpdateDateTime");
            }

            return true;
        }

        public bool ReadITOC(EndianBinaryReader br, ulong startoffset, ulong contentOffset, ushort align)
        {
            br.BaseStream.Seek((long)startoffset, SeekOrigin.Begin);

            if (Tools.ReadCString(br, 4) != "ITOC")
            {
                br.Close();
                return false;
            }

            ReadUTFData(br);

            ITOCPacket = UTFPacket;

            CPKEntry itocEntry = FileTable.Where(x => x.FileName.ToString() == "ITOC_HDR").Single();
            itocEntry.Encrypted = IsUtfEncrypted;
            itocEntry.FileSize = ITOCPacket.Length;

            MemoryStream ms = new MemoryStream(UTFPacket);
            EndianBinaryReader utfr = new EndianBinaryReader(ms, Endian.BigEndian);

            Files = new UTF();
            if (!Files.ReadUTF(utfr))
            {
                br.Close();
                return false;
            }

            utfr.Close();
            ms.Close();

            //uint FilesL = (uint)GetColumnData(files, 0, "FilesL");
            //uint FilesH = (uint)GetColumnData(files, 0, "FilesH");
            byte[] dataL = (byte[])GetColumnData(Files, 0, "DataL");
            long dataLPos = GetColumnPostion(Files, 0, "DataL");

            byte[] dataH = (byte[])GetColumnData(Files, 0, "DataH");
            long dataHPos = GetColumnPostion(Files, 0, "DataH");

            UTF utfDataL, utfDataH;
            Dictionary<int, uint> sizeTable, cSizeTable;
            Dictionary<int, long> sizePosTable, cSizePosTable;
            Dictionary<int, Type> sizeTypeTable, cSizeTypeTable;

            List<int> IDs = new List<int>();

            sizeTable = new Dictionary<int, uint>();
            sizePosTable = new Dictionary<int, long>();
            sizeTypeTable = new Dictionary<int, Type>();

            cSizeTable = new Dictionary<int, uint>();
            cSizePosTable = new Dictionary<int, long>();
            cSizeTypeTable = new Dictionary<int, Type>();

            ushort ID, size1;
            uint size2;
            long pos;
            Type type;

            if (dataL != null)
            {
                ms = new MemoryStream(dataL);
                utfr = new EndianBinaryReader(ms, Endian.BigEndian);
                utfDataL = new UTF();
                utfDataL.ReadUTF(utfr);

                for (int i = 0; i < utfDataL.NumRows; i++)
                {
                    ID = (ushort)GetColumnData(utfDataL, i, "ID");
                    size1 = (ushort)GetColumnData(utfDataL, i, "FileSize");
                    sizeTable.Add((int)ID, (uint)size1);

                    pos = GetColumnPostion(utfDataL, i, "FileSize");
                    sizePosTable.Add((int)ID, pos + dataLPos);

                    type = GetColumnType(utfDataL, i, "FileSize");
                    sizeTypeTable.Add((int)ID, type);

                    if ((GetColumnData(utfDataL, i, "ExtractSize")) != null)
                    {
                        size1 = (ushort)GetColumnData(utfDataL, i, "ExtractSize");
                        cSizeTable.Add((int)ID, (uint)size1);

                        pos = GetColumnPostion(utfDataL, i, "ExtractSize");
                        cSizePosTable.Add((int)ID, pos + dataLPos);

                        type = GetColumnType(utfDataL, i, "ExtractSize");
                        cSizeTypeTable.Add((int)ID, type);
                    }

                    IDs.Add(ID);
                }
            }

            if (dataH != null)
            {
                ms = new MemoryStream(dataH);
                utfr = new EndianBinaryReader(ms, Endian.BigEndian);
                utfDataH = new UTF();
                utfDataH.ReadUTF(utfr);

                for (int i = 0; i < utfDataH.NumRows; i++)
                {
                    ID = (ushort)GetColumnData(utfDataH, i, "ID");
                    size2 = (uint)GetColumnData(utfDataH, i, "FileSize");
                    sizeTable.Add(ID, size2);

                    pos = GetColumnPostion(utfDataH, i, "FileSize");
                    sizePosTable.Add((int)ID, pos + dataHPos);

                    type = GetColumnType(utfDataH, i, "FileSize");
                    sizeTypeTable.Add((int)ID, type);

                    if ((GetColumnData(utfDataH, i, "ExtractSize")) != null)
                    {
                        size2 = (uint)GetColumnData(utfDataH, i, "ExtractSize");
                        cSizeTable.Add(ID, size2);

                        pos = GetColumnPostion(utfDataH, i, "ExtractSize");
                        cSizePosTable.Add((int)ID, pos + dataHPos);

                        type = GetColumnType(utfDataH, i, "ExtractSize");
                        cSizeTypeTable.Add((int)ID, type);
                    }

                    IDs.Add(ID);
                }
            }

            CPKEntry temp;
            //int id = 0;
            uint value = 0, value2 = 0;
            ulong baseoffset = contentOffset;

            // Seems ITOC can mix up the IDs..... but they'll alwaysy be in order...
            IDs = IDs.OrderBy(x => x).ToList();

            for (int i = 0; i < IDs.Count; i++)
            {
                int id = IDs[i];

                temp = new CPKEntry();
                sizeTable.TryGetValue(id, out value);
                cSizeTable.TryGetValue(id, out value2);

                temp.TOCName = "ITOC";

                temp.DirName = null;
                temp.FileName = id.ToString("D4");

                temp.FileSize = value;
                temp.FileSizePos = sizePosTable[id];
                temp.FileSizeType = sizeTypeTable[id];

                if (cSizeTable.Count > 0 && cSizeTable.ContainsKey(id))
                {
                    temp.ExtractSize = value2;
                    temp.ExtractSizePos = cSizePosTable[id];
                    temp.ExtractSizeType = cSizeTypeTable[id];
                }

                temp.FileType = "FILE";

                temp.FileOffset = baseoffset;
                temp.ID = id;
                temp.UserString = null;

                FileTable.Add(temp);

                if ((value % align) > 0)
                    baseoffset += value + (align - (value % align));
                else
                    baseoffset += value;

                //id++;
            }

            Files = null;
            utfDataL = null;
            utfDataH = null;

            ms.Close();
            utfr.Close();

            return true;
        }

        public bool ReadGTOC(EndianBinaryReader br, ulong startoffset)
        {
            br.BaseStream.Seek((long)startoffset, SeekOrigin.Begin);

            if (Tools.ReadCString(br, 4) != "GTOC")
            {
                br.Close();
                return false;
            }

            br.BaseStream.Seek(0xC, SeekOrigin.Current); /* skip header data */

            return true;
        }

        public object GetColumsData2(UTF utf, int row, string name, int type)
        {
            object temp = GetColumnData(utf, row, name);

            if (temp == null)
            {
                switch (type)
                {
                    case 0: /* byte */
                        return (byte)0xFF;
                    case 1: /* short */
                        return (ushort)0xFFFF;
                    case 2: /* int */
                        return 0xFFFFFFFF;
                    case 3: /* long */
                        return 0xFFFFFFFFFFFFFFFF;
                }
            }

            if (temp is ulong)
            {
                return (temp == null) ? 0xFFFFFFFFFFFFFFFF : (ulong)temp;
            }

            if (temp is uint)
            {
                return (temp == null) ? 0xFFFFFFFF : (uint)temp;
            }

            if (temp is ushort)
            {
                return (temp == null) ? (ushort)0xFFFF : (ushort)temp;
            }

            return 0;
        }

        public object GetColumnData(UTF utf, int row, string name)
        {
            object result = null;

            for (int i = 0; i < utf.NumColumns; i++)
            {
                if (utf.Columns[i].name == name)
                {
                    result = utf.Rows[row].rows[i].GetValue();
                    break;
                }
            }

            return result;
        }

        public long GetColumnPostion(UTF utf, int row, string name)
        {
            long result = -1;

            for (int i = 0; i < utf.NumColumns; i++)
            {
                if (utf.Columns[i].name == name)
                {
                    result = utf.Rows[row].rows[i].Position;
                    break;
                }
            }

            return result;
        }

        public Type GetColumnType(UTF utf, int row, string name)
        {
            Type result = null;

            for (int i = 0; i < utf.NumColumns; i++)
            {
                if (utf.Columns[i].name == name)
                {
                    result = utf.Rows[row].rows[i].GetType();
                    break;
                }
            }

            return result;
        }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            uint files;
            ushort align;
            MemoryStream ms;
            EndianBinaryReader utfr;

            if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "CPK ")
                throw new NotImplementedException("Invalid CPK header");

            ReadUTFData(reader);

            CPKPacket = UTFPacket;

            CPKEntry CPAK_entry = new CPKEntry
            {
                FileName = "CPK_HDR",
                FileOffsetPos = reader.BaseStream.Position + 0x10,
                FileSize = CPKPacket.Length,
                Encrypted = IsUtfEncrypted,
                FileType = "CPK"
            };

            FileTable.Add(CPAK_entry);

            ms = new MemoryStream(UTFPacket);
            utfr = new EndianBinaryReader(ms, Endian.BigEndian);

            UTF = new UTF();
            if (!UTF.ReadUTF(utfr))
                throw new NotSupportedException("Invalid UTF header in CPK file");

            utfr.Close();
            ms.Close();

            CPKData = new Dictionary<string, object>();

            for (int i = 0; i < UTF.Columns.Count; i++)
            {
                CPKData.Add(UTF.Columns[i].name, UTF.Rows[0].rows[i].GetValue());
            }

            TocOffset = (ulong)GetColumsData2(UTF, 0, "TocOffset", 3);
            long tocOffsetPos = GetColumnPostion(UTF, 0, "TocOffset");

            EtocOffset = (ulong)GetColumsData2(UTF, 0, "EtocOffset", 3);
            long eTocOffsetPos = GetColumnPostion(UTF, 0, "EtocOffset");

            ItocOffset = (ulong)GetColumsData2(UTF, 0, "ItocOffset", 3);
            long iTocOffsetPos = GetColumnPostion(UTF, 0, "ItocOffset");

            GtocOffset = (ulong)GetColumsData2(UTF, 0, "GtocOffset", 3);
            long gTocOffsetPos = GetColumnPostion(UTF, 0, "GtocOffset");

            ContentOffset = (ulong)GetColumsData2(UTF, 0, "ContentOffset", 3);
            long contentOffsetPos = GetColumnPostion(UTF, 0, "ContentOffset");
            //FileTable.Add(CreateFileEntry("CONTENT_OFFSET", ContentOffset, typeof(ulong), ContentOffsetPos, "CPK", "CONTENT", false));
            FileTable.Add(new CPKEntry { FileName = "CONTENT_OFFSET", FileOffset = ContentOffset, FileOffsetType = typeof(ulong), FileOffsetPos = contentOffsetPos, TOCName = "CPK", FileType = "CONTENT", Encrypted = false });

            files = (uint)GetColumsData2(UTF, 0, "Files", 2);
            align = (ushort)GetColumsData2(UTF, 0, "Align", 1);

            if (TocOffset != 0xFFFFFFFFFFFFFFFF)
            {
                CPKEntry entry = new CPKEntry { FileName = "TOC_HDR", FileOffset = TocOffset, FileOffsetType = typeof(ulong), FileOffsetPos = tocOffsetPos, TOCName = "CPK", FileType = "HDR", Encrypted = false };
                FileTable.Add(entry);

                if (!ReadTOC(reader, TocOffset, ContentOffset))
                    throw new NotSupportedException("Error reading TOC from CPK file");
            }

            if (EtocOffset != 0xFFFFFFFFFFFFFFFF)
            {
                CPKEntry entry = new CPKEntry { FileName = "ETOC_HDR", FileOffset = EtocOffset, FileOffsetType = typeof(ulong), FileOffsetPos = eTocOffsetPos, TOCName = "CPK", FileType = "HDR", Encrypted = false };
                FileTable.Add(entry);

                if (!ReadETOC(reader, EtocOffset))
                    throw new NotSupportedException("Error reading ETOC from CPK file");
            }

            if (ItocOffset != 0xFFFFFFFFFFFFFFFF)
            {
                //FileEntry ITOC_entry = new FileEntry { 
                //    FileName = "ITOC_HDR",
                //    FileOffset = ItocOffset, FileOffsetType = typeof(ulong), FileOffsetPos = ITocOffsetPos,
                //    TOCName = "CPK",
                //    FileType = "FILE", Encrypted = true,
                //};

                CPKEntry entry = new CPKEntry { FileName = "ITOC_HDR", FileOffset = ItocOffset, FileOffsetType = typeof(ulong), FileOffsetPos = iTocOffsetPos, TOCName = "CPK", FileType = "HDR", Encrypted = false };
                FileTable.Add(entry);

                if (!ReadITOC(reader, ItocOffset, ContentOffset, align))
                    throw new NotSupportedException("Error reading ITOC from CPK file");
            }

            if (GtocOffset != 0xFFFFFFFFFFFFFFFF)
            {
                CPKEntry entry = new CPKEntry { FileName = "GTOC_HDR", FileOffset = GtocOffset, FileOffsetType = typeof(ulong), FileOffsetPos = gTocOffsetPos, TOCName = "CPK", FileType = "HDR", Encrypted = false };
                FileTable.Add(entry);

                if (!ReadGTOC(reader, GtocOffset))
                    throw new NotSupportedException("Error reading GTOC from CPK file");
            }

            // to get to the real files quickly
            AllFiles = FileTable.Where(x => x.FileType == "FILE").ToArray();
        }
    }

    public class UTF
    {
        public enum COLUMN_FLAGS : int
        {
            STORAGE_MASK = 0xf0,
            STORAGE_NONE = 0x00,
            STORAGE_ZERO = 0x10,
            STORAGE_CONSTANT = 0x30,
            STORAGE_PERROW = 0x50,


            TYPE_MASK = 0x0f,
            TYPE_DATA = 0x0b,
            TYPE_STRING = 0x0a,
            TYPE_FLOAT = 0x08,
            TYPE_8BYTE2 = 0x07,
            TYPE_8BYTE = 0x06,
            TYPE_4BYTE2 = 0x05,
            TYPE_4BYTE = 0x04,
            TYPE_2BYTE2 = 0x03,
            TYPE_2BYTE = 0x02,
            TYPE_1BYTE2 = 0x01,
            TYPE_1BYTE = 0x00,
        }

        public List<CPKColumn> Columns;
        public List<CPKRows> Rows;

        public UTF() { }

        public bool ReadUTF(EndianBinaryReader br)
        {
            long offset = br.BaseStream.Position;

            if (Tools.ReadCString(br, 4) != "@UTF")
            {
                return false;
            }

            TableSize = br.ReadInt32();
            RowsOffset = br.ReadInt32();
            StringsOffset = br.ReadInt32();
            DataOffset = br.ReadInt32();

            // CPK Header & UTF Header are ignored, so add 8 to each offset
            RowsOffset += (offset + 8);
            StringsOffset += (offset + 8);
            DataOffset += (offset + 8);

            TableName = br.ReadInt32();
            NumColumns = br.ReadInt16();
            RowLength = br.ReadInt16();
            NumRows = br.ReadInt32();

            //read Columns
            Columns = new List<CPKColumn>();
            CPKColumn column;

            for (int i = 0; i < NumColumns; i++)
            {
                column = new CPKColumn();
                column.flags = br.ReadByte();
                if (column.flags == 0)
                {
                    br.BaseStream.Seek(3, SeekOrigin.Current);
                    column.flags = br.ReadByte();
                }

                column.name = Tools.ReadCString(br, -1, (long)(br.ReadInt32() + StringsOffset));
                Columns.Add(column);
            }

            //read Rows

            Rows = new List<CPKRows>();
            CPKRows currentEntry;
            CPKRow currentRow;
            int storageFlag;

            for (int j = 0; j < NumRows; j++)
            {
                br.BaseStream.Seek(RowsOffset + (j * RowLength), SeekOrigin.Begin);

                currentEntry = new CPKRows();

                for (int i = 0; i < NumColumns; i++)
                {
                    currentRow = new CPKRow();

                    storageFlag = (Columns[i].flags & (int)COLUMN_FLAGS.STORAGE_MASK);

                    if (storageFlag == (int)COLUMN_FLAGS.STORAGE_NONE) /* 0x00 */
                    {
                        currentEntry.rows.Add(currentRow);
                        continue;
                    }

                    if (storageFlag == (int)COLUMN_FLAGS.STORAGE_ZERO) /* 0x10 */
                    {
                        currentEntry.rows.Add(currentRow);
                        continue;
                    }

                    if (storageFlag == (int)COLUMN_FLAGS.STORAGE_CONSTANT) /* 0x30 */
                    {
                        currentEntry.rows.Add(currentRow);
                        continue;
                    }

                    /* 0x50 */

                    currentRow.Type = Columns[i].flags & (int)COLUMN_FLAGS.TYPE_MASK;

                    currentRow.Position = br.BaseStream.Position;

                    switch (currentRow.Type)
                    {
                        case 0:
                        case 1:
                            currentRow.UInt8 = br.ReadByte();
                            break;

                        case 2:
                        case 3:
                            currentRow.UInt16 = br.ReadUInt16();
                            break;

                        case 4:
                        case 5:
                            currentRow.UInt32 = br.ReadUInt32();
                            break;

                        case 6:
                        case 7:
                            currentRow.UInt64 = br.ReadUInt64();
                            break;

                        case 8:
                            currentRow.UFloat = br.ReadSingle();
                            break;

                        case 0xA:
                            currentRow.Str = Tools.ReadCString(br, -1, br.ReadInt32() + StringsOffset);
                            break;

                        case 0xB:
                            long position = br.ReadInt32() + DataOffset;
                            currentRow.Position = position;
                            currentRow.Data = Tools.GetData(br, position, br.ReadInt32());
                            break;

                        default: throw new NotImplementedException();
                    }


                    currentEntry.rows.Add(currentRow);
                }

                Rows.Add(currentEntry);
            }

            return true;
        }

        public int TableSize { get; set; }

        public long RowsOffset { get; set; }
        public long StringsOffset { get; set; }
        public long DataOffset { get; set; }
        public int TableName { get; set; }
        public short NumColumns { get; set; }
        public short RowLength { get; set; }
        public int NumRows { get; set; }
    }

    public class CPKColumn
    {
        public CPKColumn()
        {
        }

        public byte flags { get; set; }
        public string name { get; set; }
    }

    public class CPKRows
    {
        public List<CPKRow> rows;

        public CPKRows()
        {
            rows = new List<CPKRow>();
        }
    }

    public class CPKRow
    {
        public CPKRow()
        {
            Type = -1;
        }

        public int Type { get; set; }

        public object GetValue()
        {
            object result = -1;

            switch (this.Type)
            {
                case 0:
                case 1: return this.UInt8;

                case 2:
                case 3: return this.UInt16;

                case 4:
                case 5: return this.UInt32;

                case 6:
                case 7: return this.UInt64;

                case 8: return this.UFloat;

                case 0xA: return this.Str;

                case 0xB: return this.Data;

                default: return null;
            }
        }

        public new Type GetType()
        {
            object result = -1;

            switch (this.Type)
            {
                case 0:
                case 1: return this.UInt8.GetType();

                case 2:
                case 3: return this.UInt16.GetType();

                case 4:
                case 5: return this.UInt32.GetType();

                case 6:
                case 7: return this.UInt64.GetType();

                case 8: return this.UFloat.GetType();

                case 0xA: return this.Str.GetType();

                case 0xB: return this.Data.GetType();

                default: return null;
            }
        }

        //column based datatypes
        public byte UInt8 { get; set; }
        public ushort UInt16 { get; set; }
        public uint UInt32 { get; set; }
        public ulong UInt64 { get; set; }
        public float UFloat { get; set; }
        public string Str { get; set; }
        public byte[] Data { get; set; }
        public long Position { get; set; }
    }
}
