using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using CorporateMessengerLibrary;
using System.Runtime.Serialization.Formatters.Binary;
using System.Globalization;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.DirectoryServices;
using System.IO;
using System.Windows.Media.Imaging;
using System.Diagnostics;

namespace SimpleChat
{
    class Server
    {
        static private List<CMClientBase> clients = new List<CMClientBase>();
        static List<CMUser> CoMessengerUsers = new List<CMUser>();
        static List<CMGroup> CoMessengerGroups = new List<CMGroup>();
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
                    foreach (CMClientBase clnt in clients.ToList())
                    {
                        if (clnt != null && clnt.InMessagesCount > 0)
                        {
                            //Console.WriteLine("{0} sent {1} bytes", clnt.tcp.Client.RemoteEndPoint.ToString(), clnt.tcp.Available);

                            CMMessage newmes = clnt.RetrieveInMessage();

                            switch (newmes.Kind)
                            {

                                //Сообщение
                                case MessageKind.RoutedMessage:

                                    RoutedMessageHandler(newmes);

                                    //clients.ForEach((a) => { a.PutOutMessage(queryMessage); });
                                    break;


                                //Авторизация
                                case MessageKind.Authorization:

                                    AuthorizationHandler(clnt, newmes);

                                    break;

                                case MessageKind.AuthorizationNewPassword:

                                    clnt.User.Password = (string)newmes.Message;

                                    break;
                                case MessageKind.Disconnect:

                                    clnt.Disconnect();

                                    break;
                                case MessageKind.Ping:
                                    break;

                                //Клиент вошел в комнату
                                case MessageKind.EnterRoom:

                                    //Ищем группу по ID
                                    ServerRoomPeer enteredRoom = ReceiversList[(string)newmes.Message] as ServerRoomPeer;

                                    //Ищем клиента по ID
                                    ServerPersonPeer joiner = (ServerPersonPeer)ReceiversList[clnt.User.UserId];

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
                                            a.User.Client.PutOutMessage(new CMMessage()
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
                                    ServerRoomPeer leavedRoom = ReceiversList[(string)newmes.Message] as ServerRoomPeer;

                                    //Ищем клиента по ID
                                    ServerPersonPeer leaver = (ServerPersonPeer)ReceiversList[clnt.User.UserId];

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
                                            a.User.Client.PutOutMessage(ImLeaving);
                                        }
                                    });

                                    //А так же самого ушедшего
                                    clnt.PutOutMessage(ImLeaving);

                                    break;

                                    //Открыть новую комнату
                                case MessageKind.NewRoom:

                                    String roomName = newmes.Message as String;

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
                                        clients.ForEach((a) => { a.PutOutMessage(new CMMessage() { Kind = MessageKind.UpdatePeer, Message = newRoom.Peer() }); });
                                    }

                                    break;
                                    //Закрыть комнату
                                case MessageKind.CloseRoom:

                                    String RoomToCloseID = newmes.Message as String;

                                    if (!String.IsNullOrWhiteSpace(RoomToCloseID))
                                    {
                                        ServerRoomPeer RoomToClose = ReceiversList[RoomToCloseID] as ServerRoomPeer;

                                        if (RoomToClose.Participants.Count == 1
                                            && RoomToClose.Participants[0].User == clnt.User)
                                        {
                                            //Удаляем комнату
                                            ReceiversList.Remove(RoomToClose.Room.PeerId);

                                            //Уведомляем всех
                                            clients.ForEach((a) => { a.PutOutMessage(new CMMessage() { Kind = MessageKind.UpdatePeer, Message = RoomToClose.Peer() }); });
                                        }
                                    }

                                    break;


                                //Запрос на получении истории
                                case MessageKind.Query:

                                    ProcessQueryMessage(clnt, newmes.Message as QueryMessage);

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

                    clnt.PutOutMessage(new CMMessage()
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

        private static void AuthorizationHandler(CMClientBase clnt, CMMessage newmes)
        {
            CMUser ReceivedUser = newmes.Message as CMUser;

            CMUser FoundedUser = null;

            if (ReceivedUser == null)
            {
                clnt.PutOutMessage(new CMMessage()
                {
                    Kind = MessageKind.AuthorizationError,
                    Message = ErrorKind.UserNotPresented
                });
            }

            //Без авторизации - вход под текущим пользователем
            if (!(String.IsNullOrEmpty(ReceivedUser.UserId)))
            {
                FoundedUser = CoMessengerUsers.Find((UserInList) => { return (UserInList.UserId == ReceivedUser.UserId); });
                if (FoundedUser != null)
                {
                    AcceptAuthorization(clnt, FoundedUser);
                }
                else
                {
                    clnt.PutOutMessage(new CMMessage()
                    {
                        Kind = MessageKind.AuthorizationError,
                        Message = ErrorKind.UserNotFound
                    });
                }
            }
            //Доменная авторизация
            else if (!(String.IsNullOrEmpty(ReceivedUser.Domain)))
            {
                //расшифруем жулебный пароль
                ReceivedUser.Password = clnt.DecryptPassword(ReceivedUser.EncryptedPassword);

                FoundedUser = CoMessengerUsers.Find((UserInList) => { return UserInList.UserName.ToLower() == ReceivedUser.UserName.ToLower(); });

                if (FoundedUser != null)
                {
                    try
                    {
                        PrincipalContext prCont = new PrincipalContext(ContextType.Domain, ReceivedUser.Domain);

                        if (prCont.ValidateCredentials(ReceivedUser.UserName, ReceivedUser.Password))
                        {
                            AcceptAuthorization(clnt, FoundedUser);
                        }
                        else
                        {
                            clnt.PutOutMessage(new CMMessage()
                            {
                                Kind = MessageKind.AuthorizationError,
                                Message = ErrorKind.WrongPassword
                            });
                        }
                    }
                    catch (PrincipalServerDownException)
                    {
                        clnt.PutOutMessage(new CMMessage()
                        {
                            Kind = MessageKind.AuthorizationError,
                            Message = ErrorKind.DomainCouldNotBeContacted
                        });
                    }

                }
                else
                {
                    clnt.PutOutMessage(new CMMessage()
                    {
                        Kind = MessageKind.AuthorizationError,
                        Message = ErrorKind.UserNotFound
                    });
                }


            }
            //Встроенная авторизация
            else
            {
                //расшифруем жулебный пароль
                ReceivedUser.Password = clnt.DecryptPassword(ReceivedUser.EncryptedPassword);

                FoundedUser = CoMessengerUsers.Find((UserInList) => { return UserInList.UserName.ToLower() == ReceivedUser.UserName.ToLower(); });


                //Нет такой буквы в этом слове
                if (FoundedUser == null)
                {
                    clnt.PutOutMessage(new CMMessage()
                    {
                        Kind = MessageKind.AuthorizationError,
                        Message = ErrorKind.UserNotFound
                    });
                }
                else
                {
                    //Сектор приз на барабане!
                    if (
                            FoundedUser.Password == MD5Helper.CreateMD5(ReceivedUser.Password)   //Верный пароль
                        || (FoundedUser.Password == String.Empty && ReceivedUser.Password == String.Empty) //Пароль не задан
                        )
                    {
                        AcceptAuthorization(clnt, FoundedUser);
                    }
                    else
                    {
                        clnt.PutOutMessage(new CMMessage()
                        {
                            Kind = MessageKind.AuthorizationError,
                            Message = ErrorKind.WrongPassword
                        });
                    }
                }
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

        private static void AcceptAuthorization(CMClientBase clnt, CMUser FoundUser)
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
                        client.PutOutMessage(new CMMessage() { Kind = MessageKind.Disconnect });
                    }
                });


            clnt.User = FoundUser;
            FoundUser.Client = clnt;

            ServerPersonPeer peer = ReceiversList[FoundUser.UserId] as ServerPersonPeer;

            peer.Person.State = PeerStatus.Online;
            //FoundUser.Status = PeerStatus.Online;

            Console.WriteLine(String.Format(CultureInfo.CurrentCulture,
                                "{1}({0}) Connected",
                                clnt.Tcp.Client.RemoteEndPoint.ToString(),
                                clnt.User != null ? clnt.User.UserId : null));

            //Подтверждаем авторизацию
            clnt.PutOutMessage(new CMMessage()
            {
                Kind = MessageKind.Authorization,
                Message = FoundUser
            });

            clients.ForEach((a) => { a.PutOutMessage(new CMMessage() { Kind = MessageKind.UpdatePeer, Message = ReceiversList[FoundUser.UserId].Peer() }); });

            clnt.PutOutMessage(new CMMessage()
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
                    if ((a.OutMessagesCount == 0) && (a.LastActivity.AddMilliseconds(15000) < DateTime.Now))
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

                CMClientBase newclient = new CMClientBase(server.EndAcceptTcpClient(iar));

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

                        CMClientBase DisconnectingClient = client as CMClientBase;

                        //Если был авторизован - ставим статус и рассылаем уведомления
                        if (DisconnectingClient.User != null)
                        {
                            ((ServerPersonPeer)ReceiversList[DisconnectingClient.User.UserId]).Person.State = PeerStatus.Offline;
                            clients.ForEach((a) => { a.PutOutMessage(new CMMessage() { Kind = MessageKind.UpdatePeer, Message = ReceiversList[DisconnectingClient.User.UserId].Peer() }); });
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


            Thread NewMesagesThread = new Thread(new ThreadStart(NewMessagesIterator));
            NewMesagesThread.Start();

            Thread CheckClientsAreAliveThread = new Thread(new ThreadStart(CheckClientsAreAlive));
            CheckClientsAreAliveThread.Start();

            Thread ProcessQueuesThread = new Thread(new ThreadStart(ProcessQueues));
            ProcessQueuesThread.Start();

            string usersplan = "userplan.xml";
            string usersListFile = "builtinusers.xml";


            Console.WriteLine("Get users plan using {0} ...", usersplan);

            LdapAuthority LdapAuth = new LdapAuthority() { InfoStream = Console.Out };
            UsersPlan Plan = LdapAuthority.GetUsersPlan(usersplan);

            CoMessengerUsers.AddRange(LdapAuth.GetWindowsUsers(Plan));

            try
            {
                CoMessengerUsers.AddRange(BuiltInAuthority.GetBuiltInUsers(usersListFile));

            }
            catch (FileNotFoundException fn)
            {
                Console.WriteLine(fn.Message);
            }
            const string MainGroupID = "{8A4458F4-6AA9-40F4-ADD3-A45491EE8FA0}";

            Console.WriteLine("done");

            //Основная группа пользователей
            CMGroup MainGroup = new CMGroup();

            MainGroup.UserIds.AddRange(CoMessengerUsers.Select<CMUser, string>((user) => { return user.UserId; }));
            MainGroup.GroupId = MainGroupID;
            MainGroup.DisplayName = "All users";

            CoMessengerGroups.Add(MainGroup);

            CoMessengerGroups.AddRange(LdapAuth.GetGroups(Plan));


            //CoMessengerGroups.First().Users = CoMessengerUsers.FindAll(user => CoMessengerGroups.First().UserIDs.Contains(user.UserId));




            CoMessengerUsers.ForEach(user => ReceiversList.Add(user.UserId, new ServerPersonPeer()
            {
                User = user,
                Person = new PersonPeer()
                {
                    DisplayName = user.DisplayName,
                    PeerId = user.UserId,
                    PeerType = PeerType.Person,
                    State = PeerStatus.Offline,
                    Avatar = File.Exists(Path.Combine(@"Storage/Avatars", String.Concat(user.UserId, ".png"))) ? System.IO.File.ReadAllBytes(Path.Combine(@"Storage/Avatars", String.Concat(user.UserId, ".png"))) : null
                }

            }));
            CoMessengerGroups.ForEach(
                (group) =>
                {
                    ServerRoomPeer Room = new ServerRoomPeer();

                    Room.Room = new RoomPeer()
                    {
                        DisplayName = group.DisplayName,
                        PeerId      = group.GroupId,
                        PeerType        = PeerType.Room,
                        IsMainRoom  = group.GroupId == MainGroupID ? true : false,
                        State       = PeerStatus.Common
                    };

                    Room.Room.Participants.AddRange(group.UserIds.Where(userID => ReceiversList.ContainsKey(userID)). //Выбираем из группы только тех, кто в списке наших пользователей
                             Select<string, PersonPeer>(userID => ((ServerPersonPeer)ReceiversList[userID]).Person). //По ID находим получателей
                             ToList<PersonPeer>());

                    Room.Participants = group.UserIds.Where(userID => ReceiversList.ContainsKey(userID)).Select<string, ServerPersonPeer>(userID => ReceiversList[userID] as ServerPersonPeer).ToList<ServerPersonPeer>();


                    ReceiversList.Add(group.GroupId, Room);

                });
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
            CoMessengerUsers.ForEach((element) => 
            {
                //element.Status = PeerStatus.Offline;
                Console.WriteLine("{0} {1}", ++i, element.DisplayName); 
            });

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
