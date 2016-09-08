using System;
using System.Collections.Generic;
using CorporateMessengerLibrary;
using System.Globalization;

namespace SimpleChat
{
    /// <summary>
    /// Получатель сообщения
    /// </summary>
    public interface IMessagable
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
    public class ServerPersonPeer : IMessagable
    {
        /// <summary>
        /// Получатель - человек
        /// </summary>
        public PersonPeer Person { get; set; }

        /// <summary>
        /// Учетная запись
        /// </summary>
        public CMUser User { get; set; }

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
                User.Client.PutOutMessage(new CMMessage()
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

        public void OnAuthorizated()
        {
            //if (History == null)
            //    History = new IndexedHistoryManager("Storage/History", false);

            //Отправим все, что пришло за время отсутствия клиента
            foreach (RoutedMessage msg in Server.TempHistory.GetMessagesSentToReceiver(Person.PeerId, ""))
            {
                if (User.Client != null)
                {
                    msg.PrevMsgId = null;

                    User.Client.PutOutMessage(new CMMessage()
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
    [Serializable]
    public class ServerRoomPeer : IMessagable
    {
        /// <summary>
        /// Получатель - комната
        /// </summary>
        public RoomPeer Room { get; set; }
        /// <summary>
        /// Участники комнаты
        /// </summary>
        public List<ServerPersonPeer> Participants { get; set; }

        public void ProcessMessage(RoutedMessage message)
        {
            //сохраняем сообщение
            message.PrevMsgId = Server.RoomsHistory.GetLastMsgTo(Room.PeerId, message.SendTime);

            Server.RoomsHistory.Save(message);

            //рассылаем всем кто онлайн
            Participants.ForEach(participant =>
                {
                    if (participant.User.Client != null && participant.User.Client.State == ClientState.Connected)
                    {
                        participant.User.Client.PutOutMessage(new CMMessage()
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
        /*
        //Инициализация обертки
        //TODO переделать вызов этой муйни из конструктора, в конструктор передавать peerID
        internal void Init()
        {
            History = new IndexedHistoryManager("Storage/History", false);
            //MessagesBuffer = History.ReadMessages();
        }
        */
    }




}
