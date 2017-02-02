using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleChat;

namespace CimServerConsole
{
    class CimServerConsole
    {
        static void Main(string[] args)
        {
            Server.Instance.Start();

            Server.Instance.ProcessConsoleInput();

            Server.Instance.Stop();
        }
    }
}
