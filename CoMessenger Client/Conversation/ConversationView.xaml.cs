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
using CorporateMessengerLibrary.History;
using CorporateMessengerLibrary.Messaging;
using COMessengerClient.CustomControls.CustomConverters;
using COMessengerClient.Tools;
using CorporateMessengerLibrary.Tools;

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
                SendMessage(Peer);
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
            ConnectionManager.Client.PutOutgoingMessage(
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
            ConnectionManager.Client.PutOutgoingMessage(
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
                            LoadFewMessageFromHistory(5);
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

            ConnectionManager.Client.PutOutgoingMessage(new CMMessage()
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

                            LoadMessages(
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
            LoadFewMessageFromHistory(3);
        }


        #endregion Обработчики


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

        private void AddNewMessage(RoutedMessage message)
        {
            //Возможно мы уже загрузили это сообщение ранее, ищем по ID
            MessageForeground existing_message;
            //Нашли
            if (IndexById.TryGetValue(message.MessageId, out existing_message))
            {
                //Отправитель совпадает - значит это редактирование сообщения
                if (existing_message.Message.Sender == message.Sender
                    && existing_message.Message.Values.Last().Value.Version < message.Values.Last().Value.Version)
                {
                    existing_message.Update(message);

                    existing_message.DisplayVersion(message.Values.Last().Value.Version);
                }
            }
            else
            {
                //Если не 0 версия, значит прислали исправление к сообщению, которое еще не загружено.
                if (message.Values.First().Value.Version != 0)
                    return;


                MessageBackground messageBackground = null;
                MessageForeground newblock = null;


                ClientPeer peer = App.FoundPeer(message.Sender);


                newblock = new MessageForeground(message);

                newblock.PrepareMessage();

                newblock.DisplayVersion(message.Values.Last().Value.Version);

                newblock.EditClick += (a, b) =>
                {
                    LoadMessageForEditing(newblock);
                };



                messageBackground = new MessageBackground();


                messageBackground.DataContext = newblock;
                BindingOperations.SetBinding(messageBackground.Avatar.Background, ImageBrush.ImageSourceProperty, new Binding("Peer.Avatar") { Source = peer, Converter = new NullImageConverter() });

                messageBackground.Avatar.HorizontalAlignment = peer.Peer.PeerId == App.ThisApp.CurrentPeer.Peer.PeerId ? HorizontalAlignment.Right : HorizontalAlignment.Left;


                InsertMessageIntoView(messageBackground, newblock);


                MessagesList.Add(newblock);
                IndexById.Add(message.MessageId, newblock);
                MessagesList.Sort(MessageForeground.ComparerByTime);
            }
        }

        private void InsertMessageIntoView(MessageBackground messageBackground, MessageForeground newblock)
        {

            if (MessagesList.Count == 0) //Добавляем первое сообщение
            {
                MessageArea.BackgroundStackPanel.Children.Add(messageBackground);
                MessageArea.ChatBox.Document.Blocks.Add(newblock);

                FirstMessageTime = newblock.MessageTime;
                LastMessageTime = newblock.MessageTime;
            }
            else //Уже есть сообщения
            {
                //Сообщение позднее чем самое позднее (99% случаев), добавляем в конец
                if (newblock.MessageTime > LastMessageTime)
                {
                    //Trace.WriteLine(DateTime.Now.ToString("HH:mm:ss.ffff") + " Вставляем");
                    MessageArea.BackgroundStackPanel.Children.Add(messageBackground);
                    MessageArea.ChatBox.Document.Blocks.Add(newblock);

                    LastMessageTime = newblock.MessageTime;
                }
                //Сообщение более раннее чем самое раннее (если загружаем историю), добавляем в начало
                else if (newblock.MessageTime < FirstMessageTime)
                {
                    MessageArea.ChatBox.Document.Blocks.InsertBefore(MessageArea.ChatBox.Document.Blocks.FirstBlock, newblock);
                    MessageArea.BackgroundStackPanel.Children.Insert(0, messageBackground);

                    FirstMessageTime = newblock.MessageTime;
                }
                //С какого то перепуга сообщение пришло прямо в середину переписки, возможно собеседник отправил его в момент отсутствия сети
                else
                {   //Ищем место куда втулить это сообщение
                    int? idx = MessagesList.FindEqualOrAboveIndex(newblock, MessageForeground.ComparerByTime);

                    if (idx == null)
                    {
                        MessageArea.BackgroundStackPanel.Children.Add(messageBackground);
                        MessageArea.ChatBox.Document.Blocks.Add(newblock);

                        LastMessageTime = newblock.MessageTime;
                    }
                    else
                    {
                        MessageArea.ChatBox.Document.Blocks.InsertBefore(MessagesList[(int)idx], newblock);
                        MessageArea.BackgroundStackPanel.Children.Insert((int)idx, messageBackground);
                    }
                }
            }
        }



        private void LoadMessageForEditing(MessageForeground MessageToEdit)
        {
            //Редактировали это же самое сообщение - отменяем режим редактирования
            if (NewMessageBox.CurrentEditingMessage == MessageToEdit)
            {
                NewMessageBox.IsEditingMode = false;
                MessageToEdit.IsEditing = false;
                NewMessageBox.CurrentEditingMessage = null;

                NewMessageBox.NewMessageTextBox.Document.Blocks.Clear();
            }
            else
            {
                NewMessageBox.IsEditingMode = true;
                MessageToEdit.IsEditing = true;
                NewMessageBox.NewMessageTextBox.Document.Blocks.Clear();

                MessageValue currentValue = MessageToEdit.Message.Values[MessageToEdit.DisplayedVersion];

                switch (currentValue.Kind)
                {
                    case RoutedMessageKind.RichText:
                        NewMessageBox.NewMessageTextBox.Document.Blocks.AddRange(MessagingService.ExtractBlocks(currentValue).ToList());
                        NewMessageBox.IsRichText = true;

                        break;
                    case RoutedMessageKind.Plaintext:
                        NewMessageBox.NewMessageTextBox.AppendText(currentValue.Text);

                        break;
                }

                //Если уже редактировали другое сообщение - то отменяем
                if (NewMessageBox.CurrentEditingMessage != null)
                    NewMessageBox.CurrentEditingMessage.IsEditing = false;

                NewMessageBox.CurrentEditingMessage = MessageToEdit;
            }
        }


        internal void LoadMessages(List<RoutedMessage> entriesToLoad)
        {
            entriesToLoad.ForEach(historyEntry =>
            App.ThisApp.Dispatcher.Invoke(new Action(() =>
            {

                AddNewMessage(historyEntry);


            }), System.Windows.Threading.DispatcherPriority.Loaded));
        }

        internal void LoadFewMessageFromHistory(int MessagesToLoad)
        {
            MessageArea.IsBusy = true;

            string lastLoadedMessage = MessagesList.Count > 0 ? MessagesList.First().Message.MessageId : String.Empty;

            List<RoutedMessage> ExistingMessages;
            if (Peer.Peer.PeerType == PeerType.Person)
                ExistingMessages = App.ThisApp.History.GetPrivateMessages(App.ThisApp.CurrentPeer.Peer.PeerId, Peer.Peer.PeerId, lastLoadedMessage, MessagesToLoad);
            else
                ExistingMessages = App.ThisApp.History.GetRoomMessages(Peer.Peer.PeerId, lastLoadedMessage, MessagesToLoad);

            LoadMessages(
            entriesToLoad: ExistingMessages
                        );

            MessageArea.IsBusy = false;

            //Если получили из истории меньше чем просили - то отправим запрос на сервер

            int notLoaded = MessagesToLoad - ExistingMessages.Count;

            if (notLoaded > 0 && Peer.Peer.PeerType == PeerType.Room)
            {

                MessageArea.IsBusy = true;

                HistoryQuery query = new HistoryQuery();

                string lastStoredMessage = String.Empty;
                if (ExistingMessages.Count > 0)
                    lastStoredMessage = ExistingMessages.Last().MessageId;
                else
                    lastStoredMessage = lastLoadedMessage;

                query.PeerId = Peer.Peer.PeerId;
                query.From = lastStoredMessage; //С последнего сообщения на сервере
                query.To = String.Empty; //До нашего последнего сообщения
                //query.QueryID = Guid.NewGuid().ToString();

                ConnectionManager.Client.PutOutgoingMessage(new CMMessage()
                {
                    Kind = MessageKind.Query,
                    Message = new QueryMessage()
                    {
                        Kind = QueryMessageKind.History,
                        Message = query,
                        MessageId = Guid.NewGuid().ToString(),

                        //При получении ответа загрузим то , что недогрузили
                        SuccessAction = (a) =>
                        {
                            HistoryQuery history = a.Message as HistoryQuery;

                            if (history.Content.Count > 0)
                            {
                                App.ThisApp.History.SaveMessages(history.Content);

                                //int skip = history.Content.Count - notLoaded;

                                //if (skip > 0)
                                //{
                                LoadMessages(
                                             entriesToLoad: history.Content.Take(notLoaded).ToList()
                                            );
                                //}
                                //else
                                //{
                                //    LoadMessages(
                                //                 conView: conView,
                                //                 entriesToLoad: history.Content
                                //                );
                                //}
                            }

                            MessageArea.IsBusy = false;

                        },
                        TimeoutAction = () =>
                        {
                            MessageBox.Show("Истекло время ожидания ответа от сервера");
                            MessageArea.IsBusy = false;
                        }
                    }
                });


            }

        }

        private void SendMessage(ClientPeer Receiver)
        {

            FlowDocument source = NewMessageBox.NewMessageTextBox.Document;

            TextRange tr = new TextRange(source.ContentStart, source.ContentEnd);

            if (!string.IsNullOrEmpty(tr.Text))
            {

                RoutedMessage newMessage = new RoutedMessage();
                MessageValue newValue = new MessageValue();

                if (NewMessageBox.IsEditingMode)
                {
                    RoutedMessage oldMessage = NewMessageBox.CurrentEditingMessage.Message;

                    newMessage.Receiver = oldMessage.Receiver;
                    newMessage.Sender = oldMessage.Sender;
                    newMessage.SendTime = oldMessage.SendTime;
                    newMessage.MessageId = oldMessage.MessageId;

                    newValue.Version = oldMessage.Values.Last().Value.Version + 1;
                    newValue.ChangeTime = DateTime.UtcNow;

                    NewMessageBox.CurrentEditingMessage.IsEditing = false;
                    NewMessageBox.IsEditingMode = false;

                }
                else
                {
                    newMessage.Receiver = Receiver.Peer.PeerId;
                    newMessage.Sender = App.ThisApp.CurrentUserId;
                    newMessage.SendTime = DateTime.UtcNow;
                    newMessage.MessageId = Guid.NewGuid().ToString("N");

                    newValue.Version = 0;
                    newValue.ChangeTime = newMessage.SendTime;
                }

                if (NewMessageBox.IsRichText)
                {
                    newValue.Kind = RoutedMessageKind.RichText;
                    newValue.Text = tr.Text;
                    //newValue.FormattedText = GetMessageBody(source); 

                    string FormattedText;
                    List<BinarySource> binaries;

                    MessagingService.Decompose(source, out FormattedText, out binaries);

                    //todo: избавиться от GetMessageBody
                    newValue.FormattedText = MessagingService.GetMessageBody(source);

                    NewMessageBox.IsRichText = false;



                    foreach (BinarySource binary in binaries)
                    {
                        //Если это какое-то новое изображение, отправляем серверу
                        if (!App.ThisApp.History.BinaryExists(binary.BinarySourceId))
                        {
                            App.ThisApp.History.SaveBinary(binary.BinarySourceData);

                            ConnectionManager.Client.PutOutgoingMessage(new CMMessage() { Kind = MessageKind.BinaryContent, Message = Compressing.Compress(binary.BinarySourceData) });
                        }
                    }

                }
                else
                {
                    newValue.Kind = RoutedMessageKind.Plaintext;
                    newValue.Text = tr.Text;
                }

                newMessage.Values.Add(newValue.Version, newValue);

                //Сообщения комнат сохраняем когда получим ответ от сервера
                if (Receiver.Peer.PeerType == PeerType.Person)
                {
                    newMessage.PreviousMessageId = App.ThisApp.History.GetLastMessageBetween(newMessage.Sender, newMessage.Receiver, newMessage.SendTime);
                    App.ThisApp.History.Save(newMessage);
                    newMessage.PreviousMessageId = null;
                }

                ConnectionManager.Client.PutOutgoingMessage(new CMMessage() { Kind = MessageKind.RoutedMessage, Message = newMessage });

                AddNewMessage(newMessage);

                source.Blocks.Clear();
                NewMessageBox.CurrentEditingMessage = null;

                MessageArea.ActualScrollViewer.ScrollToEnd();


            }

        }

        internal void ProcessNewMessage(RoutedMessage newMessage)
        {

            MessageArea.ChatBox.Dispatcher.Invoke(new Action(() =>
            {

                //Если переписка с этим пиром уже открывалась и область сообщений пролистана в самый низ, 
                // то запомним этот факт, чтобы после добавления нового сообщения пролистаем в низ.
                bool HaveToScrollToEnd;

                if (MessageArea.ActualScrollViewer != null)
                    HaveToScrollToEnd = MessageArea.ActualScrollViewer.ScrollableHeight == 0 || MessageArea.ActualScrollViewer.VerticalOffset / MessageArea.ActualScrollViewer.ScrollableHeight > 0.95;
                else
                    HaveToScrollToEnd = false;

                //Tuple<BlockCollection, string> extracted = null;

                //extracted = ExtractBlocks(newMessage);

                //Отправитель - если личное сообщение, то сам пир, если комната - то ищем отправителя по id
                //ClientPeer Sender = conView.Peer.Peer.Type == PeerType.Person ? conView.Peer : App.FoundPeer(newMessage.Sender);

                //AddNewMessage(ExtractBlocks(newMessage), conView, Sender, newMessage.SendTime, newMessage.MessageID, newMessage.Text);
                AddNewMessage(newMessage);

                //Trace.WriteLine("isActive = " + conView.ParentWindow.IsActive, "hasUnreadMessage");
                //Trace.WriteLine("conView visibility = " + conView.Visibility, "hasUnreadMessage");

                if (!ParentWindow.IsActive || Visibility != Visibility.Visible)
                {
                    Peer.ViewModel.HasUnreadMessages = true;

                    //Пиликнуть
                    App.ThisApp.Sound.Play("NewMessage");

                    //И поморгать
                    var helper = new FlashWindowHelper(Application.Current);
                    helper.FlashApplicationWindow();
                }

                if (HaveToScrollToEnd)
                    MessageArea.ActualScrollViewer.ScrollToEnd();
            }));

            //App.ThisApp.Client.ViewModel.ConnectionStatus = App.ThisApp.Client.ViewModel.ConnectionStatus + " Обработка окончена через:" + App.sw.ElapsedMilliseconds;
            //App.sw.Reset();
        }
    }

}



