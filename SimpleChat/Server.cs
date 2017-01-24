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
using System.Collections.Concurrent;

namespace SimpleChat
{
    public class Server
    {

        enum ServerState
        {
            Stopped,
            Running
        }

        ServerState state;

        private CompositionContainer container;


        [ImportMany]
        private IEnumerable<IPlugin> plugins;
        private IEnumerable<IAuthentication> authPlugins;
        private IEnumerable<IGroupCollector> groupPlugins;

        private List<IPlugin> disabledPlugins;

        private ConcurrentList<ServerSideClient> clients = new ConcurrentList<ServerSideClient>();
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
                    int d = String.Compare(this.Domain, that.Domain, StringComparison.OrdinalIgnoreCase);
                    return d == 0 ? String.Compare(this.UserName, that.UserName, StringComparison.OrdinalIgnoreCase) : d;
                }
                else
                    return 0;
            }
        }

        internal SortedDictionary<FullUserName, CMUser> CoMessengerUsers = new SortedDictionary<FullUserName, CMUser>();
        internal SortedDictionary<string, CMGroup> CoMessengerGroups = new SortedDictionary<string, CMGroup>();
        SortedList<string, IMessageReceiver> ReceiversList = new SortedList<string, IMessageReceiver>();

        internal SortedSet<string> RemovedReceivers = new SortedSet<string>();


        // set the TcpListener on port 13000
        int port = 13000;
        TcpListener server;

        internal static IndexedHistoryManager RoomsHistory = new IndexedHistoryManager("Storage\\History\\Rooms", true);
        internal static IndexedHistoryManager TempHistory = new IndexedHistoryManager("Storage\\History\\Temporary", true);

        private Server()
        {
            server = new TcpListener(IPAddress.Any, port);
        }


        private static Server instance;

        public static Server Instance
        {
            get
            {
                if (instance == null)
                    instance = new Server();

                return instance;
            }
        }


        void NewMessagesIterator()
        {
            while (state == ServerState.Running)
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
                                    ServerPersonPeer joiner = (ServerPersonPeer)ReceiversList[clnt.User.Authentication.UserId];

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
                                    ServerPersonPeer leaver = (ServerPersonPeer)ReceiversList[clnt.User.Authentication.UserId];

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
                                            }
                                        };

                                        //Добавляем в список
                                        ReceiversList.Add(newRoom.Room.PeerId, newRoom);

                                        //Уведомляем всех
                                        //clients.ForEach((a) => { a.PutOutgoingMessage(new CMMessage() { Kind = MessageKind.UpdatePeer, Message = newRoom.Peer() }); });
                                        SendTo(clients, new CMMessage() { Kind = MessageKind.UpdatePeer, Message = newRoom.Peer() });
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
                                            //clients.ForEach((a) => { a.PutOutgoingMessage(new CMMessage() { Kind = MessageKind.UpdatePeer, Message = RoomToClose.Peer() }); });
                                            SendTo(clients, new CMMessage() { Kind = MessageKind.UpdatePeer, Message = RoomToClose.Peer() });
                                        }
                                    }

                                    break;


                                //Запрос на получении истории
                                case MessageKind.Query:

                                    ProcessQueryMessage(clnt, incomingMessage.Message as QueryMessage);

                                    break;

                            case MessageKind.BinaryContent:

                                byte[] content = Compressing.Decompress(incomingMessage.Message as byte[]);

                                TempHistory.SaveBinary(content);

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

        private void ProcessQueryMessage(CMClientBase clnt, QueryMessage queryMessage)
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

                case QueryMessageKind.Binary:

                    string hash = queryMessage.Message as string; 

                    if (hash != null)
                    {
                        byte[] binary = TempHistory.RestoreBinary(hash);

                        if (binary != null)
                            binary = Compressing.Compress(binary);


                        clnt.PutOutgoingMessage(new CMMessage()
                        {
                            Kind = MessageKind.Answer,
                            Message = new QueryMessage()
                            {
                                Kind = QueryMessageKind.Binary,
                                MessageId = queryMessage.MessageId,
                                Message = binary //null if binary not found
                            }
                        });
                    }

                    break;
                default:
                    break;
            }


        }

        private static void SendTo(IEnumerable<ServerSideClient> recipients, CMMessage message)
        {
            foreach (ServerSideClient client in recipients)
            {
                client.PutOutgoingMessage(message);
            }
        }

        private void RoutedMessageHandler(CMMessage newmes)
        {
            RoutedMessage routedMessage = (RoutedMessage)newmes.Message;

            if (!ReceiversList.ContainsKey(routedMessage.Receiver))
                throw new InvalidOperationException("Не найдено ни группы ни пользователя по указанному сообщению");

            //Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.ffff", CultureInfo.CurrentCulture) + " " + routedMessage.MessageID + " received");
            ReceiversList[routedMessage.Receiver].ProcessMessage(routedMessage);

        }

        internal void AcceptAuthorization(ServerSideClient newClient, CMUser FoundUser)
        {
            //Если уже был авторизован с другой машины - отключаем
            clients.Where((client) => client.User == FoundUser).ToList().ForEach(
                (client) =>
                {
                    if (client != null && client.State != ClientState.Disconnected)
                    {
                        Console.WriteLine("Disconnecting " + client.Tcp.Client.RemoteEndPoint);
                        client.User.Client = null;
                        client.User = null;
                        client.PutOutgoingMessage(new CMMessage() { Kind = MessageKind.Disconnect });
                    }
                });

            newClient.User = FoundUser;
            FoundUser.Client = newClient;

            ServerPersonPeer peer = ReceiversList[FoundUser.Authentication.UserId] as ServerPersonPeer;

            peer.Person.State = PeerStatus.Online;
            //FoundUser.Status = PeerStatus.Online;

            Console.WriteLine(String.Format(CultureInfo.CurrentCulture,
                                "{1}({0}) Connected",
                                newClient.Tcp.Client.RemoteEndPoint.ToString(),
                                newClient.User != null ? newClient.User.UserId : null));

            //Подтверждаем авторизацию
            newClient.PutOutgoingMessage(new CMMessage()
            {
                Kind = MessageKind.Authorization,
                Message = FoundUser.UserId
            });

            //clients.ForEach((a) => { a.PutOutgoingMessage(new CMMessage() { Kind = MessageKind.UpdatePeer, Message = ReceiversList[FoundUser.UserId].Peer() }); });
            SendTo(clients, new CMMessage() { Kind = MessageKind.UpdatePeer, Message = ReceiversList[FoundUser.UserId].Peer() });

            newClient.PutOutgoingMessage(new CMMessage()
            {
                Kind = MessageKind.UsersList,
                Message = ReceiversList.Values.OfType<ServerRoomPeer>().Select<ServerRoomPeer, RoomPeer>(room => room.Room).ToList<RoomPeer>()
            });

            peer.OnAuthorizationConfirmed();

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
        void ProcessQueues()
        {
            while (state == ServerState.Running)
            {
                //lock (clients)
                //{

                //foreach (CMClientBase a in clients.AsParallel())
                //{
                //    a.ProcessQueue();
                //}

                //List<CMClientBase> copy = new List<CMClientBase>(clients);

                //copy.AsParallel().ToList().ForEach((a) => { if (a != null) a.ProcessQueue(); });

                foreach (ServerSideClient client in clients)
                {
                    client.ProcessQueue();
                }

                //}

                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Отслеживаем мертвых клиентов.
        /// </summary>
        void CheckClientsAreAlive()
        {
            while (state == ServerState.Running)
            {
                //Проверяем всех клиентов
                foreach (ServerSideClient client in clients)
                {
                    if ((client.OutgoingMessagesCount == 0) && (client.LastActivity.AddMilliseconds(15000) < DateTime.Now))
                    { 
                        client.CheckAlive(); 
                    } 
                }

                //Удаляем мертвых
                //lock (clients)
                //{
                    clients.RemoveAll((a) => {return (a.State == ClientState.MarkedToKill || a.State == ClientState.Disconnected);});
                //}

                Thread.Sleep(1000);
            }
        }

        void OnNewConnection(IAsyncResult iar)
        {

            try
            {
                server.BeginAcceptTcpClient(
                    callback: new AsyncCallback(OnNewConnection),
                    state: server
                    );

                ServerSideClient newclient = new ServerSideClient(server.EndAcceptTcpClient(iar));

                newclient.ErrorStream = newclient.InfoStream = newclient.WarningStream = Console.Out;

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
                            //clients.ForEach((a) => { a.PutOutgoingMessage(new CMMessage() { Kind = MessageKind.UpdatePeer, Message = ReceiversList[DisconnectingClient.User.UserId].Peer() }); });
                            SendTo(clients, new CMMessage() { Kind = MessageKind.UpdatePeer, Message = ReceiversList[DisconnectingClient.User.UserId].Peer() });
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



        public void Start()
        {
            state = ServerState.Running;


            Console.WriteLine("C-Messenger server v.0.0.0.1 Povyshev Nikolay © ");

            LoadPlugins();

            Thread NewMesagesThread = new Thread(new ThreadStart(NewMessagesIterator));
            NewMesagesThread.Start();

            Thread CheckClientsAreAliveThread = new Thread(new ThreadStart(CheckClientsAreAlive));
            CheckClientsAreAliveThread.Start();

            Thread ProcessQueuesThread = new Thread(new ThreadStart(ProcessQueues));
            ProcessQueuesThread.Start();

            UpdateUsers(true);

            UpdateGroups(true);

            Console.WriteLine("Starting listener...");

            try
            {
                server.Start();

                // Запускаем прослушивание в отдельном потоке
                server.BeginAcceptTcpClient(
                    callback: new AsyncCallback(OnNewConnection),
                    state: server
                    );
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                Console.WriteLine("Address {server.LocalEndpoint} is already in use");
            }

            Console.WriteLine("Listening on {0}...", server.LocalEndpoint);

            //ProcessConsoleInput();





        }


        public void Stop()
        {
            state = ServerState.Stopped;
            //NewMesagesThread.Abort();
            //CheckClientsAreAliveThread.Abort();
            //ProcessQueuesThread.Abort();
        }

        private void UpdateGroups(bool IsInitialization)
        {
            const string AllUsersGroupId = "{8A4458F4-6AA9-40F4-ADD3-A45491EE8FA0}";

            List<CMGroup> newGroups = new List<CMGroup>();

            if (IsInitialization)
            {

                CMGroup AllUsersGroup =
                new CMGroup
                (
                    displayName: "All users group",
                    groupId: AllUsersGroupId,
                    userIds: CoMessengerUsers.Values.Select(user => user.UserId)
                );

                //CoMessengerGroups.Add(AllUsersGroup.DisplayName, AllUsersGroup);
                newGroups.Add(AllUsersGroup);
            }


            foreach (IGroupCollector plugin in groupPlugins)
            {
                if (disabledPlugins.Contains(plugin))
                    continue;

                IEnumerable<Group> ActualGroups;

                try
                {
                    plugin.ErrorStream = Console.Error;
                    plugin.InfoStream = Console.Out;
                    plugin.WarningStream = Console.Out;

                    ActualGroups = plugin.CollectGroups();
                }
                catch (Exception)
                {
                    disabledPlugins.Add(plugin);
                    Console.WriteLine(String.Format(CultureInfo.CurrentUICulture, "An error has occurred in plugin {0}. The plugin has been disabled", plugin.Name));
                    continue;
                }

                foreach (CMGroup removedGroup in CoMessengerGroups.Values.Where(group => !ActualGroups.Any(newGroup => newGroup.GroupId == group.GroupId)))
                {
                    RemovedReceivers.Add(removedGroup.GroupId);
                }


                foreach (Group group in ActualGroups)
                {
                    if (CoMessengerGroups.ContainsKey(group.DisplayName))
                    {
                        if (IsInitialization)
                            Console.WriteLine("Warning, group {0} already exists", group.DisplayName);
                    }
                    else if (ReceiversList.ContainsKey(group.GroupId))
                    {
                        if (IsInitialization)
                            Console.WriteLine("Warning, group or user with Id {0} already exists", group.GroupId);
                    }
                    else
                    {
                        newGroups.Add(new CMGroup(group.DisplayName, group.GroupId, group.UserIds));
                    }
                }

            }

            foreach (CMGroup newGroup in newGroups)
            {
                CoMessengerGroups.Add(newGroup.DisplayName, newGroup);

                ServerRoomPeer Room = new ServerRoomPeer();

                Room.Room = new RoomPeer()
                {
                    DisplayName = newGroup.DisplayName,
                    PeerId = newGroup.GroupId,
                    PeerType = PeerType.Room,
                    IsMainRoom = newGroup.GroupId == AllUsersGroupId ? true : false,
                    State = PeerStatus.Common
                };

                Room.Room.Participants.AddRange(newGroup.UserIds.Where(userID => ReceiversList.ContainsKey(userID)). //Выбираем из группы только тех, кто в списке наших пользователей
                         Select(userID => ((ServerPersonPeer)ReceiversList[userID]).Person)); //По ID находим получателей

                Room.Participants.AddRange(newGroup.UserIds.Where(userID => ReceiversList.ContainsKey(userID)).Select(userID => ReceiversList[userID] as ServerPersonPeer));


                ReceiversList.Add(newGroup.GroupId, Room);

            }
        }

        private void UpdateUsers(bool IsInitialization)
        {

            List<AuthenticationData> newUsers = new List<AuthenticationData>();

            foreach (IAuthentication plugin in authPlugins)
            {

                if (disabledPlugins.Contains(plugin))
                    continue;

                IEnumerable<AuthenticationData> ActualUsers;

                try
                {
                    plugin.ErrorStream = Console.Error;
                    plugin.InfoStream = Console.Out;
                    plugin.WarningStream = Console.Out;

                    ActualUsers = plugin.CollectUsers();

                }
                catch (Exception)
                {
                    disabledPlugins.Add(plugin);
                    Console.WriteLine(String.Format(CultureInfo.CurrentUICulture, "An error has occurred in plugin {0}. The plugin has been disabled", plugin.Name));
                    continue;
                }

                foreach (CMUser removedUser in CoMessengerUsers.Values.Where(user => !ActualUsers.Any(newUser => newUser.UserId == user.UserId)))
                {
                    RemovedReceivers.Add(removedUser.UserId);
                }

                foreach (AuthenticationData auth in ActualUsers)
                {
                    if (CoMessengerUsers.ContainsKey(new FullUserName(auth.Domain ?? String.Empty, auth.UserName)) ||
                        newUsers.Any(existingAuth => String.Equals(existingAuth.Domain, auth.Domain, StringComparison.OrdinalIgnoreCase) &&
                                                     String.Equals(existingAuth.UserName, auth.UserName, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (IsInitialization)
                            Console.WriteLine("Warning, user {0}\\{1} already exists", auth.Domain, auth.UserName);
                    }
                    else if (new List<string>() { auth.DisplayName, auth.UserId, auth.UserName }.Any(value => String.IsNullOrWhiteSpace(value)))
                    {
                        Console.WriteLine("Warning, user {0} (Domain = {1}, UserName = {2}, UserId = {3}) ignored: one or more of required fields are empty", auth.DisplayName, auth.Domain, auth.UserName, auth.UserId);
                    }
                    else if (ReceiversList.ContainsKey(auth.UserId) ||
                             newUsers.Any(existingAuth => String.Equals(existingAuth.UserId, auth.UserId, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (IsInitialization)
                            Console.WriteLine("Warning, group or user with Id {0} already exists", auth.UserId);
                    }
                    else
                    {
                        newUsers.Add(auth);
                    }
                }
            }

            foreach (AuthenticationData user in newUsers)
            {
                CMUser newUser = new CMUser() { Authentication = user, UserId = user.UserId };

                CoMessengerUsers.Add(new FullUserName(user.Domain ?? String.Empty, user.UserName), newUser);

                ReceiversList.Add(user.UserId, new ServerPersonPeer()
                {
                    User = newUser,
                    Person = new PersonPeer()
                    {
                        DisplayName = user.DisplayName,
                        PeerId = user.UserId,
                        PeerType = PeerType.Person,
                        State = PeerStatus.Offline,
                        Avatar = File.Exists(Path.Combine(@"Storage/Avatars", string.Concat(user.UserId, ".png"))) ? System.IO.File.ReadAllBytes(Path.Combine(@"Storage/Avatars", string.Concat(user.UserId, ".png"))) : null
                    }
                });
            }
        }

        private void LoadPlugins()
        {
            Console.WriteLine("Loading plugins");

            using (AggregateCatalog catalog = new AggregateCatalog())
            {
                catalog.Catalogs.Add(new AssemblyCatalog(typeof(Server).Assembly));

                //catalog.Catalogs.Add(new DirectoryCatalog("..\\..\\Plugins"));
                catalog.Catalogs.Add(new DirectoryCatalog("Plugins"));

                container = new CompositionContainer(catalog);
                plugins = container.GetExportedValues<IPlugin>();
                authPlugins = container.GetExportedValues<IAuthentication>();
                groupPlugins = container.GetExportedValues<IGroupCollector>();

                disabledPlugins = new List<IPlugin>();

                foreach (IPlugin plugin in plugins)
                {
                    Console.WriteLine("Name: {0}, Author: {1}, Version: {2}", plugin.Name, plugin.Author, plugin.Version);
                }
            }
        }

        public void ProcessConsoleInput()
        {
            bool notStop = true;

            while (notStop)
            {
                Console.Write(">");
                string msg = Console.ReadLine().ToUpperInvariant();

                switch (msg)
                {
                    case "E":
                    case "EXIT":
                        return;

                    case "LIST":

                        Console.WriteLine("Current users");

                        int i = 1;

                        foreach (var User in CoMessengerUsers.Values)
                        {
                            Console.WriteLine(String.Format(CultureInfo.CurrentUICulture, "{0} {1}", i++, User.Authentication.DisplayName));
                        }

                        break;


                    default:
                        break;
                }
            }
        }
    }
}
