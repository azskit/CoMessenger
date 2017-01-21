using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace CorporateMessengerLibrary
{

    [Serializable]
    public class HistoryQuery
    {
        public string PeerId { get; set; }

        public string From { get; set; }
        public string To { get; set; }

        public List<RoutedMessage> Content { get; } = new List<RoutedMessage>();
        //public string QueryID { get; set; }
    }

    public class IndexedHistoryManager
    {
        private BinaryFormatter fmt = new BinaryFormatter();
        //private string StorageCatalog;
        //private string Sender;
        private string HistoryPath;



        private OleDbConnection HistoryDBConnection { get; set; }
        private string HistoryDBFile;

        private string formattingFileName;
        private object formattingFileSemafor = new object();
        //private FileStream formattingFileStream;

        private string binaryFileName;
        private object binaryFileSemafor = new object();


        //public List<HistoryFile> HistoryFiles { get; private set; }
        //public bool HasNotLoadedFiles { get; set; }

        public bool KeepConnection { get; set; }

        //public Action OnHistoryQueryProcessed { get; set; }


        //public IndexedHistoryManager(string storageCatalog, string RECEIVER, string sender)

        public IndexedHistoryManager()
        {

        }

        public IndexedHistoryManager(string storageCatalog, bool keepConnection)
        {
            InitHistoryManager(storageCatalog, keepConnection);
        }

        public void InitHistoryManager(string storageCatalog, bool keepConnection)
        {
            //StorageCatalog = storageCatalog;

            HistoryPath = storageCatalog;

            //База данных сообщений
            HistoryDBFile = Path.Combine(HistoryPath, "history.db");

            if (File.Exists(HistoryDBFile))
                Open();
            else
                GenerateIndexDataBase();


            //Форматированные сообщения
            formattingFileName = Path.Combine(HistoryPath, "history.dat");
            binaryFileName = Path.Combine(HistoryPath, "cache.dat");

            KeepConnection = keepConnection;
        }

        private RoutedMessage RestoreMessageFromHistory(string MessageID)
        {
            OleDbCommand cmd;
            OleDbDataReader reader;


            cmd = HistoryDBConnection.CreateCommand();

            cmd.CommandText = @"SELECT * FROM Messages WHERE MessageID = @MESSAGEID";

            cmd.Parameters.Add(new OleDbParameter("MESSAGEID", MessageID));


            reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            RoutedMessage entry = new RoutedMessage();

            entry.PreviousMessageId = reader["PrevMsgID"] as string;
            entry.MessageId = reader["MessageID"] as string;
            entry.Sender = reader["Sender"] as string;
            entry.Receiver = reader["Receiver"] as string;
            entry.SendTime = (DateTime)reader["SendTime"];

            OleDbCommand ContentCmd;
            OleDbDataReader ContentReader;

            ContentCmd = HistoryDBConnection.CreateCommand();
            
            ContentCmd.CommandText = @"SELECT        `MessageAutoID`
		                                            ,`Version`
		                                            ,`MessageText`
		                                            ,`ChangeTime`
		                                            ,`PositionInFile`
	                                            FROM `MessageContent`
                                               WHERE MessageAutoID = @MESSAGEAUTOID
                                            ORDER BY Version";

            ContentCmd.Parameters.Add(new OleDbParameter("MESSAGEAUTOID", (int)reader["AutoID"]));


            ContentReader = ContentCmd.ExecuteReader();

            while (ContentReader.Read())
            {
                MessageValue value = new MessageValue();

                value.Text       =           ContentReader["MessageText"] as string;
                value.Version    = (int)     ContentReader["Version"];
                value.ChangeTime = (DateTime)ContentReader["ChangeTime"];

                long position = (long)(decimal)ContentReader["PositionInFile"];

                value.Kind = position >= 0 ? RoutedMessageKind.RichText : RoutedMessageKind.Plaintext;

                if (value.Kind == RoutedMessageKind.RichText) 
                {
                    lock (formattingFileSemafor)
                    {
                        using (FileStream formattingFileStream = new FileStream(formattingFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            formattingFileStream.Position = position;
                            value.FormattedText = fmt.Deserialize(formattingFileStream) as byte[];
                        }
                    }
                }

                entry.Values.Add(value.Version, value);
            }

            reader.Close();
            ContentReader.Close();

            return entry;
        }

        private List<RoutedMessage> GetMessagesStartingWith(string FirstMessageID, int MessagesToLoad)
        {

            List<RoutedMessage> retList = new List<RoutedMessage>(MessagesToLoad);


            string NextID = FirstMessageID;

            while (!String.IsNullOrEmpty(NextID) && retList.Count < MessagesToLoad)
            {
                RoutedMessage message = RestoreMessageFromHistory(NextID);

                if (message == null)
                    break;

                NextID = message.PreviousMessageId;

                retList.Add(message);
            }

            return retList;
        }

        public List<RoutedMessage> GetRoomMessages(string roomId, string lastEntryId, int messagesToLoad)
        {
            OleDbCommand cmd;
            OleDbDataReader reader;

            List<RoutedMessage> retList = new List<RoutedMessage>(messagesToLoad);

            if (HistoryDBConnection == null)
            {
                if (File.Exists(HistoryDBFile))
                    Open();
                else
                    return retList;
            }

            #region Ищем запись, с которой начнем загрузку истории
            string NextID = String.Empty;

            cmd = HistoryDBConnection.CreateCommand();

            if (!String.IsNullOrEmpty(lastEntryId))
            {
                cmd.CommandText = @"SELECT PrevMsgID FROM Messages WHERE MessageID = @MESSAGEID";

                cmd.Parameters.Add(new OleDbParameter("MESSAGEID", lastEntryId));
            }
            else
            {
                cmd.CommandText = @"SELECT TOP 1 MessageID 
                                      FROM Messages 
                                     WHERE ([RECEIVER] = @PEER)  
                                  ORDER BY SendTime desc";

                cmd.Parameters.Add(new OleDbParameter("PEER", roomId));
            }
            reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                NextID = (string)reader[0];
            }
            #endregion

            reader.Close();

            retList = GetMessagesStartingWith(NextID, messagesToLoad);

            if (!KeepConnection)
                HistoryDBConnection = null;

            return retList;

        }
        
        public List<RoutedMessage> GetPrivateMessages(string peer1, string peer2, string lastEntryId, int messagesToLoad)
        {
            OleDbCommand cmd;
            OleDbDataReader reader;

            List<RoutedMessage> retList = new List<RoutedMessage>(messagesToLoad);

            if (HistoryDBConnection == null)
            {
                if (File.Exists(HistoryDBFile))
                    Open();
                else
                    return retList;
            }

            #region Ищем запись, с которой начнем загрузку истории
            string NextID = String.Empty;

            cmd = HistoryDBConnection.CreateCommand();

            if ( !String.IsNullOrEmpty(lastEntryId))
            {
                cmd.CommandText = @"SELECT PrevMsgID FROM Messages WHERE MessageID = @MESSAGEID";

                cmd.Parameters.Add(new OleDbParameter("MESSAGEID", lastEntryId));
            }
            else
            {
                cmd.CommandText = @"SELECT TOP 1 MessageID 
                                      FROM Messages 
                                     WHERE (
                                               ([Sender]   = @SENDER AND [RECEIVER] = @RECEIVER)
                                            OR ([RECEIVER] = @SENDER AND [Sender]   = @RECEIVER)
                                           )  
                                  ORDER BY SendTime desc";

                cmd.Parameters.Add(new OleDbParameter("SENDER", peer1));
                cmd.Parameters.Add(new OleDbParameter("RECEIVER", peer2));
            }
            reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                NextID = (string)reader[0];
            }

            reader.Close();

            #endregion

            retList = GetMessagesStartingWith(NextID, messagesToLoad);

            if (!KeepConnection)
                HistoryDBConnection = null;

            return retList;
        }

        public List<RoutedMessage> GetMessagesSentToReceiver(string receiver, string lastEntryId)
        {
            OleDbCommand cmd;
            OleDbDataReader reader;

            List<RoutedMessage> retList = new List<RoutedMessage>();

            if (HistoryDBConnection == null)
            {
                if (File.Exists(HistoryDBFile))
                    Open();
                else
                    return retList;
            }

            cmd = HistoryDBConnection.CreateCommand();

            if ( !String.IsNullOrEmpty(lastEntryId))
            {
                cmd.CommandText = @"SELECT MESSAGEID 
                                      FROM Messages 
                                     WHERE ([RECEIVER] = @PEER)  
                                       AND SendTime < (SELECT SendTime FROM Messages WHERE MESSAGEID = @MESSAGEID)
                                  ORDER BY SendTime asc";

                cmd.Parameters.Add(new OleDbParameter("PEER", receiver));
                cmd.Parameters.Add(new OleDbParameter("MESSAGEID", lastEntryId));
            }
            else
            {
                cmd.CommandText = @"SELECT MESSAGEID 
                                      FROM Messages 
                                     WHERE ([RECEIVER] = @PEER)  
                                  ORDER BY SendTime asc";

                cmd.Parameters.Add(new OleDbParameter("PEER", receiver));
            }
            reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                RoutedMessage message = RestoreMessageFromHistory(reader["MESSAGEID"] as string);

                if (message == null)
                    break;
                else
                    retList.Add(message);
            }

            reader.Close();

            if (!KeepConnection)
                HistoryDBConnection = null;

            return retList;

        }

        public void SaveMessages(List<RoutedMessage> messagesToSave)
        {
            if (messagesToSave == null)
                throw new ArgumentNullException("messagesToSave");

            bool actualKeepConnection = KeepConnection;

            KeepConnection = true;

            messagesToSave.ForEach(msg => Save(msg));

            KeepConnection = actualKeepConnection;
        }

        public void Delete(RoutedMessage message)
        {
            if (message == null)
                throw new ArgumentNullException("msg");

            Delete(message.MessageId);
        }

        private void Delete(string messageID)
        {
            if (HistoryDBConnection == null)
            {
                if (!File.Exists(HistoryDBFile))
                    return;
                else
                    Open();
            }

            if (HistoryDBConnection != null)
            {
                OleDbCommand cmd;

                cmd = HistoryDBConnection.CreateCommand();

                cmd.CommandText = @"DELETE FROM [Messages]  WHERE [MessageID] = @MESSAGEID";

                cmd.Parameters.Add(new OleDbParameter("MESSAGEID", messageID));

                cmd.ExecuteNonQuery();
            }

            if (!KeepConnection)
                HistoryDBConnection = null;

        }

        public void Save(RoutedMessage entryToSave)
        {
            if (entryToSave == null)
                throw new ArgumentNullException("entryToSave");

            System.IO.Directory.CreateDirectory(HistoryPath);

            if (HistoryDBConnection == null)
            {
                if (!File.Exists(HistoryDBFile))
                    GenerateIndexDataBase();
                else
                    Open();
            }



            if (HistoryDBConnection != null)
            {
                OleDbCommand cmd;
                OleDbDataReader reader;

                cmd = HistoryDBConnection.CreateCommand();

                cmd.CommandText = @"SELECT [AutoID] FROM [Messages]  WHERE [MessageID] = @MESSAGEID";

                cmd.Parameters.Add(new OleDbParameter("MESSAGEID", entryToSave.MessageId));

                reader = cmd.ExecuteReader();

                //Уже есть такое сообщение в истории
                #region
                if (reader.Read())
                {

                    int AutoID = (int)reader["AutoID"];

                    reader.Close();

                    //Проверим, версию
                    cmd = HistoryDBConnection.CreateCommand();

                    cmd.CommandText = @"SELECT MAX([Version]) FROM [MessageContent]  WHERE [MessageAutoID] = @AUTOID";

                    cmd.Parameters.Add(new OleDbParameter("AUTOID", AutoID));

                    reader = cmd.ExecuteReader();

                    reader.Read();

                    int currentVersion = reader[0] is Int32 ? (int)reader[0] : 0 ;

                    reader.Close();

                    foreach (MessageValue value in entryToSave.Values.Values)
                    {
                        //Пришло что-то новее, вставляем, иначе насрать
                        if (value.Version > currentVersion)
                        {
                            SaveMessageContent(AutoID, value);
                        }
                    }
                }
                #endregion
                //Такого сообщения еще не было
                #region
                else
                {
                    if (entryToSave.PreviousMessageId == null) entryToSave.PreviousMessageId = String.Empty;


                    if (!String.IsNullOrEmpty(entryToSave.PreviousMessageId))
                    {

                        cmd = HistoryDBConnection.CreateCommand();

                        cmd.CommandText = @"UPDATE [Messages] SET [PrevMsgID] = @CURMSGID  WHERE [PrevMsgID] = @PREVMSGID";

                        cmd.Parameters.Add(new OleDbParameter("CURMSGID", entryToSave.MessageId));
                        cmd.Parameters.Add(new OleDbParameter("PREVMSGID", entryToSave.PreviousMessageId));

                        cmd.ExecuteNonQuery();
                    }

                    cmd = HistoryDBConnection.CreateCommand();

                    cmd.CommandText = @"INSERT INTO [Messages] ([MessageID], [PrevMsgID], [Sender], [Receiver], [SendTime]) 
                                             VALUES            (@MESSAGEID , @PREVMSGID , @SENDER , @RECEIVER , @SENDTIME )";

                    cmd.Parameters.Add(new OleDbParameter("MESSAGEID", entryToSave.MessageId));
                    cmd.Parameters.Add(new OleDbParameter("PREVMSGID", entryToSave.PreviousMessageId));
                    cmd.Parameters.Add(new OleDbParameter("SENDER",    entryToSave.Sender));
                    cmd.Parameters.Add(new OleDbParameter("RECEIVER",  entryToSave.Receiver));
                    cmd.Parameters.Add(new OleDbParameter("SENDTIME",  entryToSave.SendTime.ToOADate()));

                    cmd.ExecuteNonQuery();

                    cmd = HistoryDBConnection.CreateCommand();

                    cmd.CommandText = @"SELECT MAX([AutoID]) FROM [Messages]";

                    reader = cmd.ExecuteReader();

                    reader.Read();

                    int newID = (int)reader[0];

                    reader.Close();

                    entryToSave.Values.ToList().ForEach(pair => SaveMessageContent(newID, pair.Value));

                    //foreach (MessageValue value in entryToSave.Values.Values)
                    //{
                    //    SaveMessageContent(newID, value);
                    //}
                }
                #endregion
            }

            if (!KeepConnection)
                HistoryDBConnection = null;

        }

        //Сохранить содержание сообщения
        private void SaveMessageContent(int AutoID, MessageValue value)
        {
            long position = -1;
            if (value.Kind == RoutedMessageKind.RichText) //Форматированное сообщение
            {
                lock (formattingFileSemafor)
                {
                    using (FileStream formattingFileStream = new FileStream(formattingFileName, FileMode.Append, FileAccess.Write, FileShare.Read))
                    {
                        position = formattingFileStream.Position;
                        fmt.Serialize(formattingFileStream, value.FormattedText);
                    }
                }
            }

            OleDbCommand cmd = HistoryDBConnection.CreateCommand();

            cmd.CommandText = @"INSERT INTO  `MessageContent`
		                                                (`MessageAutoID`
		                                                ,`Version`
		                                                ,`MessageText`
		                                                ,`ChangeTime`
		                                                ,`PositionInFile`)
	                                                VALUES
		                                                (@MESSAGEAUTOID 
		                                                ,@VERSION      
		                                                ,@MESSAGETEXT   
		                                                ,@CHANGETIME   
		                                                ,@POSITIONINFILE)";

            cmd.Parameters.Add(new OleDbParameter("MESSAGEAUTOID", AutoID));
            cmd.Parameters.Add(new OleDbParameter("VERSION", value.Version));
            cmd.Parameters.Add(new OleDbParameter("MESSAGETEXT", value.Text));
            cmd.Parameters.Add(new OleDbParameter("CHANGETIME", value.ChangeTime.ToOADate()));
            cmd.Parameters.Add(new OleDbParameter("POSITIONINFILE", position));

            cmd.ExecuteNonQuery();
        }

        public void SaveBinary(byte[] uncompressedBytes)
        {
            string hash = SHA1Helper.GetHash(uncompressedBytes);

            if (BinaryExists(hash))
                return;

            byte[] compressedBytes = Compressing.Compress(uncompressedBytes);


            long position = -1;
            lock (binaryFileSemafor)
            {
                using (FileStream filestream = new FileStream(binaryFileName, FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    position = filestream.Position;
                    filestream.Write(compressedBytes, 0, compressedBytes.Length);
                }
            }

            long length = compressedBytes.Length;

            OleDbCommand cmd = HistoryDBConnection.CreateCommand();
            cmd.CommandText = @"INSERT INTO  `BinaryContent`
		                                                (`SHA1Hash`
		                                                ,`Length`
		                                                ,`PositionInFile`)
	                                                VALUES
		                                                (@SHA1HASH 
		                                                ,@LENGTH
		                                                ,@POSITIONINFILE)";

            cmd.Parameters.Add(new OleDbParameter("SHA1HASH",       hash));
            cmd.Parameters.Add(new OleDbParameter("LENGTH",         length));
            cmd.Parameters.Add(new OleDbParameter("POSITIONINFILE", position));

            cmd.ExecuteNonQuery();
        }

        public byte[] RestoreBinary(string hash)
        {

            byte[] retval = null;

            OleDbCommand ContentCmd;
            OleDbDataReader ContentReader;

            ContentCmd = HistoryDBConnection.CreateCommand();

            ContentCmd.CommandText = @"SELECT        `SHA1Hash`
		                                            ,`Length`
		                                            ,`PositionInFile`
	                                            FROM `BinaryContent`
                                               WHERE SHA1Hash = @SHA1HASH";

            ContentCmd.Parameters.Add(new OleDbParameter("SHA1HASH", hash));


            ContentReader = ContentCmd.ExecuteReader();

            if (ContentReader.Read())
            {
                long length = (long)(decimal)ContentReader["Length"];

                long position = (long)(decimal)ContentReader["PositionInFile"];
                lock (binaryFileSemafor)
                {
                    try
                    {
                        using (FileStream filestream = new FileStream(binaryFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            filestream.Position = position;
                            retval = new byte[length];
                            filestream.Read(retval, 0, retval.Length);
                        }

                        retval = Compressing.Decompress(retval);
                    }
                    catch (FileNotFoundException) { }
                    catch (InvalidDataException)
                    {
                        DeleteBinary(hash);
                    }
                }

                
            }

            ContentReader.Close();

            return retval;
        }

        private void DeleteBinary(string hash)
        {
            OleDbCommand cmd;

            cmd = HistoryDBConnection.CreateCommand();

            cmd.CommandText = @"DELETE FROM [BinaryContent]  WHERE [SHA1Hash] = @SHA1HASH";

            cmd.Parameters.Add(new OleDbParameter("SHA1HASH", hash));

            cmd.ExecuteNonQuery();
        }

        public bool BinaryExists(string hash)
        {

            OleDbCommand ContentCmd;
            OleDbDataReader ContentReader;

            ContentCmd = HistoryDBConnection.CreateCommand();

            ContentCmd.CommandText = @"SELECT        `SHA1Hash`
		                                            ,`Length`
		                                            ,`PositionInFile`
	                                            FROM `BinaryContent`
                                               WHERE SHA1Hash = @SHA1HASH";

            ContentCmd.Parameters.Add(new OleDbParameter("SHA1HASH", hash));


            ContentReader = ContentCmd.ExecuteReader();

            return ContentReader.Read();
        }

        private void Open()
        {
            if (File.Exists(HistoryDBFile))
            {
                string conString = "provider=Microsoft.Jet.OLEDB.4.0;data source=" + HistoryDBFile + ";";

                HistoryDBConnection = new OleDbConnection(conString);

                //try
                //{
                HistoryDBConnection.Open();

                System.Diagnostics.Trace.WriteLine(HistoryDBConnection.DataSource + " opened", "History");

                //}
                //catch (Exception e)
                //{
                //we must do something, but i don't know what exactly :(
                //}  
            }
            else
            {
                throw new FileNotFoundException("index database file not found", HistoryDBFile);
            }
        }

        public void Close()
        {
            HistoryDBConnection = null;
        }

        private void GenerateIndexDataBase()
        {

            System.IO.Directory.CreateDirectory(HistoryPath);

            if (File.Exists(HistoryDBFile))
                File.Delete(HistoryDBFile);

            ADOX.Catalog cat = new ADOX.Catalog();

            cat.Create("Provider=Microsoft.Jet.OLEDB.4.0;" +
                   "Data Source=" + HistoryDBFile + ";" +
                   "Jet OLEDB:Engine Type=5");

            cat = null;

            File.SetAttributes(Path.Combine(HistoryPath, "history.ldb"), FileAttributes.Hidden);

            OleDbCommand com;

            Open();
            com = HistoryDBConnection.CreateCommand();
            com.CommandText = @"CREATE TABLE [Messages] ( [AutoID]      COUNTER CONSTRAINT [AutoIDPrimKey] PRIMARY KEY,
                                                          [MessageID]   STRING(32) NOT NULL,
                                                          [PrevMsgID]   STRING(32), 
                                                          [Sender]      STRING(64) NOT NULL, 
                                                          [Receiver]    STRING(64) NOT NULL, 
                                                          [SendTime]    DATETIME  NOT NULL
                                                          )";
            com.ExecuteNonQuery();

            com = HistoryDBConnection.CreateCommand();
            com.CommandText = @"CREATE INDEX `MessageID_IDX`
	                                      ON `Messages` (`MessageID`)";
            com.ExecuteNonQuery();

            com = HistoryDBConnection.CreateCommand();
            com.CommandText = @"CREATE INDEX `SortingIndex`
	                                      ON `Messages` (`SendTime` DESC)";
            com.ExecuteNonQuery();

            com = HistoryDBConnection.CreateCommand();
            com.CommandText = @"CREATE TABLE `MessageContent` ( `MessageAutoID`  LONG           NOT NULL, 
	                                                            `Version`        LONG           NOT NULL, 
	                                                            `MessageText`    LONGTEXT       NOT NULL, 
	                                                            `ChangeTime`     DATETIME       NOT NULL, 
	                                                            `PositionInFile` DECIMAL(20, 0) NOT NULL, 

	                                                            FOREIGN KEY (`MessageAutoID`)
		                                                            REFERENCES `Messages`    (`AutoID`)
		                                                            ON UPDATE CASCADE        ON DELETE CASCADE
                                                            )";
            com.ExecuteNonQuery();

            com = HistoryDBConnection.CreateCommand();
            com.CommandText = @"CREATE TABLE `BinaryContent` (  `SHA1Hash`       STRING(64)     NOT NULL, 
	                                                            `Length`         DECIMAL(20, 0) NOT NULL, 
	                                                            `PositionInFile` DECIMAL(20, 0) NOT NULL
                                                             )";
            com.ExecuteNonQuery();

            com = HistoryDBConnection.CreateCommand();
            com.CommandText = @"CREATE INDEX `SHA1HashIndex`
	                                      ON `BinaryContent` (`SHA1Hash` DESC)";
            com.ExecuteNonQuery();
        }

        public string GetLastMessageBetween(string peer1, string peer2, DateTime sendTime)
        {
            OleDbCommand cmd;
            OleDbDataReader reader;

            string PrevMsgID = String.Empty;

            //Находим предыдущее сообщение
            cmd = HistoryDBConnection.CreateCommand();

            cmd.CommandText = @"SELECT TOP 1 [AutoID], [MessageID] 
                                              FROM [Messages] 
                                             WHERE [SendTime] <= @SENDTIME 
                                               AND (
                                                       ([Sender]   = @SENDER AND [RECEIVER] = @RECEIVER)
                                                    OR ([RECEIVER] = @SENDER AND [Sender]   = @RECEIVER)
                                                   )  
                                          ORDER BY [SendTime] DESC";

            cmd.Parameters.Add(new OleDbParameter("SENDTIME", sendTime.ToOADate()));
            cmd.Parameters.Add(new OleDbParameter("SENDER", peer1));
            cmd.Parameters.Add(new OleDbParameter("RECEIVER", peer2));

            reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                PrevMsgID = (string)reader["MessageID"];

                reader.Close();
            }

            return PrevMsgID;

        }

        public string GetLastMessageTo(string receiver, DateTime sendTime)
        {
            OleDbCommand cmd;
            OleDbDataReader reader;

            string PrevMsgID = String.Empty;

            //Находим предыдущее сообщение
            cmd = HistoryDBConnection.CreateCommand();

            cmd.CommandText = @"SELECT TOP 1 [AutoID], [MessageID] 
                                              FROM [Messages] 
                                             WHERE [SendTime] <= @SENDTIME 
                                               AND [RECEIVER] =  @RECEIVER
                                          ORDER BY [SendTime] DESC";

            cmd.Parameters.Add(new OleDbParameter("SENDTIME", sendTime.ToOADate()));
            cmd.Parameters.Add(new OleDbParameter("RECEIVER", receiver));

            reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                PrevMsgID = (string)reader["MessageID"];

                reader.Close();
            }

            return PrevMsgID;

        }

    
    }

}
