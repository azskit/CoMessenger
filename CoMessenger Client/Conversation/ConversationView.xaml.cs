using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;
//using Microsoft.VisualStudio.Profiler;
using System.Windows.Markup;
using System.ComponentModel;
using System.Collections.ObjectModel;
using COMessengerClient.CustomControls;
using CorporateMessengerLibrary;
using System.Threading;
using System.Threading.Tasks;
using System.Xaml;
using System.Globalization;
using COMessengerClient.Conversation;

namespace COMessengerClient.Conversation
{

    /// <summary>
    /// Interaction logic for PrivateConversationWindow.xaml
    /// </summary>
    public partial class ConversationView : UserControl
    {
        public Window ParentWindow { get; set; }

        public ClientPeer Peer { get; set; }

        private List<MessageForeground> messagesList;
        internal List<MessageForeground> MessagesList { get { return messagesList; } }

        //private List<MessageForeground> messagesListByID;
        //internal List<MessageForeground> MessagesListByID { get { return messagesListByID; } }
        public SortedList<string, MessageForeground> IndexById { get; private set; }

        public DateTime LastMessageTime { get; set; }
        public DateTime FirstMessageTime { get; set; }

        private bool FirstTimeLoaded = true;


        #region Конструктор

        public ConversationView(Window parentWindow)
        {
            ParentWindow = parentWindow;

            messagesList = new List<MessageForeground>();
            //messagesListByID = new List<MessageForeground>();
            IndexById = new SortedList<string, MessageForeground>();

            InitializeComponent();

            AddHandler(Hyperlink.RequestNavigateEvent, new RoutedEventHandler(OnNavigationRequest));
        }


        public ConversationView(Window parentWindow, ClientPeer peer)
            : this(parentWindow)
        {
            Peer = peer;

            DataContext = Peer;

            if (Peer.Peer.PeerType != PeerType.Room)
            {
                ParticipantsLink.IsEnabled = false;
                ParticipantsLink.Visibility = System.Windows.Visibility.Collapsed;
            }
        }
        #endregion

        #region Обработчики событий

        //Отправить сообщение
        
        private void NewMessageBox_KeyDown(object sender, KeyEventArgs e)
        {
            //Нажали Ctrl+Enter
            if (((e.KeyboardDevice.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) && (e.Key == Key.Enter))
            {
                ConversationModel.SendMessage(this, Peer);
            }
        }
        
        //Клик по ссылке
        private void OnNavigationRequest(object sender, RoutedEventArgs e)
        {
            var source = e.OriginalSource as Hyperlink;
            if (source != null)
                Process.Start(source.NavigateUri.ToString());
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            ParticipantsList.IsOpen = true;
        }

        private void LeaveRoomClick(object sender, RoutedEventArgs e)
        {
            //Уведомим сервер
            ConnectionManager.Client.PutOutMessage(
                new CMMessage()
                {
                    Kind = MessageKind.LeaveRoom,
                    Message = this.Peer.Peer.PeerId
                });

            //И закроемся
            StartScreen.StartScreenView startscreen = App.ThisApp.MainWindow as StartScreen.StartScreenView;
            startscreen.MainGrid.Children.Remove(this);
            Visibility = System.Windows.Visibility.Hidden;
            Peer.View = null;
        }

        private void CloseRoomClick(object sender, RoutedEventArgs e)
        {
            //Уведомим сервер
            ConnectionManager.Client.PutOutMessage(
                new CMMessage()
                {
                    Kind = MessageKind.CloseRoom,
                    Message = this.Peer.Peer.PeerId
                });

            //И закроемся
            StartScreen.StartScreenView startscreen = App.ThisApp.MainWindow as StartScreen.StartScreenView;
            startscreen.MainGrid.Children.Remove(this);
            Visibility = System.Windows.Visibility.Hidden;
            Peer.View = null;
        }

        private void ElementLoaded(object sender, RoutedEventArgs e)
        {
            MessageArea.Loaded += (s, args) =>
                {
                    if (FirstTimeLoaded)
                    {
                        //Если комната, попросим у сервера немножко истории
                        if (this.Peer.Peer.PeerType == PeerType.Room)
                        {


                            HistoryQuery query = new HistoryQuery();

                            query.PeerId = this.Peer.Peer.PeerId;
                            query.From = String.Empty; //С последнего сообщения на сервере
                            query.To = String.Empty; //До нашего последнего сообщения
                                                     //query.QueryID = Guid.NewGuid().ToString();
                            AskHistory(query);
                        }
                        else
                        {
                            ConversationModel.LoadFewMessageFromHistory(this, 5);
                        }


                        if (this.MessageArea.ActualScrollViewer != null) this.MessageArea.ActualScrollViewer.ScrollToEnd();
                        FirstTimeLoaded = false;


                    }
                };

            ParentWindow.Activated += (c, d) =>
            {
                if (Peer.ViewModel.HasUnreadMessages && this.IsVisible)
                    Peer.ViewModel.HasUnreadMessages = false;
            };


            //Сфокусироваться на поле ввода
            NewMessageBox.NewMessageTextBox.Focus();
        }

        private void AskHistory(HistoryQuery query)
        {
            MessageArea.IsBusy = true;

            Stopwatch sw = new Stopwatch();

            sw.Start();

            ConnectionManager.Client.PutOutMessage(new CMMessage()
            {
                Kind = MessageKind.Query,
                Message = new QueryMessage()
                {
                    Kind = QueryMessageKind.History,
                    Message = query,
                    MessageId = Guid.NewGuid().ToString(),

                    //При получении ответа загрузим 5 сообщений
                    SuccessAction = (a) =>
                    {

                        MessageArea.IsBusy = false;

                        sw.Stop();

                        ConnectionManager.Client.ViewModel.ConnectionStatus = "Waiting response took " + sw.ElapsedMilliseconds;

                        sw.Reset();

                        HistoryQuery history = a.Message as HistoryQuery;

                        if (history.Content.Count > 0)
                        {
                            App.ThisApp.History.SaveMessages(history.Content);

                            ConversationModel.LoadMessages(
                                             conView: this,
                                             entriesToLoad: history.Content.Take(5).ToList() //Отсортировано по убыванию
                                            );
                        }

                    },
                    TimeoutAction = () =>
                    {
                        MessageArea.IsBusy = false;

                        sw.Stop();

                        //MessageBox.Show("Истекло время ожидания ответа от сервера"); 
                        ConnectionManager.Client.ViewModel.ConnectionStatus = "Timeout expired after " + sw.ElapsedMilliseconds;

                        sw.Reset();

                    }
                }
            });
        }

        private void ElementUnloaded(object sender, RoutedEventArgs e)
        {


        }

        //Ctrl+F Показать окно поиска
        private void ChatBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (((e.KeyboardDevice.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) && (e.Key == Key.F))
            {
                if (App.ThisApp.SearchWindow == null)
                {
                    App.ThisApp.SearchWindow = new SearchListWindow(this);

                    App.ThisApp.SearchWindow.Closed += (window, args) => { App.ThisApp.SearchWindow = null; };

                    App.ThisApp.SearchWindow.Show();
                }
                else
                {
                    App.ThisApp.SearchWindow.Activate();
                }


                //if (SearchPanel.Visibility == Visibility.Visible)
                //{
                //    SearchPanel.Visibility = Visibility.Collapsed;

                //    //откатываем все выделения, сделанные поиском
                //    UndoSearchHighlighting.ForEach(tuple => tuple.Item1.ApplyPropertyValue(TextElement.BackgroundProperty, tuple.Item2));
                //    UndoSearchHighlighting.Clear();

                //    NewMessageBox.NewMessageTextBox.Focus();
                //}
                //else
                //{
                //    SearchPanel.Visibility = Visibility.Visible;
                //    SearchTextBox.Focus();
                //}

                e.Handled = true;
            }
        }


        //Клик по ссылке "Загрузить предыдущие сообщения" TODO - выпилить ссылку, сделать загрузку сообщений через контекстное меню
        private void MessageArea_PreviousMessagesClicked(object sender, EventArgs e)
        {
            //Peer.History.HistoryFiles.ForEach((file) => ConversationModel.LoadDailyHistory(this, file.Date));

            NewMessageBox.NewMessageTextBox.Focus();

        }

        //При прокрутке сообщений вверх когда самый верх достигнут
        private void MessageArea_ScrollViewerStartPositionReached(object sender, EventArgs e)
        {
            ConversationModel.LoadFewMessageFromHistory(this, 3);
        }


        #endregion Обработчики


        # region Поиск

        //Встроенный поиск выпилен и заменен окошком поиска
            /*
        private void SearchBackward(object sender, RoutedEventArgs e)
        {
            //LookFor(SearchTextBox.Text, LookingDirection.Backward);
        }

        private void SearchForward(object sender, RoutedEventArgs e)
        {
            //LookFor(SearchTextBox.Text, LookingDirection.Forward);
        }

        private void FindAllClicked(object sender, RoutedEventArgs e)
        {
            //FindAll(SearchTextBox.Text);
        }

        //Список выделений во время поиска
        private List<Tuple<TextRange, Brush>> UndoSearchHighlighting = new List<Tuple<TextRange, Brush>>();

        //private List<MessageForeground> ResultSet;

        private void FindAll(string Text)
        {

            //ResultsWindow.SearchList.MessageDoubleClicked += (s, args) =>
            //    {
            //        LookAt(args.SearchResult.ContentStart);
            //    };


            //ResultsWindow.Show();
        }

        private void LookFor(string Text, LookingDirection direction)
        {

            if (String.IsNullOrWhiteSpace(Text))
                return;




            Block msg = MessageArea.ChatBox.Selection.Start.Parent as Block ?? MessageArea.ChatBox.Selection.Start.Paragraph as Block;

            int idx;

            //Курсор позиционирован на каком-либо сообщении
            if (msg != null)
            {
                while (!(msg is MessageForeground))
                {
                    msg = msg.Parent as Block;
                }

                idx = MessagesList.FindIndex(item => item == msg);
            }
            //Курсор не спозиционирован - начинаем с края документа
            else
            {
                idx = direction == LookingDirection.Backward ? MessagesList.Count : 0;
            }

            IEnumerable<MessageForeground> FirstScope = null;
            IEnumerable<MessageForeground> SecondScope = null;
            switch (direction)
            {
                case LookingDirection.Backward:

                    FirstScope = MessagesList.Take(idx).Reverse();
                    SecondScope = MessagesList.Skip(idx + 1).Reverse();

                    break;
                case LookingDirection.Forward:

                    FirstScope = MessagesList.Skip(idx + 1);
                    SecondScope = MessagesList.Take(idx);

                    break;
            }
            if (ScanScope(Text, FirstScope)) return;
            if (ScanScope(Text, SecondScope)) return;

            App.ThisApp.Client.ViewModel.ConnectionStatus = "Ничего не найдено";
        }

        private bool ScanScope(string Text, IEnumerable<MessageForeground> FirstScope)
        {

            foreach (MessageForeground item in FirstScope)
            {
                if (item.Text.Contains(Text))
                {
                    //откатываем все выделения, сделанные предыдущим поиском
                    UndoSearchHighlighting.ForEach(tuple => tuple.Item1.ApplyPropertyValue(TextElement.BackgroundProperty, tuple.Item2));
                    UndoSearchHighlighting.Clear();

                    //Пролистываем до найденного сообщения (BringIntoView как то не канает, хз почему)
                    //MessageArea.ActualScrollViewer.ScrollToVerticalOffset(item.ContentStart.GetCharacterRect(LogicalDirection.Forward).Top);
                    //MessageArea.ActualScrollViewer.ScrollToVerticalOffset(MessageArea.ActualScrollViewer.VerticalOffset - MessageArea.ActualScrollViewer.ViewportHeight + item.Top);
                    //MessageArea.ChatBox.Selection.Select(item.ContentStart, item.ContentStart);

                    //TODO переписать это говно
                    TextPointer text = item.ContentStart;
                    TextPointer sta  = null;
                    TextPointer end = null;
                    while (true)
                    {
                        TextPointer next = text.GetNextContextPosition(LogicalDirection.Forward);

                        if (next == null || next.CompareTo(item.ContentEnd) >= 0)
                        {
                            break;
                        }
                        TextRange txt = new TextRange(text, next);

                        int indx = txt.Text.IndexOf(Text);
                        if (indx >= 0)
                        {
                            sta = text.GetPositionAtOffset(indx);
                            end = text.GetPositionAtOffset(indx + Text.Length);
                            TextRange textR = new TextRange(sta, end);

                            //Запоминаем старое выделение, чтобы потом вернуть как было
                            UndoSearchHighlighting.Add(new Tuple<TextRange, Brush>(textR, textR.GetPropertyValue(TextElement.BackgroundProperty) as Brush));

                            textR.ApplyPropertyValue(TextElement.BackgroundProperty, new SolidColorBrush(Colors.Yellow));
                        }
                        text = next;
                    }

                    LookAt(sta);

                    MessageArea.ChatBox.Selection.Select(item.ContentStart, item.ContentStart);



                    return true;
                }
            }
            return false;
        }

       */
        #endregion Поиск



        private void LookAt(TextPointer sta)
        {

            //MessageArea.ActualScrollViewer.ScrollToVerticalOffset(MessageArea.ActualScrollViewer.VerticalOffset + sta.GetCharacterRect(LogicalDirection.Forward).Top);
            MessageArea.ActualScrollViewer.ScrollToVerticalOffset(sta.GetCharacterRect(LogicalDirection.Forward).Top);
        }

        public void LookAt(TextElement message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            LookAt(message.ContentStart);
        }
        
    }

}



