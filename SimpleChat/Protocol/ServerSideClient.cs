using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using CorporateMessengerLibrary;
using SimpleChat.Identity;

namespace SimpleChat.Protocol
{
    internal class ServerSideClient : CMClientBase
    {
        internal CMUser User { get; set; }


        internal ServerSideClient(TcpClient tcp) : base(tcp)
        {
        }


        internal void AuthorizationHandler(CMMessage newmes)
        {
            CimCredentials credentials = newmes.Message as CimCredentials;

            CMUser FoundUser = null;

            if (credentials == null)
            {
                PutOutgoingMessage(new CMMessage()
                {
                    Kind = MessageKind.AuthorizationError,
                    Message = ErrorKind.UserNotPresented
                });
                return;
            }

            if (!Server.CoMessengerUsers.TryGetValue(new Server.FullUserName(credentials.Domain,credentials.UserName), out FoundUser))
            {
                PutOutgoingMessage(new CMMessage()
                {
                    Kind = MessageKind.AuthorizationError,
                    Message = ErrorKind.UserNotFound
                });
                return;
            }

            string PasswordCheckError = String.Empty;
            if (FoundUser.AuthData.CheckPassword(DecryptPassword(credentials.Password) as string, out PasswordCheckError))
            {
                Server.AcceptAuthorization(this, FoundUser);
            }
            else
            {
                PutOutgoingMessage(new CMMessage()
                {
                    Kind = MessageKind.AuthorizationError,
                    Message = ErrorKind.WrongPassword
                });
            }

            /*
            //Без авторизации - вход под текущим пользователем
            if (credentials.IsLoggedIn)
            {
                //FoundUser = Server.CoMessengerUsers.First()
                //FoundUser = Server.CoMessengerUsers.Find((UserInList) => { return (UserInList.UserId == DecryptSomething(credentials.Password) as string); });
                if (Server.CoMessengerUsers.TryGetValue(credentials.UserName, out FoundUser))
                {
                    Server.AcceptAuthorization(this, FoundUser);
                }
                else
                {
                    PutOutMessage(new CMMessage()
                    {
                        Kind = MessageKind.AuthorizationError,
                        Message = ErrorKind.UserNotFound
                    });
                }
            }
            //Доменная авторизация
            else if (!(String.IsNullOrEmpty(credentials.Domain)))
            {
                //расшифруем жулебный пароль
                //string Password = DecryptPassword(credentials.Password);

                FoundUser = Server.CoMessengerUsers.Find((UserInList) => { return UserInList.UserName.ToLower() == credentials.UserName.ToLower(); });

                if (FoundUser != null)
                {
                    try
                    {
                        PrincipalContext prCont = new PrincipalContext(ContextType.Domain, credentials.Domain);

                        if (prCont.ValidateCredentials(credentials.UserName, DecryptPassword(credentials.Password)))
                        {
                            Server.AcceptAuthorization(this, FoundUser);
                        }
                        else
                        {
                            PutOutMessage(new CMMessage()
                            {
                                Kind = MessageKind.AuthorizationError,
                                Message = ErrorKind.WrongPassword
                            });
                        }
                    }
                    catch (PrincipalServerDownException)
                    {
                        PutOutMessage(new CMMessage()
                        {
                            Kind = MessageKind.AuthorizationError,
                            Message = ErrorKind.DomainCouldNotBeContacted
                        });
                    }

                }
                else
                {
                    PutOutMessage(new CMMessage()
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
                //credentials.Password = DecryptPassword(credentials.EncryptedPassword);

                FoundUser = Server.CoMessengerUsers.Find((UserInList) => { return UserInList.UserName.ToLower() == credentials.UserName.ToLower(); });


                //Нет такой буквы в этом слове
                if (FoundUser == null)
                {
                    PutOutMessage(new CMMessage()
                    {
                        Kind = MessageKind.AuthorizationError,
                        Message = ErrorKind.UserNotFound
                    });
                }
                else
                {
                    //Сектор приз на барабане!
                    if (FoundUser.CheckPassword(DecryptPassword(credentials.Password)))
                    //if (
                    //        FoundUser.Password == MD5Helper.CreateMD5(DecryptPassword(credentials.Password))   //Верный пароль
                    //    || (FoundUser.Password == String.Empty && DecryptPassword(credentials.Password) == String.Empty) //Пароль не задан
                    //    )
                        {
                            Server.AcceptAuthorization(this, FoundUser);
                    }
                    else
                    {
                        PutOutMessage(new CMMessage()
                        {
                            Kind = MessageKind.AuthorizationError,
                            Message = ErrorKind.WrongPassword
                        });
                    }
                }
            }*/
        }

    }
}
