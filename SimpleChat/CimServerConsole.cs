using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleChat
{
    class CimServerConsole
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello world!");

            Server.Instance.Start();

        }
    }
}
