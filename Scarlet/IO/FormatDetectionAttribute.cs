using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scarlet.IO
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class FormatDetectionAttribute : Attribute
    {
        public FormatDetectionDelegate FormatDetectionDelegate { get; set; }

        public FormatDetectionAttribute(Type formatClassType, string functionName)
        {
            FormatDetectionDelegate = (FormatDetectionDelegate)Delegate.CreateDelegate(typeof(FormatDetectionDelegate), formatClassType.GetMethod(functionName));
        }
    }

    public delegate bool FormatDetectionDelegate(EndianBinaryReader reader);
}
