using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using COMessengerClient.Tools;
using CorporateMessengerLibrary.Messaging;

namespace COMessengerClient.Notifications.MessageNotification
{
    internal class MessageNotificationVM : NotifyPropertyChanged
    {
        private ClientPeer peer;

        public ClientPeer Peer
        {
            get { return peer; }
            set
            {
                peer = value;
                OnPropertyChanged("Peer");
            }
        }

        private MessageValue message;

        public MessageValue Message
        {
            get { return message; }
            set
            {
                message = value;
                OnPropertyChanged("Message");
            }
        }

        public static MessageNotificationVM DesignTimeMessageNotification;


        static MessageNotificationVM()
        {

            //ClientPeer DesignTimeRoomPeer = new ClientPeer();

            //DesignTimeRoomPeer.UpdatePeer(new RoomPeer()
            //{
            //    DisplayName = "Design Time Room",
            //    PeerType = PeerType.Room,
            //    State = PeerStatus.Common
            //});

            ClientPeer DesignTimePersonPeer = new ClientPeer();

            DesignTimePersonPeer.UpdatePeer(new PersonPeer()
            {
                DisplayName = "Design Time Person",
                PeerType = PeerType.Person,
                State = PeerStatus.Online
            });


            DesignTimeMessageNotification = new MessageNotificationVM()
            {
                Peer = DesignTimePersonPeer,
                Message = new MessageValue() { Text =
@"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
안녕하세요~ 나는 많은 사람과 친구가되고 싶어요! 영어도 배우고 한국어도 잘 알려줄게요"
                }
            };
        }


    }
}
