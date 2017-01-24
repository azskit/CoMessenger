using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using CorporateMessengerLibrary;
using CorporateMessengerLibrary.CimProtocol;
using CorporateMessengerLibrary.Messaging;
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

            if (!Server.Instance.CoMessengerUsers.TryGetValue(new Server.FullUserName(credentials.Domain,credentials.UserName), out FoundUser))
            {
                PutOutgoingMessage(new CMMessage()
                {
                    Kind = MessageKind.AuthorizationError,
                    Message = ErrorKind.UserNotFound
                });
                return;
            }

            //Уже авторизован на своей машине, тогда вместо пароля должен быть Id
            if (credentials.IsCurrentUser)
            {
                if (FoundUser.UserId == DecryptSomething(credentials.Password) as string)
                    Server.Instance.AcceptAuthorization(this, FoundUser);
                else
                    PutOutgoingMessage(new CMMessage()
                    {
                        Kind = MessageKind.AuthorizationError,
                        Message = ErrorKind.UserNotFound
                    });
                return;
            }




            string PasswordCheckError = String.Empty;
            if (FoundUser.Authentication.CheckPassword(DecryptPassword(credentials.Password) as string, out PasswordCheckError))
            {
                Server.Instance.AcceptAuthorization(this, FoundUser);
            }
            else
            {
                PutOutgoingMessage(new CMMessage()
                {
                    Kind = MessageKind.AuthorizationError,
                    Message = ErrorKind.WrongPassword
                });
            }

        }

    }
}
