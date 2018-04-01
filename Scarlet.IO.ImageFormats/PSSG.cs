using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

using Scarlet.Drawing;
using Scarlet.IO;

namespace Scarlet.IO.ImageFormats
{
    /* Note/TODO: PSSG is actually a 3D model format but it is used for texture-only data in many games ... */

    public class PSSGAttributeInfo
    {
        public Int32 ID { get; private set; }
        public string Name { get; private set; }

        public PSSGAttributeInfo(EndianBinaryReader br)
        {
            int nameLength;

            ID = br.ReadInt32();
            nameLength = br.ReadInt32();
            Name = Encoding.ASCII.GetString(br.ReadBytes(nameLength));
        }

        public override string ToString()
        {
            return String.Format("Attribute 0x{0:x8}: {1}", ID, Name);
        }
    }

    public class PSSGNodeInfo
    {
        public Int32 ID { get; private set; }
        public String Name { get; private set; }
        public Dictionary<int, PSSGAttributeInfo> AttributeInfo { get; private set; }

        public PSSGNodeInfo(EndianBinaryReader br)
        {
            AttributeInfo = new Dictionary<int, PSSGAttributeInfo>();
            int nameLength;

            ID = br.ReadInt32();
            nameLength = br.ReadInt32();
            Name = Encoding.ASCII.GetString(br.ReadBytes(nameLength));
            int attributeInfoCount = br.ReadInt32();
            for (int i = 0; i < attributeInfoCount; i++)
            {
                PSSGAttributeInfo ai = new PSSGAttributeInfo(br);
                AttributeInfo.Add(ai.ID, ai);
            }
        }

        public override string ToString()
        {
            return String.Format("Node {0:x8}: {1}", ID, Name);
        }
    }

    public class PSSGAttribute
    {
        public Int32 ID { get; private set; }
        public object Data { get; private set; }
        public string Value
        {
            get
            {
                if (Data is Int32)
                {
                    return ((int)Data).ToString();
                }
                else if (Data is string)
                {
                    return (string)Data;
                }

                return "(data)";
            }
        }

        public string Name
        {
            get
            {
                return pssgFile.AttributeInfo[ID - 1].Name;
            }
        }

        public override string ToString()
        {
            return String.Format("Attribute 0x{0:x8}: {1} = {2}", ID, Name, Value);
        }

        private PSSG pssgFile;

        public PSSGAttribute(EndianBinaryReader br, PSSG _pssgFile)
        {
            int size;

            /* to get the name of the attribute we need the global attribute info table */
            pssgFile = _pssgFile;

            ID = br.ReadInt32();
            size = br.ReadInt32();
            if (size == 4)
            {
                Data = br.ReadInt32();
                return;
            }
            else if (size > 4)
            {
                int len = br.ReadInt32();
                if (size - 4 == len)
                {
                    Data = Encoding.ASCII.GetString(br.ReadBytes(len));
                    return;
                }
                else
                {
                    br.BaseStream.Seek(-4, SeekOrigin.Current);
                }
            }
            Data = br.ReadBytes(size);
        }
    }

    public class PSSGNode
    {
        public Int32 ID { get; private set; }
        public Dictionary<string, PSSGAttribute> Attributes { get; private set; }
        public PSSGNode[] SubNodes { get; private set; }
        public bool IsDataNode
        {
            get
            {
                // TODO: this is a WIP check... let's see how it works out. The original has a few names hardcoded that are known data nodes:
                string[] knownDataNodes = new string[] {"BOUNDINGBOX", "DATA", "DATABLOCKDATA", "DATABLOCKBUFFERED", "INDEXSOURCEDATA",
                    "INVERSEBINDMATRIX", "MODIFIERNETWORKINSTANCEUNIQUEMODIFIERINPUT", "NeAnimPacketData_B1", "NeAnimPacketData_B4",
                    "RENDERINTERFACEBOUNDBUFFERED", "SHADERINPUT", "TEXTUREIMAGEBLOCKDATA", "TRANSFORM" };

                // instead, we do this check:
                return Attributes.Count == 0;
            }
        }
        public byte[] Data { get; private set; }
        public string Name
        {
            get
            {
                return pssgFile.NodeInfo[ID - 1].Name;
            }
        }

        /* for the name of the node */
        private PSSG pssgFile;

        public PSSGNode(EndianBinaryReader br, PSSG _pssgFile)
        {
            pssgFile = _pssgFile;

            ID = br.ReadInt32();
            int size = br.ReadInt32();
            long end = br.BaseStream.Position + size;

            int attributeSize = br.ReadInt32();
            long attributeEnd = br.BaseStream.Position + attributeSize;

            /* read in the attributes */
            Attributes = new Dictionary<string, PSSGAttribute>();
            while (br.BaseStream.Position < attributeEnd)
            {
                PSSGAttribute attr = new PSSGAttribute(br, pssgFile);
                Attributes.Add(attr.Name, attr);
            }

            if (IsDataNode)
            {
                Data = br.ReadBytes((int)(end - br.BaseStream.Position));
            }
            else
            {
                List<PSSGNode> tempSubNodes = new List<PSSGNode>();
                while (br.BaseStream.Position < end)
                {
                    tempSubNodes.Add(new PSSGNode(br, pssgFile));
                }
                SubNodes = tempSubNodes.ToArray();
            }
        }

        public List<PSSGNode> FindNodes(string nodeName)
        {
            return FindNodes(nodeName, null, null);
        }

        public List<PSSGNode> FindNodes(string nodeName, string attributeName, string attributeValue)
        {
            List<PSSGNode> results = new List<PSSGNode>();

            if (this.Name == nodeName)
            {
                /* are we looking for a node with a particular attribute=value pair? */
                if (attributeName != null && attributeValue != null)
                {
                    if (Attributes.ContainsKey(attributeName) && Attributes[attributeName].Value == attributeValue)
                    {
                        results.Add(this);
                    }
                }
                else
                {
                    results.Add(this);
                }
            }

            if (SubNodes != null)
            {
                foreach(PSSGNode sn in SubNodes)
                {
                    results.AddRange(sn.FindNodes(nodeName, attributeName, attributeValue));
                }
            }

            return results;
        }

        public override string ToString()
        {
            return String.Format("Node 0x{0:x8}: {1}", ID, Name);
        }
    }

    [MagicNumber("PSSG", 0x00)]
    public class PSSG : ImageFormat
    {
        public string MagicNumber { get; private set; }
        public Int32 FileSize { get; private set; }
        public Int32 AttributeInfoCount { get; private set; }
        public Int32 NodeInfoCount { get; private set; }
        public PSSGAttributeInfo[] AttributeInfo { get; private set; }
        public PSSGNodeInfo[] NodeInfo { get; private set; }
        public PSSGNode RootNode { get; private set; }

        protected override Bitmap OnGetBitmap(int imageIndex, int paletteIndex)
        {
            List<PSSGNode> allTextures = RootNode.FindNodes("TEXTURE");
            PSSGNode texNode = allTextures[imageIndex];
            Int32 width = Convert.ToInt32(texNode.Attributes["width"].Value);
            Int32 height = Convert.ToInt32(texNode.Attributes["height"].Value);
            Int32 numBlocks = Convert.ToInt32(texNode.Attributes["imageBlockCount"].Value);
            string texelFormat = texNode.Attributes["texelFormat"].Value;
            PixelDataFormat pixelFormat = PixelDataFormat.Undefined;
            bool flipY = false;
            List<PSSGNode> texImageBlocks = texNode.FindNodes("TEXTUREIMAGEBLOCK");

            switch (texelFormat)
            {
                case "ui8x4":
                    pixelFormat = PixelDataFormat.FormatArgb8888 | PixelDataFormat.PixelOrderingLinear;
                    flipY = true;
                    break;
                case "u8x4":
                    pixelFormat = PixelDataFormat.FormatAbgr8888 | PixelDataFormat.PixelOrderingLinear;
                    flipY = true;
                    break;
                case "dxt1":
                    pixelFormat = PixelDataFormat.FormatDXT1Rgba;
                    flipY = true;
                    break;
                case "dxt5":
                    pixelFormat = PixelDataFormat.FormatDXT5;
                    flipY = true;
                    break;
                default:
                    throw new NotSupportedException(String.Format("Unsupported PSSG texel Format: {0}", texelFormat));
            }

            /* find out how many raw data blocks there are */
            if (numBlocks > 1)
            {
                throw new NotSupportedException("Loading PSSG cube maps is not yet supported");
            }
            else
            {
                /* we only have a single block. use that */
                ImageBinary imgbin = new ImageBinary();
                imgbin.Width = width;
                imgbin.Height = height;
                imgbin.InputPixelFormat = pixelFormat;
                imgbin.InputEndianness = Endian.LittleEndian;
                imgbin.AddInputPixels(texImageBlocks[0].FindNodes("TEXTUREIMAGEBLOCKDATA")[0].Data);
                Bitmap bmp = imgbin.GetBitmap();
                if (flipY)
                {
                    bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                }
                return bmp;
            }
        }

        protected override void OnOpen(EndianBinaryReader reader)
        {
            MagicNumber = Encoding.ASCII.GetString(reader.ReadBytes(4));

            reader.Endianness = Endian.BigEndian;
            FileSize = reader.ReadInt32();
            AttributeInfoCount = reader.ReadInt32();
            NodeInfoCount = reader.ReadInt32();

            AttributeInfo = new PSSGAttributeInfo[AttributeInfoCount];
            NodeInfo = new PSSGNodeInfo[NodeInfoCount];

            for (int i = 0; i < NodeInfoCount; i++)
            {
                NodeInfo[i] = new PSSGNodeInfo(reader);

                foreach (PSSGAttributeInfo ai in NodeInfo[i].AttributeInfo.Values)
                {
                    AttributeInfo[ai.ID - 1] = ai;
                }
            }

            RootNode = new PSSGNode(reader, this);
        }

        public override int GetImageCount()
        {
            return RootNode.FindNodes("TEXTURE").Count;
        }

        public override int GetPaletteCount()
        {
            return 0;
        }
    }
}
