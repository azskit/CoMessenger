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
        public string PeerID { get; set; }

        public string From { get; set; }
        public string To { get; set; }

        public List<RoutedMessage> Content { get; set; }
        //public string QueryID { get; set; }
    }

    public class IndexedHistoryManager
    {
        private BinaryFormatter fmt = new BinaryFormatter();
        private string StorageCatalog;
        //private string Sender;
        private string HistoryPath;



        private OleDbConnection HistoryDBConnection { get; set; }
        private string HistoryDBFile;

        private string formattingFileName;
        //private FileStream formattingFileStream;

        //public List<HistoryFile> HistoryFiles { get; private set; }
        //public bool HasNotLoadedFiles { get; set; }

        public bool KeepConnection { get; set; }

        //public Action OnHistoryQueryProcessed { get; set; }


        //public IndexedHistoryManager(string storageCatalog, string RECEIVER, string sender)
        public IndexedHistoryManager(string storageCatalog, bool keepConnection)
        {
            StorageCatalog = storageCatalog;

            //Sender = sender;

            //HistoryPath = System.IO.Path.Combine(StorageCatalog, Sender);
            HistoryPath = StorageCatalog;

            //База данных сообщений
            HistoryDBFile = Path.Combine(HistoryPath, "history.db");

            if (File.Exists(HistoryDBFile))
                Open();
            else
                GenerateIndexDataBase();


            //Форматированные сообщения
            formattingFileName = Path.Combine(HistoryPath, "history.dat");

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

            entry.PrevMsgID = reader["PrevMsgID"] as string;
            entry.MessageID = reader["MessageID"] as string;
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

                value.Kind = position >= 0 ? RoutedMessageKind.RichText : RoutedMessageKind.PlainText;

                if (value.Kind == RoutedMessageKind.RichText) 
                {
                    lock (formattingFileName)
                    {
                        using (FileStream formattingFileStream = new FileStream(formattingFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            formattingFileStream.Position = position;
                            value.Body = fmt.Deserialize(formattingFileStream) as byte[];
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

            while (NextID != String.Empty && retList.Count < MessagesToLoad)
            {
                RoutedMessage message = RestoreMessageFromHistory(NextID);

                if (message == null)
                    break;

                NextID = message.PrevMsgID;

                retList.Add(message);
            }

            return retList;
        }

        public List<RoutedMessage> GetRoomMessages(string RoomID, string lastEntryID, int MessagesToLoad)
        {
            OleDbCommand cmd;
            OleDbDataReader reader;

            List<RoutedMessage> retList = new List<RoutedMessage>(MessagesToLoad);

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

            if (lastEntryID != String.Empty)
            {
                cmd.CommandText = @"SELECT PrevMsgID FROM Messages WHERE MessageID = @MESSAGEID";

                cmd.Parameters.Add(new OleDbParameter("MESSAGEID", lastEntryID));
            }
            else
            {
                cmd.CommandText = @"SELECT TOP 1 MessageID 
                                      FROM Messages 
                                     WHERE ([RECEIVER] = @PEER)  
                                  ORDER BY SendTime desc";

                cmd.Parameters.Add(new OleDbParameter("PEER", RoomID));
            }
            reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                NextID = (string)reader[0];
            }
            #endregion

            reader.Close();

            retList = GetMessagesStartingWith(NextID, MessagesToLoad);

            if (!KeepConnection)
                HistoryDBConnection = null;

            return retList;

        }
        
        public List<RoutedMessage> GetPrivateMessages(string peer1, string peer2, string lastEntryID, int MessagesToLoad)
        {
            OleDbCommand cmd;
            OleDbDataReader reader;

            List<RoutedMessage> retList = new List<RoutedMessage>(MessagesToLoad);

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

            if (lastEntryID != String.Empty)
            {
                cmd.CommandText = @"SELECT PrevMsgID FROM Messages WHERE MessageID = @MESSAGEID";

                cmd.Parameters.Add(new OleDbParameter("MESSAGEID", lastEntryID));
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

            retList = GetMessagesStartingWith(NextID, MessagesToLoad);

            if (!KeepConnection)
                HistoryDBConnection = null;

            return retList;
        }

        public List<RoutedMessage> GetMessagesSentToReceiver(string Receiver, string lastEntryID)
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

            if (lastEntryID != String.Empty)
            {
                cmd.CommandText = @"SELECT MESSAGEID 
                                      FROM Messages 
                                     WHERE ([RECEIVER] = @PEER)  
                                       AND SendTime < (SELECT SendTime FROM Messages WHERE MESSAGEID = @MESSAGEID)
                                  ORDER BY SendTime asc";

                cmd.Parameters.Add(new OleDbParameter("PEER", Receiver));
                cmd.Parameters.Add(new OleDbParameter("MESSAGEID", lastEntryID));
            }
            else
            {
                cmd.CommandText = @"SELECT MESSAGEID 
                                      FROM Messages 
                                     WHERE ([RECEIVER] = @PEER)  
                                  ORDER BY SendTime asc";

                cmd.Parameters.Add(new OleDbParameter("PEER", Receiver));
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
            bool actualKeepConnection = KeepConnection;

            KeepConnection = true;

            messagesToSave.ForEach(msg => Save(msg));

            KeepConnection = actualKeepConnection;
        }

        public void Delete(RoutedMessage msg)
        {
            Delete(msg.MessageID);
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

                cmd.Parameters.Add(new OleDbParameter("MESSAGEID", entryToSave.MessageID));

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

                    int currentVersion = (int)reader[0];

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
                    if (entryToSave.PrevMsgID == null) entryToSave.PrevMsgID = String.Empty;


                    if (entryToSave.PrevMsgID != String.Empty)
                    {

                        cmd = HistoryDBConnection.CreateCommand();

                        cmd.CommandText = @"UPDATE [Messages] SET [PrevMsgID] = @CURMSGID  WHERE [PrevMsgID] = @PREVMSGID";

                        cmd.Parameters.Add(new OleDbParameter("CURMSGID", entryToSave.MessageID));
                        cmd.Parameters.Add(new OleDbParameter("PREVMSGID", entryToSave.PrevMsgID));

                        cmd.ExecuteNonQuery();
                    }

                    cmd = HistoryDBConnection.CreateCommand();

                    cmd.CommandText = @"INSERT INTO [Messages] ([MessageID], [PrevMsgID], [Sender], [Receiver], [SendTime]) 
                                             VALUES            (@MESSAGEID , @PREVMSGID , @SENDER , @RECEIVER , @SENDTIME )";

                    cmd.Parameters.Add(new OleDbParameter("MESSAGEID", entryToSave.MessageID));
                    cmd.Parameters.Add(new OleDbParameter("PREVMSGID", entryToSave.PrevMsgID));
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
                lock (formattingFileName)
                {
                    using (FileStream formattingFileStream = new FileStream(formattingFileName, FileMode.Append, FileAccess.Write, FileShare.Read))
                    {
                        position = formattingFileStream.Position;
                        fmt.Serialize(formattingFileStream, value.Body);
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
        }

        public string GetLastMsgBetween(string Peer1, string Peer2, DateTime SendTime)
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

            cmd.Parameters.Add(new OleDbParameter("SENDTIME", SendTime.ToOADate()));
            cmd.Parameters.Add(new OleDbParameter("SENDER", Peer1));
            cmd.Parameters.Add(new OleDbParameter("RECEIVER", Peer2));

            reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                PrevMsgID = (string)reader["MessageID"];

                reader.Close();
            }

            return PrevMsgID;

        }

        public string GetLastMsgTo(string Receiver, DateTime SendTime)
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

            cmd.Parameters.Add(new OleDbParameter("SENDTIME", SendTime.ToOADate()));
            cmd.Parameters.Add(new OleDbParameter("RECEIVER", Receiver));

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
