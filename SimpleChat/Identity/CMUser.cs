using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using CorporateMessengerLibrary;
using SimpleChat.Protocol;

namespace SimpleChat.Identity
{
    public abstract class CMUser : IEquatable<CMUser>
    {

        public string DisplayName { get; set; }

        public string UserId { get; set; }

        public string UserName { get; set; }
        
        //[NonSerialized()]
        //private string password;
        //private byte[] encryptedPassword;

        //public string Password
        //{
        //    get
        //    {
        //        return password;
        //    }

        //    set
        //    {
        //        password = value;
        //    }
        //}

        //public byte[] EncryptedPassword
        //{
        //    get
        //    {
        //        return encryptedPassword;
        //    }

        //    set
        //    {
        //        encryptedPassword = value;
        //    }
        //}

        //public bool IsBuiltIn { get; set; }

        public string Domain { get; set; }

        internal ServerSideClient Client { get; set; }

        public bool Equals(CMUser other)
        {
            if (other == null)
                return false;

            return other.UserId == this.UserId;
        }

        internal abstract bool CheckPassword(string password);
    }
}
