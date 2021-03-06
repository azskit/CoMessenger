﻿using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

[assembly: CLSCompliant(true)]
namespace CorporateMessengerLibrary.Messaging
{
    public enum MessageKind
    {
        Authorization,
        Disconnect,
        Ping,
        AuthorizationError,
        //AuthorizationNewPassword,
        UsersList,
        UpdatePeer,
        Groups,
        RoutedMessage,
        CryptoKey,
        EnterRoom,
        Query,
        Answer,
        LeaveRoom,
        NewRoom,
        CloseRoom,
        BinaryContent
    }

    public enum ErrorKind
    {
        UserNotPresented,
        UserNotFound,
        WrongPassword,
        DomainCouldNotBeContacted
    }


    public static class Serializer
    {
        public static BinaryFormatter Formatter { get; private set; }

        static Serializer()
        {
            Formatter = new BinaryFormatter();
        }
    }

    [Serializable]
    public class CMMessage
    {
        public MessageKind Kind { get; set; }

        public object Message { get; set; }
    }

    [Serializable]
    public class RoutedMessage
    {
        //public RoutedMessageKind Kind      { get; set; }               //Вид сообщения
        //public object            Message   { get; set; }               //Тело сообщения
        public string            Receiver  { get; set; }               //ID пира-получателя
        public string            Sender    { get; set; }               //ID пира-отправителя
        public DateTime          SendTime  { get; set; }               //Момент отправки
        public string            MessageId { get; set; }               //Уникальный ID сообщения
        //public string            Text      { get; set; }               //Текст сообщения
        //public byte              Version   { get; set; }               //Версия сообщения (увеличивается при редактировании)

        public string            PreviousMessageId { get; set; }

        public SortedList<int, MessageValue> Values { get; private set; }

        public RoutedMessage()
        {
            Values = new SortedList<int, MessageValue>();
        }

#if DEBUG
        public RoutedMessage(bool isDesignTime) : this()
        {
            Sender = "";
            Receiver = "";
            SendTime = DateTime.Now;
            Values.Add(0,
                            new MessageValue()
                            {
                                ChangeTime = DateTime.Now,
                                Kind = RoutedMessageKind.Plaintext,
                                Text = "Hello world!",
                                Version = 0
                            });
        } 
#endif

    }

    [Serializable]
    public class MessageValue
    {
        public RoutedMessageKind          Kind              { get; set; }               //Вид сообщения
        public string                     Text              { get; set; }               //Текст сообщения
        public byte[]                     FormattedText     { get; set; }               //Форматированный и сжатый текст XAML
        //public Dictionary<string, byte[]> BinaryCollection  { get; set; }               //Двоичные данные (изображения и т.п.)
        public DateTime                   ChangeTime        { get; set; }               //Дата изменения
        public int                        Version           { get; set; }               //Версия сообщения (увеличивается при редактировании)
    }

    [Serializable]
    public class QueryMessage
    {
        public string           MessageId { get; set; }
        public QueryMessageKind Kind      { get; set; }
        public object           Message   { get; set; }

        [NonSerialized]
        private Action<QueryMessage> successAction;
        public Action<QueryMessage> SuccessAction
        { get { return successAction; } set { successAction = value; }}

        [NonSerialized]
        private Action timeoutAction;
        public Action TimeoutAction
        {get{ return timeoutAction;} set {timeoutAction = value;}}

        [NonSerialized]
        private System.Threading.Timer timer;
        public Timer Timer
        {get{ return timer;}set {timer = value;}}

    }

    public enum RoutedMessageKind
    {
        RichText,
        Plaintext
    }

    public enum QueryMessageKind
    {
        History,
        Binary
    }


}
