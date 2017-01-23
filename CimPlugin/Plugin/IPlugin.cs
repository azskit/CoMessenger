using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;

namespace CimPlugin.Plugin
{
    public interface IPlugin
    {

        string Name { get; }
        string Version { get; }
        string Author { get; }

        TextWriter WarningStream { get; set; }
        TextWriter ErrorStream { get; set; }
        TextWriter InfoStream { get; set; }
    }


}
