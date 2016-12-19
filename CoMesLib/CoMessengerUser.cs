using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Xml.Serialization;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Globalization;

namespace CorporateMessengerLibrary
{
    public enum PeerType
    {
        Person,
        Room
    }

    [Serializable]
    public class PersonPeer : Peer
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly PersonPeerEqualityComparer EqualityComparer = new PersonPeerEqualityComparer();
    }

    [Serializable]
    public class RoomPeer : Peer
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
        public List<PersonPeer> Participants { get; } = new List<PersonPeer>();
        public bool IsMainRoom { get; set; }
    }

    //[XmlInclude(typeof(RoomPeer))]
    //[XmlInclude(typeof(PersonPeer))]
    [Serializable]
    public abstract class Peer : IEquatable<Peer>
    {
        public string DisplayName { get; set; }
        public string PeerId { get; set; }
        public PeerType PeerType { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public byte[] Avatar { get; set; }

        public PeerStatus State { get; set; }


        public bool Equals(Peer other)
        {
            if (other == null)
                return false;

            return other.PeerId == this.PeerId;
        }
    }

    public class PeerComparer : Comparer<Peer>
    {
        public override int Compare(Peer x, Peer y)
        {
            //if (x.Status == y.Status)

            if (x == null)
                throw new ArgumentNullException("x");

            if (y == null)
                throw new ArgumentNullException("y");

            return string.Compare(x.DisplayName, y.DisplayName, StringComparison.CurrentCulture);
            ////else if (x.Status == PeerStatus.Online)
            //    return -1;
            //else if (y.Status == PeerStatus.Online)
            //    return 1;
            //else
            //    return x.DisplayName.CompareTo(y.DisplayName);
        }
    }

    public class PersonPeerEqualityComparer : IEqualityComparer<PersonPeer>
    {
        public bool Equals(PersonPeer x, PersonPeer y)
        {
            return x != null && y != null && x.PeerId == y.PeerId;
        }

        public int GetHashCode(PersonPeer obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            return obj.GetHashCode();
        }


    }

    /*
    public class CoMessengerUserComparer : Comparer<CoMessengerUser>
    {
        public override int Compare(CoMessengerUser x, CoMessengerUser y)
        {
            if (x.Status == y.Status)
                return x.DisplayName.CompareTo(y.DisplayName);
            else if (x.Status == PeerStatus.Online)
                return -1;
            else if (y.Status == PeerStatus.Online)
                return 1;
            else
                return x.DisplayName.CompareTo(y.DisplayName);
        }
    

    }
    */
    public enum PeerStatus
    {
        Online,
        Offline,
        Deleted,
        Common
    }

    //public enum RoomStatus
    //{
    //    Common

    //}

    public static class MD5Helper
    {
        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2", CultureInfo.InvariantCulture));
                }
                return sb.ToString();
            }
        }
    }

    [Serializable]
    public class CMGroup
    {
        public string GroupId { get; set; }
        public List<string> UserIds { get; } = new List<string>();
        public string DisplayName { get; set; }

        //[NonSerialized]
        //private List<CoMessengerUser> users;
        //[XmlIgnore]
        //public List<CoMessengerUser> Users
        //{
        //    get { return users; }
        //    set { users = value; }
        //}

    }



}
