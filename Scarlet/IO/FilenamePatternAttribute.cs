using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scarlet.IO
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class FilenamePatternAttribute : Attribute
    {
        public string Pattern { get; private set; }

        public FilenamePatternAttribute(string pattern)
        {
            Pattern = pattern;
        }
    }
}
