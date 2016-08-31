using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Linq;

[assembly: CLSCompliant(true)]
namespace CorporateMessengerLibrary
{
    public enum MessageKind
    {
        Authorization,
        Disconnect,
        Ping,
        AuthorizationError,
        AuthorizationNewPassword,
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
        CloseRoom
    }

    public enum ErrorKind
    {
        UserNotPresented,
        UserNotFound,
        WrongPassword,
        DomainCouldNotBeContacted
    }


    public static class Serializator
    {
        public static BinaryFormatter fmt = new BinaryFormatter();
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
        public string            MessageID { get; set; }               //Уникальный ID сообщения
        //public string            Text      { get; set; }               //Текст сообщения
        //public byte              Version   { get; set; }               //Версия сообщения (увеличивается при редактировании)

        public string            PrevMsgID { get; set; }

        public SortedList<int, MessageValue> Values { get; private set; }

        public RoutedMessage()
        {
            Values = new SortedList<int, MessageValue>();
        }

    }

    [Serializable]
    public struct MessageValue
    {
        public RoutedMessageKind Kind       { get; set; }               //Вид сообщения
        public string            Text       { get; set; }               //Текст сообщения
        public byte[]            Body       { get; set; }               //Тело сообщения
        public DateTime          ChangeTime { get; set; }               //Дата изменения
        public int               Version    { get; set; }               //Версия сообщения (увеличивается при редактировании)
    }

    [Serializable]
    public class QueryMessage
    {
        public string           MessageID { get; set; }
        public QueryMessageKind Kind      { get; set; }
        public object           Message   { get; set; }

        [NonSerialized]
        public Action<QueryMessage> SuccessAction;
        [NonSerialized]
        public Action TimeoutAction;
        [NonSerialized]
        public System.Threading.Timer Timer;
    }

    public enum RoutedMessageKind
    {
        RichText,
        PlainText
    }

    public enum QueryMessageKind
    {
        History
    }


}
