using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScarletWinTest
{
    public class FileFormatInfo
    {
        public string FormatDescription { get; private set; }
        public string FileExtension { get; private set; }
        public Type FormatInstanceType { get; private set; }

        public FileFormatInfo(string formatDescription, string fileExtension, Type formatInstanceType)
        {
            FormatDescription = formatDescription;
            FileExtension = fileExtension;
            FormatInstanceType = formatInstanceType;
        }
    }
}
