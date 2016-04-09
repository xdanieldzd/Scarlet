using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scarlet.IO
{
    public class CommandLineArgsException : Exception
    {
        public string ExpectedArgs { get; private set; }

        public CommandLineArgsException(string expectedArgs) : base() { this.ExpectedArgs = expectedArgs; }
    }
}
