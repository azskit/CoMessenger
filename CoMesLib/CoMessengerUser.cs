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
        public static PersonPeerEqualityComparer EqualityComparer = new PersonPeerEqualityComparer();
    }

    [Serializable]
    public class RoomPeer : Peer
    {
        public List<PersonPeer> Participants { get; set; }
        public bool IsMainRoom { get; set; }
    }

    //[XmlInclude(typeof(RoomPeer))]
    //[XmlInclude(typeof(PersonPeer))]
    [Serializable]
    public abstract class Peer : IEquatable<Peer>
    {
        public string DisplayName { get; set; }
        public string PeerID { get; set; }
        public PeerType Type { get; set; }

        public byte[] Avatar { get; set; }

        public PeerStatus State { get; set; }


        public bool Equals(Peer other)
        {
            return other.PeerID == this.PeerID;
        }
    }

    public class PeerComparer : Comparer<Peer>
    {
        public override int Compare(Peer x, Peer y)
        {
            //if (x.Status == y.Status)
                return x.DisplayName.CompareTo(y.DisplayName);
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
            return x != null && y != null && x.PeerID == y.PeerID;
        }

        public int GetHashCode(PersonPeer obj)
        {
            return obj.GetHashCode();
        }


    }

    [Serializable]
    public class CoMessengerUser : IEquatable<CoMessengerUser>
    {
        private string _DisplayName;

        public string DisplayName
        {
            get { return _DisplayName; }
            set { _DisplayName = value; }
        }

        private string _UserID;

        public string UserId
        {
            get { return _UserID; }
            set { _UserID = value; }
        }

        public string Login;

        [NonSerialized()]
        public string Password;
        public byte[] EncryptedPassword;


        public virtual bool IsPasswordCorrect(string password)
        {
            if (IsBuiltIn)
                return (MD5Helper.CreateMD5(password) == Password);
            else
                throw new NotImplementedException("Проверка пароля должна быть реализована в наследующем классе");
        }

        public bool IsBuiltIn { get; set; }

        public string Domain { get; set; }

        [NonSerialized()]
        private CMClientBase client;
        [XmlIgnore]
        public CMClientBase Client
        {
            get { return client; }
            set { client = value; }
        }

        //public PeerStatus Status { get; set; }

        public bool Equals(CoMessengerUser other)
        {
            return other.UserId == this.UserId;
        }

        [NonSerialized]
        private Queue<CMMessage> messagesQueue = new Queue<CMMessage>();
        [XmlIgnore]
        public Queue<CMMessage> MessagesQueue
        {
            get { return messagesQueue; }
            set { messagesQueue = value; }
        }

        //[NonSerialized]
        //private Color backColor = new Color();

        //[XmlIgnore]
        //public Color BackColor
        //{
        //    get { return backColor; }
        //    set { backColor = value; }
        //}

        //public static IComparer<CoMessengerUser> Comparator = new CoMessengerUserComparer();

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
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }

    [Serializable]
    public class CoMessengerGroup
    {
        public string GroupID { get; set; }
        public List<string> UserIDs { get; set; }
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
