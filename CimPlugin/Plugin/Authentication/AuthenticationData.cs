using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;

namespace CimPlugin.Plugin.Authentication
{

    public abstract class AuthenticationData
    {
        /// <summary>
        /// Displayed user name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Unique user identificater
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Unique user name for authentication
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// User domain name (optional)
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Check user password, specified by string
        /// </summary>
        /// <param name="password">entered user password specified by string</param>
        /// <returns></returns>
        public abstract bool CheckPassword(string password, out string error);
    }
}
