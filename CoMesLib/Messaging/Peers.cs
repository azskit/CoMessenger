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

namespace CorporateMessengerLibrary.Messaging
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
    public enum PeerStatus
    {
        Online,
        Offline,
        Deleted,
        Common
    }
}
