using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleChat.Plugin
{
    public interface IPlugin
    {
        string Name { get; }
        string Version { get; }
        string Author { get; }
        object Data(Dictionary<string, string> query);
    }
}
