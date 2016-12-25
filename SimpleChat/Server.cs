using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using CorporateMessengerLibrary;
using System.Globalization;
using System.Linq;
using System.IO;
using SimpleChat.Protocol;
using SimpleChat.Identity;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using CimPlugin.Plugin;
using CimPlugin.Plugin.Authentication;
using CimPlugin.Plugin.Groups;

namespace SimpleChat
{
    class Server
    {
        static private CompositionContainer container;


        [ImportMany]
        static private IEnumerable<IPlugin> plugins;
        static private IEnumerable<IAuthentication> authPlugins;
        static private IEnumerable<IGroupCollector> groupPlugins;

        static private List<ServerSideClient> clients = new List<ServerSideClient>();
        //static internal List<CMUser> CoMessengerUsers = new List<CMUser>();


        internal struct FullUserName : IComparable
        {
            public readonly string Domain;
            public readonly string UserName;

            public FullUserName(string domain, string userName)
            {
                Domain = domain;
                UserName = userName;
            }

            public int CompareTo(object obj)
            {
                if (obj is FullUserName)
                {
                    FullUserName that = (FullUserName)obj;
                    int d = String.Compare(this.Domain, that.Domain, false);
                    return d == 0 ? String.Compare(this.UserName, that.UserName, false) : d;
                }
                else
                    return 0;
            }
        }

        static internal SortedDictionary<FullUserName, CMUser> CoMessengerUsers = new SortedDictionary<FullUserName, CMUser>();
        static internal SortedDictionary<string, CMGroup> CoMessengerGroups = new SortedDictionary<string, CMGroup>();
        static SortedList<string, IMessagable> ReceiversList = new SortedList<string, IMessagable>();

        // set the TcpListener on port 13000
        static int port = 13000;
        static TcpListener server = new TcpListener(IPAddress.Any, port);

        static internal IndexedHistoryManager RoomsHistory = new IndexedHistoryManager("Storage\\History\\Rooms", true);
        static internal IndexedHistoryManager TempHistory = new IndexedHistoryManager("Storage\\History\\Temporary", true);

        static void NewMessagesIterator()
        {
            while (true)
            {
                //lock (clients)
                //{
                    foreach (ServerSideClient clnt in clients.ToList())
                    {
                        if (clnt != null && clnt.IncomingMessagesCount > 0)
                        {
                            //Console.WriteLine("{0} sent {1} bytes", clnt.tcp.Client.RemoteEndPoint.ToString(), clnt.tcp.Available);

                            CMMessage incomingMessage = clnt.RetrieveIncomingMessage();

                            switch (incomingMessage.Kind)
                            {

                                //Сообщение
                                case MessageKind.RoutedMessage:

                                    RoutedMessageHandler(incomingMessage);

                                    //clients.ForEach((a) => { a.PutOutMessage(queryMessage); });
                                    break;


                                //Авторизация
                                case MessageKind.Authorization:

                                    clnt.AuthorizationHandler(incomingMessage);

                                    break;

                                //case MessageKind.AuthorizationNewPassword:

                                    //clnt.User.Password = (string)incomingMessage.Message;

                                    //break;
                                case MessageKind.Disconnect:

                                    clnt.Disconnect();

                                    break;
                                case MessageKind.Ping:
                                    break;

                                //Клиент вошел в комнату
                                case MessageKind.EnterRoom:

                                    //Ищем группу по ID
                                    ServerRoomPeer enteredRoom = ReceiversList[(string)incomingMessage.Message] as ServerRoomPeer;

                                    //Ищем клиента по ID
                                    ServerPersonPeer joiner = (ServerPersonPeer)ReceiversList[clnt.User.AuthData.UserId];

                                    //Если клиент еще не в комнате, то добавляем
                                    if (!enteredRoom.Participants.Contains(joiner))
                                    {
                                        enteredRoom.Participants.Add(joiner);
                                        enteredRoom.Room.Participants.Add(joiner.Person);
                                    }

                                    //Уведомляем других присутствующих в комнате о вновь вошедшем пользователе



                                    enteredRoom.Participants.ForEach((a) =>
                                    {
                                        if (a.User.Client != null)
                                        {
                                            a.User.Client.PutOutgoingMessage(new CMMessage()
                                            {
                                                Kind = MessageKind.UpdatePeer,
                                                Message = enteredRoom.Peer()
                                            });
                                        }
                                    });

                                    break;
                                //Клиент покинул комнату
                                case MessageKind.LeaveRoom:

                                    //Ищем группу по ID
                                    ServerRoomPeer leavedRoom = ReceiversList[(string)incomingMessage.Message] as ServerRoomPeer;

                                    //Ищем клиента по ID
                                    ServerPersonPeer leaver = (ServerPersonPeer)ReceiversList[clnt.User.AuthData.UserId];

                                    //Если клиент еще не в комнате, то добавляем
                                    if (leavedRoom.Participants.Contains(leaver))
                                    {
                                        leavedRoom.Participants.Remove(leaver);
                                        leavedRoom.Room.Participants.Remove(leaver.Person);
                                    }

                                    CMMessage ImLeaving = new CMMessage()
                                    {
                                        Kind = MessageKind.UpdatePeer,
                                        Message = leavedRoom.Peer()
                                    };

                                    //Уведомляем других присутствующих в комнате
                                    leavedRoom.Participants.ForEach((a) =>
                                    {
                                        if (a.User.Client != null)
                                        {
                                            a.User.Client.PutOutgoingMessage(ImLeaving);
                                        }
                                    });

                                    //А так же самого ушедшего
                                    clnt.PutOutgoingMessage(ImLeaving);

                                    break;

                                    //Открыть новую комнату
                                case MessageKind.NewRoom:

                                    String roomName = incomingMessage.Message as String;

                                    if (!String.IsNullOrWhiteSpace(roomName))
                                    {
                                        //Создаем комнату
                                        ServerRoomPeer newRoom = new ServerRoomPeer()
                                        {
                                            Room = new RoomPeer()
                                            {
                                                DisplayName = roomName,

                                                //Список присутствующих в комнате для отправки клиентам (PersonPeer)
                                                //Participants = new List<PersonPeer>(),
                                                PeerId = Guid.NewGuid().ToString("N"),
                                                PeerType = PeerType.Room,
                                                IsMainRoom = false,
                                                State = PeerStatus.Common
                                            },

                                            //Список присутствующих в комнате для обработки на сервере
                                            Participants = new List<ServerPersonPeer>()
                                        };

                                        //Добавляем в список
                                        ReceiversList.Add(newRoom.Room.PeerId, newRoom);

                                        //Уведомляем всех
                                        clients.ForEach((a) => { a.PutOutgoingMessage(new CMMessage() { Kind = MessageKind.UpdatePeer, Message = newRoom.Peer() }); });
                                    }

                                    break;
                                    //Закрыть комнату
                                case MessageKind.CloseRoom:

                                    String RoomToCloseID = incomingMessage.Message as String;

                                    if (!String.IsNullOrWhiteSpace(RoomToCloseID))
                                    {
                                        ServerRoomPeer RoomToClose = ReceiversList[RoomToCloseID] as ServerRoomPeer;

                                        if (RoomToClose.Participants.Count == 1
                                            && RoomToClose.Participants[0].User == clnt.User)
                                        {
                                            //Удаляем комнату
                                            ReceiversList.Remove(RoomToClose.Room.PeerId);

                                            //Уведомляем всех
                                            clients.ForEach((a) => { a.PutOutgoingMessage(new CMMessage() { Kind = MessageKind.UpdatePeer, Message = RoomToClose.Peer() }); });
                                        }
                                    }

                                    break;


                                //Запрос на получении истории
                                case MessageKind.Query:

                                    ProcessQueryMessage(clnt, incomingMessage.Message as QueryMessage);

                                    break;

                                default:
                                    break;
                            }

                            //if (queryMessage.Kind == MessageKind.Text || queryMessage.Kind == MessageKind.RichText)
                            //    clients.ForEach((a) => { a.PutOutMessage(queryMessage); });

                            //SendToAll(data, data.Length);
                        }
                    }
                //}
                Thread.Sleep(1);
            }
        }

        private static void ProcessQueryMessage(CMClientBase clnt, QueryMessage queryMessage)
        {
            switch (queryMessage.Kind)
            {
                case QueryMessageKind.History:

                    HistoryQuery query = queryMessage.Message as HistoryQuery;

                    ServerRoomPeer queriedRoom = ReceiversList[query.PeerId] as ServerRoomPeer;

                    //answer.Content = queriedRoom.History.GetPrivateMessages(answer.From, 20).ToList();
                    query.Content.AddRange(RoomsHistory.GetRoomMessages(queriedRoom.Peer().PeerId, query.From, 20));

                    clnt.PutOutgoingMessage(new CMMessage()
                    {
                        Kind = MessageKind.Answer,
                        Message = new QueryMessage()
                        {
                            Kind = QueryMessageKind.History,
                            MessageId = queryMessage.MessageId,
                            Message = query                            
                        }
                    });


                    break;
                default:
                    break;
            }


        }


        private static void RoutedMessageHandler(CMMessage newmes)
        {
            RoutedMessage routedMessage = (RoutedMessage)newmes.Message;

            if (!ReceiversList.ContainsKey(routedMessage.Receiver))
                throw new InvalidOperationException("Не найдено ни группы ни пользователя по указанному сообщению");

            //Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.ffff", CultureInfo.CurrentCulture) + " " + routedMessage.MessageID + " received");
            ReceiversList[routedMessage.Receiver].ProcessMessage(routedMessage);

        }

        internal static void AcceptAuthorization(ServerSideClient clnt, CMUser FoundUser)
        {
            //Если уже был авторизован с другой машины - отключаем
            clients.FindAll((client) => client.User == FoundUser).ToList().ForEach(
                (client) =>
                {
                    if (client != null)
                    {
                        Console.WriteLine("Disconnecting " + client.Tcp.Client.RemoteEndPoint);
                        client.User.Client = null;
                        client.User = null;
                        client.PutOutgoingMessage(new CMMessage() { Kind = MessageKind.Disconnect });
                    }
                });


            clnt.User = FoundUser;
            FoundUser.Client = clnt;

            ServerPersonPeer peer = ReceiversList[FoundUser.AuthData.UserId] as ServerPersonPeer;

            peer.Person.State = PeerStatus.Online;
            //FoundUser.Status = PeerStatus.Online;

            Console.WriteLine(String.Format(CultureInfo.CurrentCulture,
                                "{1}({0}) Connected",
                                clnt.Tcp.Client.RemoteEndPoint.ToString(),
                                clnt.User != null ? clnt.User.UserId : null));

            //Подтверждаем авторизацию
            clnt.PutOutgoingMessage(new CMMessage()
            {
                Kind = MessageKind.Authorization,
                Message = FoundUser.UserId
            });

            clients.ForEach((a) => { a.PutOutgoingMessage(new CMMessage() { Kind = MessageKind.UpdatePeer, Message = ReceiversList[FoundUser.UserId].Peer() }); });

            clnt.PutOutgoingMessage(new CMMessage()
            {
                Kind = MessageKind.UsersList,
                Message = ReceiversList.Values.OfType<ServerRoomPeer>().Select<ServerRoomPeer, RoomPeer>(room => room.Room).ToList<RoomPeer>()
            });

            peer.OnAuthorizated();

            //List<RoomPeer> personlist = ReceiversList.Values.OfType<ServerRoomPeer>().ToList<RoomPeer>();

            //Отправляем последние 20 сообщений
            /*
            ReceiversList.ToList().ForEach(pair =>
            {
                ServerRoomPeer room = pair.Value as ServerRoomPeer;
                if (room != null)
                {
                    List<HistoryEntry> snapshot = new List<HistoryEntry>(room.MessagesBuffer);

                    snapshot.ForEach(entry =>
                        {
                            clnt.PutOutMessage(new CMMessage()
                            {
                                Kind = MessageKind.RoutedMessage,
                                Message = entry.routedMessage
                            });
                        });
                }
            });
            */
        }

        /// <summary>
        /// Обработчик очередей сообщений
        /// </summary>
        static void ProcessQueues()
        {
            while (true)
            {
                //lock (clients)
                //{

                    //foreach (CMClientBase a in clients.AsParallel())
                    //{
                    //    a.ProcessQueue();
                    //}

                List<CMClientBase> copy = new List<CMClientBase>(clients);

                copy.AsParallel().ToList().ForEach((a) => { if (a != null ) a.ProcessQueue(); });
                //}

                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Отслеживаем мертвых клиентов.
        /// </summary>
        static void CheckClientsAreAlive()
        {
            while (true)
            {
                //Проверяем всех клиентов
                clients.ForEach((a) => 
                {
                    if ((a.OutgoingMessagesCount == 0) && (a.LastActivity.AddMilliseconds(15000) < DateTime.Now))
                    { 
                        a.CheckAlive(); 
                    } 
                });

                //Удаляем мертвых
                //lock (clients)
                //{
                    clients.RemoveAll((a) => {return (a.State == ClientState.MarkedToKill || a.State == ClientState.Disconnected);});
                //}

                Thread.Sleep(1000);
            }
        }

        static void OnNewConnection(IAsyncResult iar)
        {

            try
            {
                server.BeginAcceptTcpClient(
                    callback: new AsyncCallback(OnNewConnection),
                    state: server
                    );

                ServerSideClient newclient = new ServerSideClient(server.EndAcceptTcpClient(iar));

                //newclient.ClientCulture = new CultureInfo("ru-RU");

                //Обмениваемся ключами
                newclient.WaitKey();

                newclient.SendKey();

                //Добавляем приконектившегося клиента в список
                //lock (clients)
                //{
                    clients.Add(newclient);
                //}



                Console.WriteLine(String.Format(CultureInfo.CurrentCulture, "{0} connecting...", newclient.Tcp.Client.RemoteEndPoint.ToString()));

                newclient.Disconnecting += 
                    (client, args) => 
                    {

                        ServerSideClient DisconnectingClient = client as ServerSideClient;

                        //Если был авторизован - ставим статус и рассылаем уведомления
                        if (DisconnectingClient.User != null)
                        {
                            ((ServerPersonPeer)ReceiversList[DisconnectingClient.User.UserId]).Person.State = PeerStatus.Offline;
                            clients.ForEach((a) => { a.PutOutgoingMessage(new CMMessage() { Kind = MessageKind.UpdatePeer, Message = ReceiversList[DisconnectingClient.User.UserId].Peer() }); });
                        }

                        Console.WriteLine(String.Format(CultureInfo.CurrentCulture, 
                                                        "{1}({0}) Disconnected", 
                                                        DisconnectingClient.Tcp.Client.RemoteEndPoint.ToString(),
                                                        DisconnectingClient.User != null ? DisconnectingClient.User.UserId : null));
                    };


            }
            catch (ObjectDisposedException)
            {
                //Слушающий сокет остановлен, просто игнорируем
            }
        }



        static void Main(string[] args)
        {

            

            Console.WriteLine("C-Messenger server v.0.0.0.1 Povyshev Nikolay © ");
            /*
            List<string> ab = LdapAuthority.GetDomainList() as List<string>;

            using (DirectoryEntry en = new DirectoryEntry("LDAP://"))
            using (DirectorySearcher srch = new DirectorySearcher("objectCategory=Domain"))
            {
                SearchResultCollection coll = null;

                try
                {
                    coll = srch.FindAll();
                }
                catch (Exception)
                {
                }
                // Enumerate over each returned domainEntry.
                foreach (SearchResult rs in coll)
                {
                    ResultPropertyCollection resultPropColl = rs.Properties;
                    foreach (object paramname in resultPropColl.PropertyNames)
                    {
                        Console.Write(paramname.ToString() + ": ");

                        foreach (object domainName in resultPropColl[paramname.ToString()])
                    {
                        Console.WriteLine(domainName.ToString());
                    }
                    }
                }
            }
            */


            Console.WriteLine("Loading plugins");

            var catalog = new AggregateCatalog();

            catalog.Catalogs.Add(new AssemblyCatalog(typeof(Server).Assembly));

            catalog.Catalogs.Add(new DirectoryCatalog("..\\..\\Plugins"));

            container = new CompositionContainer(catalog);
            plugins = container.GetExportedValues<IPlugin>();
            authPlugins = container.GetExportedValues<IAuthentication>();
            groupPlugins = container.GetExportedValues<IGroupCollector>();

            foreach (IPlugin plugin in plugins)
            {
                Console.WriteLine("Name: {0}, Author: {1}, Version: {2}", plugin.Name, plugin.Author, plugin.Version);
            }

            Thread NewMesagesThread = new Thread(new ThreadStart(NewMessagesIterator));
            NewMesagesThread.Start();

            Thread CheckClientsAreAliveThread = new Thread(new ThreadStart(CheckClientsAreAlive));
            CheckClientsAreAliveThread.Start();

            Thread ProcessQueuesThread = new Thread(new ThreadStart(ProcessQueues));
            ProcessQueuesThread.Start();

            //string usersplan = "userplan.xml";
            //string usersListFile = "builtinusers.xml";


            //Console.WriteLine("Get users plan using {0} ...", usersplan);

            //LdapAuthority LdapAuth = new LdapAuthority() { InfoStream = Console.Out };
            //UsersPlan Plan = LdapAuthority.GetUsersPlan(usersplan);

            //CoMessengerUsers.AddRange(LdapAuth.GetWindowsUsers(Plan).Where(user => !String.IsNullOrWhiteSpace(user.UserName) && !String.IsNullOrWhiteSpace(user.UserId)));

            //Console.WriteLine("Get builtin users using {0} ...", usersListFile);

            //try
            //{
            foreach (IAuthentication plugin in authPlugins)
            {
                plugin.ErrorStream = Console.Error;
                plugin.InfoStream = Console.Out;
                plugin.WarningStream = Console.Out;


                //plugin.ProcessMessage(new MessageArgs<object>(new List<ICimUser>(), Message.CollectingUsers));
                //Console.WriteLine("Name: {0}, Author: {1}, Version: {2}", plugin.Name, plugin.Author, plugin.Version);
                //CoMessengerUsers.AddRange(plugin.CollectUsers().Select(i => new CMUser() {);
                foreach (AuthenticationData auth in plugin.CollectUsers())
                {
                    if (CoMessengerUsers.ContainsKey(new FullUserName(auth.Domain ?? String.Empty, auth.UserName)))
                        Console.WriteLine("Warning, user {0}\\{1} is already exists", auth.Domain, auth.UserName);
                    else if (new List<string>(){ auth.DisplayName, auth.UserId, auth.UserName}.Any(value => String.IsNullOrWhiteSpace(value)))
                        Console.WriteLine("Warning, user {0} (Domain = {1}, UserName = {2}, UserId = {3}) ignored: one or more of requered fields are empty", auth.DisplayName, auth.Domain, auth.UserName, auth.UserId);
                    else
                        CoMessengerUsers.Add(new FullUserName(auth.Domain??String.Empty, auth.UserName), new CMUser() { AuthData = auth, UserId = auth.UserId });
                }
            }


            //CoMessengerUsers.AddRange(BuiltInAuthority.GetBuiltInUsers(usersListFile).Where(user => !String.IsNullOrWhiteSpace(user.UserName) && !String.IsNullOrWhiteSpace(user.UserId)));

            //}
            const string MainGroupID = "{8A4458F4-6AA9-40F4-ADD3-A45491EE8FA0}";

            //Console.WriteLine("done");

            //Основная группа пользователей
            CMGroup MainGroup =
                new CMGroup
                (
                    displayName: "All users",
                    groupId: MainGroupID,
                    userIds: CoMessengerUsers.Values.Select(user => user.UserId)

                    );

            CoMessengerGroups.Add(MainGroup.DisplayName, MainGroup);


            //Console.WriteLine("Get groups using {0} ...", usersplan);

            //CoMessengerGroups.AddRange(LdapAuth.GetGroups(Plan));
            foreach (IGroupCollector plugin in groupPlugins)
            {
                plugin.ErrorStream = Console.Error;
                plugin.InfoStream = Console.Out;
                plugin.WarningStream = Console.Out;

                foreach (Group group in plugin.CollectGroups())
                {
                    if (CoMessengerGroups.ContainsKey(group.DisplayName))
                        Console.WriteLine("Warning, group {0} is already exists", group.DisplayName);
                    else
                        CoMessengerGroups.Add(group.DisplayName, new CMGroup(group.DisplayName, group.GroupId, group.UserIds));
                }
            }


            //CoMessengerGroups.First().Users = CoMessengerUsers.FindAll(user => CoMessengerGroups.First().UserIDs.Contains(user.UserId));

            //CoMessengerUsers.Values. ForEach(user => 
            foreach (CMUser user in CoMessengerUsers.Values)
            {
                ReceiversList.Add(user.UserId, new ServerPersonPeer()
                {
                    User = user,
                    Person = new PersonPeer()
                    {
                        DisplayName = user.AuthData.DisplayName,
                        PeerId = user.UserId,
                        PeerType = PeerType.Person,
                        State = PeerStatus.Offline,
                        Avatar = File.Exists(Path.Combine(@"Storage/Avatars", string.Concat(user.UserId, ".png"))) ? System.IO.File.ReadAllBytes(Path.Combine(@"Storage/Avatars", string.Concat(user.UserId, ".png"))) : null
                    }
                });

            }

            //CoMessengerGroups.ForEach(
            //    (group) =>
            foreach (CMGroup group in CoMessengerGroups.Values)
            {
                ServerRoomPeer Room = new ServerRoomPeer();

                Room.Room = new RoomPeer()
                {
                    DisplayName = group.DisplayName,
                    PeerId = group.GroupId,
                    PeerType = PeerType.Room,
                    IsMainRoom = group.GroupId == MainGroupID ? true : false,
                    State = PeerStatus.Common
                };

                Room.Room.Participants.AddRange(group.UserIds.Where(userID => ReceiversList.ContainsKey(userID)). //Выбираем из группы только тех, кто в списке наших пользователей
                         Select(userID => ((ServerPersonPeer)ReceiversList[userID]).Person). //По ID находим получателей
                         ToList());

                Room.Participants = group.UserIds.Where(userID => ReceiversList.ContainsKey(userID)).Select(userID => ReceiversList[userID] as ServerPersonPeer).ToList();


                ReceiversList.Add(group.GroupId, Room);

            }
            /*
            ReceiversList.ToList().ForEach(pair =>
                {
                    ServerRoomPeer room = pair.Value as ServerRoomPeer;
                    if (room != null)
                    {
                        room.Init();
                    }
                });
            */
            //List<Peer> newl = ReceiversList.Select<KeyValuePair<string, IMessagable>, Peer>(pair => { if (pair.Value is ServerPersonPeer) return pair.Value as PersonPeer; else return pair.Value as RoomPeer; }).ToList<Peer>();

            //List<RoomPeer> personlist = ReceiversList.Values.OfType<ServerRoomPeer>().ToList<RoomPeer>();

            //foreach (var Room in personlist)
            //{
            //    Console.WriteLine(Room.DisplayName);
            //}

            //ServerPersonPeer sp = new ServerPersonPeer() { DisplayName = "abc", Receiver = CoMessengerUsers[0], Type = PeerType.Person, PeerID = "1" };

            //PersonPeer pp = (PersonPeer)sp;

            //List<PersonPeer> lp = new List<PersonPeer>();

            //lp.Add(pp);


            //System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(List<PersonPeer>), new Type[] { typeof(List<ServerPersonPeer>), typeof(List<ServerRoomPeer>) });
            ////System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(List<RoomPeer>), new Type[] { typeof(List<ServerRoomPeer>), typeof(List<ServerPersonPeer>) });

            //using (System.IO.StreamWriter writer = new System.IO.StreamWriter("test.xml"))
            //{
            //    serializer.Serialize(writer, lp);
            //}

            int i = 0;
            Console.WriteLine("Common Users List:");

            if (CoMessengerUsers.Count == 0) Console.WriteLine("No users were found!");

            //CoMessengerUsers.ForEach((element) => 
            //{
            //    //element.Status = PeerStatus.Offline;
            //    Console.WriteLine("{0} {1}", ++i, element.DisplayName); 
            //});

            foreach (CMUser user in CoMessengerUsers.Values)
            {
                Console.WriteLine("{0} {1}", ++i, user.AuthData.DisplayName);
            }

            Console.WriteLine("Starting listener...");

            server.Start();

            // Запускаем прослушивание в отдельном потоке
            server.BeginAcceptTcpClient(
                callback: new AsyncCallback(OnNewConnection),
                state: server
                );

            Console.WriteLine("Listening on {0}...", server.LocalEndpoint);

            bool notStop = true;

            while (notStop)
            {
                Console.Write(">");
                string msg = Console.ReadLine();

                if (msg == "e" || msg == "exit")
                    notStop = false;
                //else if (!string.IsNullOrEmpty(msg))
                //    clients.ForEach((a) => { a.PutOutMessage(new CMMessage() { Kind = MessageKind.Text, Message = msg }); });
            }

            NewMesagesThread.Abort();
            CheckClientsAreAliveThread.Abort();
            ProcessQueuesThread.Abort();

        }
    }
}
