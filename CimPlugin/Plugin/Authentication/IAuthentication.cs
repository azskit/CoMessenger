using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CimPlugin.Plugin.Authentication
{
    public interface IAuthentication : IPlugin
    {
        IEnumerable<AuthenticationData> CollectUsers();
    }
}
