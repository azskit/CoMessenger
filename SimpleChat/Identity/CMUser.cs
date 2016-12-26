using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using CimPlugin.Plugin.Authentication;
using CorporateMessengerLibrary;
using SimpleChat.Protocol;

namespace SimpleChat.Identity
{
    public class CMUser : IEquatable<CMUser>
    {
        public AuthenticationData AuthData { get; set; }

        public string UserId { get; set; }
        
        internal ServerSideClient Client { get; set; }

        public bool Equals(CMUser other)
        {
            if (other == null || other.AuthData == null)
                return false;

            if (this.AuthData == null)
                return false;

            return other.AuthData.UserId == this.AuthData.UserId;
        }
    }
}
