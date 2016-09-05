using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CorporateMessengerLibrary;
using System.Windows.Media.Animation;
using COMessengerClient.Conversation;

namespace COMessengerClient.ContactList
{
    /// <summary>
    /// Interaction logic for ContactListView.xaml
    /// </summary>
    public partial class ContactListView : UserControl
    {
        StartScreen.StartScreenView startscreen = App.ThisApp.MainWindow as StartScreen.StartScreenView;
        ContactListViewModel viewModel;

        public ContactListView()
        {
            InitializeComponent();
            viewModel = new ContactListViewModel();

            //По двойному клику открываем окно разговора
            UserList.ClientPeerDoubleClicked += (listBox, args) =>
                {
                    bool HaveToAdd = true;

                    //foreach (UIElement conversation in startscreen.ConversationsGrid.Children)

                    foreach (ClientPeer peer in App.ThisApp.ListOfConversations)
                    {
                        if (peer.View == null)
                            continue;

                        ConversationView view = peer.View;

                        //Если разговор уже в процессе, то просто отобразим его
                        if (view.Peer == args.ClickedClientPeer)
                        {
                            //Еще нигде не открыт
                            if (view.Parent == null)
                                startscreen.MainGrid.Children.Add(args.ClickedClientPeer.View);


                            view.Visibility = Visibility.Visible;

                            //if (view.Peer.ViewModel.HasUnreadMessages)
                            //    view.MessageArea.ActualScrollViewer.ScrollToEnd();

                            HaveToAdd = false;
                            view.Peer.ViewModel.HasUnreadMessages = false;

                        }
                            //А все остальные разговоры скроем
                        else
                        {
                            view.Visibility = Visibility.Hidden;
                        }
                    }

                    //Начинаем разговор
                    if (HaveToAdd)
                    {
                        if (args.ClickedClientPeer.Peer.PeerType == PeerType.Room)
                        {
                            RoomPeer room = args.ClickedClientPeer.Peer as RoomPeer;

                            //Если мы не являемся участником разговора в этой комнате, то уведомим сервер, что мы присоединились
                            if (!room.Participants.Contains(App.ThisApp.CurrentPeer.Peer as PersonPeer))
                            {
                                App.ThisApp.Client.PutOutMessage(
                                    new CMMessage()
                                    {
                                        Kind = MessageKind.EnterRoom,
                                        Message = room.PeerId
                                    });
                            }
                        }

                        args.ClickedClientPeer.InitView();
                        args.ClickedClientPeer.View.Visibility = System.Windows.Visibility.Visible;
                        //startscreen.ConversationsGrid.Children.Add(args.ClickedClientPeer.View);
                        startscreen.MainGrid.Children.Add(args.ClickedClientPeer.View);
                        args.ClickedClientPeer.View.Peer.ViewModel.HasUnreadMessages = false;
                    }

                }; 

            DataContext = viewModel;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CreateNewRoomPanel.IsOpen = true;
        }

        private void CreateNewRoom(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(viewModel.NewRoomName))
            {
                App.ThisApp.Client.PutOutMessage(
                new CMMessage()
                {
                    Kind = MessageKind.NewRoom,
                    Message = viewModel.NewRoomName
                });

                CreateNewRoomPanel.IsOpen = false;
            }

            
        }
    }
}
