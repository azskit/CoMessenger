using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

[assembly: CLSCompliant(true)]
namespace CorporateMessengerLibrary
{
    public enum MessageKind
    {
        Authorization,
        Disconnect,
        Ping,
        AuthorizationError,
        AuthorizationNewPassword,
        Rooms,
        Person,
        Groups,
        RoutedMessage
    }

    public enum ErrorKind
    {
        UserNotPresented,
        UserNotFound,
        WrongPassword,
        DomainCouldNotBeContacted
    }


    public static class Serializator
    {
        public static BinaryFormatter fmt = new BinaryFormatter();
    }

    [Serializable]
    public class CMMessage
    {
        private MessageKind _kind;

        public MessageKind Kind
        {
            get { return _kind; }
            set { _kind = value; }
        }

        private object _message;

        public object Message
        {
            get { return _message; }
            set { _message = value; }
        }
    }

    [Serializable]
    public class RoutedMessage
    {
        public RoutedMessageKind Kind { get; set; }
        public object Message { get; set; }
        public string Receiver { get; set; }
        public string Sender { get; set; }
        public DateTime SendTime { get; set; }
    }


    public enum RoutedMessageKind
    {
        RichText
    }

    public enum MessageType
    {
        Income,
        Outcome
    }

    [Serializable]
    public class HistoryEntry
    {
        public RoutedMessage routedMessage;
        public MessageType Type;
    }

    public class HistoryManager
    {
        private BinaryFormatter fmt = new BinaryFormatter();
        private string StorageCatalog;
        private string Reciever;
        private string Sender;
        private string HistoryPath;
        //private string FileName;

        public HistoryManager(string storageCatalog, string reciever, string sender)
        {
            StorageCatalog = storageCatalog;
            Reciever = reciever;
            Sender = sender;

            HistoryPath = System.IO.Path.Combine(StorageCatalog, Reciever, Sender);

            //FileName = fileName;
        }

        /// <summary>
        /// Загрузить историю
        /// </summary>
        /// <param name="Date">Дата загрузки</param>
        /// <returns>Список сообщений</returns>
        public List<HistoryEntry> LoadHistory(DateTime Date)
        {
            List<HistoryEntry> historyList = new List<HistoryEntry>();

            //string HistoryPath = System.IO.Path.Combine(StorageCatalog, Reciever, Sender);
            if (File.Exists(System.IO.Path.Combine(HistoryPath, Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".dat")))
            {
                using (FileStream history = new FileStream(System.IO.Path.Combine(HistoryPath, DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".dat"), FileMode.Open))
                {
                    while (history.Position != history.Length)
                        historyList.Add(fmt.Deserialize(history) as HistoryEntry);
                }
            }
            return historyList;
        }

        public void Save(HistoryEntry entryToSave)
        {
            //string HistoryPath = System.IO.Path.Combine(App.StorageCatalog, routedMessage.Receiver, routedMessage.Sender);
            System.IO.Directory.CreateDirectory(HistoryPath);

            using (FileStream history = new FileStream(System.IO.Path.Combine(HistoryPath, entryToSave.routedMessage.SendTime.ToLocalTime().ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".dat"), FileMode.Append))
            {
                fmt.Serialize(history, entryToSave);
            }

        }
    }

}
