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
        public AuthenticationData Authentication { get; set; }

        public string UserId { get; set; }
        
        internal ServerSideClient Client { get; set; }

        public bool Equals(CMUser other)
        {
            if (other == null || other.Authentication == null)
                return false;

            if (this.Authentication == null)
                return false;

            return other.Authentication.UserId == this.Authentication.UserId;
        }
    }
}
