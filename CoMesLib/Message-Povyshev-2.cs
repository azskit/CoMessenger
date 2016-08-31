using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO.Compression;

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
        UsersList,
        User,
        Groups,
        RoutedMessage,
        CryptoKey
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
        public string MessageID { get; set; }
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

        public bool Loaded = false;
    }

    public class HistoryFile
    {
        public string FileName;
        public DateTime Date;
        public List<HistoryEntry> Entries;

        public bool Loaded = false;
    }

    public class HistoryManager
    {
        private static string FileNamePattern = "yyyyMMdd";

        private BinaryFormatter fmt = new BinaryFormatter();
        private string StorageCatalog;
        private string Reciever;
        private string Sender;
        private string HistoryPath;
        //private string FileName;

        public List<HistoryFile> HistoryFiles { get; private set; }
        public bool HasNotLoadedFiles { get; set; }

        public HistoryManager(string storageCatalog, string reciever, string sender)
        {
            StorageCatalog = storageCatalog;
            Reciever = reciever;
            Sender = sender;

            HistoryPath = System.IO.Path.Combine(StorageCatalog, Reciever, Sender);

            HistoryFiles = new List<HistoryFile>();

            if (Directory.Exists(HistoryPath))
            {
                //Посмотрим какие файлы из истории мы можем загрузить
                foreach (string filename in Directory.EnumerateFiles(HistoryPath, "????????.dat", SearchOption.TopDirectoryOnly))
                {
                    HistoryFile hFile = new HistoryFile();

                    if (DateTime.TryParseExact(Path.GetFileNameWithoutExtension(filename), FileNamePattern, CultureInfo.InvariantCulture, DateTimeStyles.None, out hFile.Date))
                    {
                        hFile.FileName = filename;
                        HistoryFiles.Add(hFile);
                    }

                }

                if (HistoryFiles.Count != 0)
                {
                    HasNotLoadedFiles = true;
                    //Сортируем в обратном порядке
                    HistoryFiles.Sort((file1, file2) => file2.Date.CompareTo(file1.Date));
                }
                else
                {
                    HasNotLoadedFiles = false;
                }
            }
            //FileName = fileName;
        }

        /// <summary>
        /// Загрузить историю
        /// </summary>
        /// <param name="Date">Дата загрузки</param>
        /// <returns>Список сообщений</returns>
        public void ReadHistoryFromDisc(HistoryFile hFile)
        {
            List<HistoryEntry> historyList = new List<HistoryEntry>();

            //string HistoryPath = System.IO.Path.Combine(StorageCatalog, Reciever, Sender);
            if (File.Exists(hFile.FileName))
            {/*
                lock (hFile)
                {
                    //using (MemoryStream ms = new MemoryStream())
                    //using (MemoryStream ms2 = new MemoryStream())
                    using (FileStream history = new FileStream(hFile.FileName, FileMode.Open))
                    using (GZipStream compressedHistory = new GZipStream(history, CompressionMode.Decompress))
                    {

                        //byte[] bt = fmt.Deserialize(history) as byte[];
                        //ms.Write(bt, 0, bt.Length);
                        //ms.Position = 0;
                        //compressedHistory.CopyTo(ms2);
                        historyList.Add(fmt.Deserialize(compressedHistory) as HistoryEntry);

                        //fmt.Serialize(history, ms2.ToArray());

                        //ms.CopyTo(compressedHistory);
                    }

                }*/
            }
            hFile.Entries = historyList.OrderByDescending<HistoryEntry, DateTime>(entry => entry.routedMessage.SendTime).ToList();
        }

        public void Save(HistoryEntry entryToSave)
        {
            //string HistoryPath = System.IO.Path.Combine(App.StorageCatalog, routedMessage.Receiver, routedMessage.Sender);
            string filename = System.IO.Path.Combine(HistoryPath, entryToSave.routedMessage.SendTime.ToLocalTime().ToString(FileNamePattern, CultureInfo.InvariantCulture) + ".dat");

            //Ищем подходящий файл
            HistoryFile hFile = HistoryFiles.FirstOrDefault(file => file.FileName == filename);

            if (hFile == null)
            {
                hFile = new HistoryFile();
                hFile.FileName = filename;
                hFile.Date = entryToSave.routedMessage.SendTime.ToLocalTime();
            }

            System.IO.Directory.CreateDirectory(HistoryPath);
            lock (hFile)
            {
                using (MemoryStream ms = new MemoryStream())
                using (MemoryStream ms2 = new MemoryStream())
                using (FileStream history = new FileStream(hFile.FileName, FileMode.Append))
                using (DeflateStream compressedHistory = new DeflateStream(ms, CompressionMode.Compress))
                using (DeflateStream decompressedHistory = new DeflateStream(ms2, CompressionMode.Decompress))
                {

                    fmt.Serialize(compressedHistory, entryToSave);
                    compressedHistory.Flush();
                    ms.Position = 0;
                    ms.CopyTo(ms2);
                    ms2.Position =0;
                    HistoryEntry newh = fmt.Deserialize(decompressedHistory) as HistoryEntry;


                    //ms.Position = 0; 
                    //compressedHistory.CopyTo(ms2);
                    //fmt.Serialize(history, ms.ToArray());
                    
                    //ms.CopyTo(compressedHistory);
                }
            }
        }

        public void SaveMessage(string messageId, HistoryEntry entryToSave)
        {
            //string HistoryPath = System.IO.Path.Combine(App.StorageCatalog, routedMessage.Receiver, routedMessage.Sender);
            System.IO.Directory.CreateDirectory(HistoryPath);

            using (FileStream history = new FileStream(System.IO.Path.Combine(HistoryPath, messageId + ".dat"), FileMode.Create))
            {
                fmt.Serialize(history, entryToSave);
            }
        }

        public Queue<HistoryEntry> ReadMessages()
        {
            List<HistoryEntry> tmp_list = new List<HistoryEntry>();

            if (Directory.Exists(HistoryPath))
            {
                foreach (string filename in Directory.EnumerateFiles(HistoryPath, "*.dat"))
                {
                    using (FileStream history = new FileStream(filename, FileMode.Open))
                    {
                        tmp_list.Add(fmt.Deserialize(history) as HistoryEntry);
                    }
                }

                tmp_list.Sort((x, y) => { return x.routedMessage.SendTime.CompareTo(y.routedMessage.SendTime); });
            }

            return new Queue<HistoryEntry>(tmp_list);

        }

        public void DeleteMessage(string messageId)
        {
            File.Delete(System.IO.Path.Combine(HistoryPath, messageId + ".dat"));
        }


    }

}
