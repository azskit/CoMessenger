using System;
using System.Collections.Generic;
using CorporateMessengerLibrary;
using System.Globalization;
using SimpleChat.Identity;
using CorporateMessengerLibrary.Messaging;
using CorporateMessengerLibrary.CimProtocol;

namespace SimpleChat
{
    /// <summary>
    /// Получатель сообщения
    /// </summary>
    internal interface IMessageReceiver
    {
        /// <summary>
        /// Обработать адресованное сообщение
        /// </summary>
        /// <param name="message">адресованное сообщение</param>
        void ProcessMessage(RoutedMessage message);

        /// <summary>
        /// Вернуть получателя
        /// </summary>
        /// <returns>получатель сообщения (Peer), может быть человек или комната</returns>
        Peer Peer();
    }


    /// <summary>
    /// Серверная обертка получателя - человека
    /// </summary>
    public class ServerPersonPeer : IMessageReceiver
    {
        /// <summary>
        /// Получатель - человек
        /// </summary>
        public PersonPeer Person { get; set; }

        /// <summary>
        /// Учетная запись
        /// </summary>
        internal CMUser User { get; set; }

        /// <summary>
        /// Очередь неотправленных сообщений
        /// </summary>
        //private Queue<RoutedMessage> outcomeMessages = new Queue<RoutedMessage>();
        //public Queue<RoutedMessage> OutcomeMessages
        //{
        //    get { return outcomeMessages; }
        //}

        //public IndexedHistoryManager History { get; set; }


        /// <summary>
        /// Реализация обработки сообщения
        /// </summary>
        /// <param name="message"></param>
        public void ProcessMessage(RoutedMessage message)
        {
            //Если существует подключение, то отправляем сообщение сразу
            if (User.Client != null && User.Client.State == ClientState.Connected)
            {
                User.Client.PutOutgoingMessage(new CMMessage()
                {
                    Kind = MessageKind.RoutedMessage,
                    Message = message
                });
            }
            //Иначе ставим в очередь неотправленных
            else
            {
                //if (History == null)
                //    History = new IndexedHistoryManager("Storage/History",  false);

                Server.TempHistory.Save(message);

            }
        }

        public void OnAuthorizationConfirmed()
        {
            //if (History == null)
            //    History = new IndexedHistoryManager("Storage/History", false);

            //Отправим все, что пришло за время отсутствия клиента
            foreach (RoutedMessage msg in Server.TempHistory.GetMessagesSentToReceiver(Person.PeerId, ""))
            {
                if (User.Client != null)
                {
                    msg.PreviousMessageId = null;

                    User.Client.PutOutgoingMessage(new CMMessage()
                    {
                        Kind = MessageKind.RoutedMessage,
                        Message = msg
                    });

                    Server.TempHistory.Delete(msg);
                }
            } 
        }


        /// <summary>
        /// Вернуть получателя
        /// </summary>
        /// <returns>получатель-человек</returns>
        public Peer Peer()
        {
            return Person;
        }

    }

    /// <summary>
    /// Серверная обертка получателя-комнаты
    /// </summary>
    internal class ServerRoomPeer : IMessageReceiver
    {
        /// <summary>
        /// Получатель - комната
        /// </summary>
        internal RoomPeer Room { get; set; }
        /// <summary>
        /// Участники комнаты
        /// </summary>
        internal List<ServerPersonPeer> Participants { get; private set; }

        public void ProcessMessage(RoutedMessage message)
        {
            //сохраняем сообщение
            message.PreviousMessageId = Server.RoomsHistory.GetLastMessageTo(Room.PeerId, message.SendTime);

            Server.RoomsHistory.Save(message);

            //рассылаем всем кто онлайн
            Participants.ForEach(participant =>
                {
                    if (participant.User.Client != null && participant.User.Client.State == ClientState.Connected)
                    {
                        participant.User.Client.PutOutgoingMessage(new CMMessage()
                        {
                            Kind = MessageKind.RoutedMessage,
                            Message = message
                        });
                    }
                });
        }

        /// <summary>
        /// Вернуть получателя
        /// </summary>
        /// <returns>получатель-комната</returns>
        public Peer Peer()
        {
            return Room;
        }

        internal ServerRoomPeer()
        {
            Participants = new List<ServerPersonPeer>();
        }
    }




}
