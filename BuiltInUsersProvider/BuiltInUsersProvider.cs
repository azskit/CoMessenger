using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleChat.Plugin;

namespace BuiltInUsersProvider
{
    public class BuiltInUsersProvider : IPlugin

    {
        public string Author
        {
            get
            {
                return "azskit";
            }
        }

        public string Name
        {
            get
            {
                return "BuiltInUsersProvider";
            }
        }

        public string Version
        {
            get
            {
                return "0.0.0.1";
            }
        }

        public object Data(Dictionary<string, string> query)
        {
            throw new NotImplementedException();
        }
    }
}
