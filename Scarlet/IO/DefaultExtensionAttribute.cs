using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scarlet.IO
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class DefaultExtensionAttribute : Attribute
    {
        public string Extension { get; private set; }

        public DefaultExtensionAttribute(string extension)
        {
            Extension = extension;
        }
    }
}
