using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Globalization;
using CorporateMessengerLibrary;
using System.Windows.Documents;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using COMessengerClient.CustomControls;
using System.Collections.ObjectModel;
using COMessengerClient.CustomControls.CustomConverters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Media;
using System.Windows.Markup;
using COMessengerClient.ChatFace;
using System.Xml;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Markup.Primitives;
using COMessengerClient.Tools;

namespace COMessengerClient.Conversation
{
    public static class ListBinarySearch
    {
        internal static T FindEqualOrAbove<T>(this List<T> theList, T key, IComparer<T> comparer)
        {
            if (theList == null)
                throw new ArgumentNullException("theList");

            if (theList.Count == 0) return default(T);
            //if (theList.Count == 1) return theList[0];

            int idx = theList.BinarySearch(key, comparer);

            if (idx >= 0)
                return theList[idx];
            else
            {
                if (-(idx) > theList.Count)
                    return default(T);
                else
                    return theList[-(idx + 1)];
            }
        }

        internal static int? FindEqualOrAboveIndex<T>(this List<T> theList, T key, IComparer<T> comparer)
        {
            if (theList == null)
                throw new ArgumentNullException("theList");

            if (theList.Count == 0) return null;
            //if (theList.Count == 1) return theList[0];

            int idx = theList.BinarySearch(key, comparer);

            if (idx >= 0)
                return idx;
            else
            {
                if (-(idx) > theList.Count)
                    return null;
                else
                    return -(idx + 1);
            }
        }

    }

    static class EditorHelper
    {
        public static void Register<T, TC>()
        {
            Attribute[] attr = new Attribute[1];
            TypeConverterAttribute vConv = new TypeConverterAttribute(typeof(TC));
            attr[0] = vConv;
            var i = TypeDescriptor.AddAttributes(typeof(T), attr);

           

            
        }
        public static void RegisterVS<T, TC>()
        {
            Attribute[] attr = new Attribute[1];
            ValueSerializerAttribute vConv = new ValueSerializerAttribute(typeof(TC));
            attr[0] = vConv;
            var i = TypeDescriptor.AddAttributes(typeof(T), attr);




        }

    }

    class  CustomImageValueSerializer : ImageSourceValueSerializer
    {
        public override bool CanConvertToString(object value, IValueSerializerContext context)
        {
            
            if (value is AnimatedImage)
                return false;
            else if (value is System.Windows.Controls.Image)
                return true;
            return base.CanConvertToString(value, context);
        }

        public override string ConvertToString(object value, IValueSerializerContext context)
        {

            return base.ConvertToString(value, context);
        }
    }


    class CustomImageConvertor : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(MarkupExtension))
            {

                return true;
            }
            else return false;
        }
        public override object ConvertTo(ITypeDescriptorContext context,
                        System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(MarkupExtension))
            {
                BindingExpression bindingExpression = value as BindingExpression;
                if (bindingExpression == null)
                    throw new Exception();
                return bindingExpression.ParentBinding;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }



    public sealed class ConversationModel
    {
        //private static TaskFactory factory = new TaskFactory();

        private ConversationModel() { }

        /// <summary>
        /// Обработчик новых сообщений
        /// </summary>
        public static void NewMessageHandler(RoutedMessage newMessage, ConversationView conView)
        {
            if (newMessage == null)
                throw new ArgumentNullException("newMessage");

            if (conView == null)
                throw new ArgumentNullException("conView");

                    conView.MessageArea.ChatBox.Dispatcher.Invoke(new Action(() =>
                    {

                        //Если переписка с этим пиром уже открывалась и область сообщений пролистана в самый низ, 
                        // то запомним этот факт, чтобы после добавления нового сообщения пролистаем в низ.
                        bool HaveToScrollToEnd;
                        
                        if (conView.MessageArea.ActualScrollViewer != null)
                            HaveToScrollToEnd = conView.MessageArea.ActualScrollViewer.ScrollableHeight == 0 || conView.MessageArea.ActualScrollViewer.VerticalOffset / conView.MessageArea.ActualScrollViewer.ScrollableHeight > 0.95;
                        else
                            HaveToScrollToEnd = false;

                        //Tuple<BlockCollection, string> extracted = null;

                        //extracted = ExtractBlocks(newMessage);

                        //Отправитель - если личное сообщение, то сам пир, если комната - то ищем отправителя по id
                        //ClientPeer Sender = conView.Peer.Peer.Type == PeerType.Person ? conView.Peer : App.FoundPeer(newMessage.Sender);

                        //AddNewMessage(ExtractBlocks(newMessage), conView, Sender, newMessage.SendTime, newMessage.MessageID, newMessage.Text);
                        AddNewMessage(conView, newMessage);

                        //Trace.WriteLine("isActive = " + conView.ParentWindow.IsActive, "hasUnreadMessage");
                        //Trace.WriteLine("conView visibility = " + conView.Visibility, "hasUnreadMessage");

                        if (!conView.ParentWindow.IsActive || conView.Visibility != Visibility.Visible)
                        {
                            conView.Peer.ViewModel.HasUnreadMessages = true;

                            //Пиликнуть
                            App.ThisApp.Sound.Play("NewMessage");

                            //И поморгать
                            var helper = new FlashWindowHelper(Application.Current);
                            helper.FlashApplicationWindow(); 
                        }

                        if (HaveToScrollToEnd)
                            conView.MessageArea.ActualScrollViewer.ScrollToEnd();
                    }));

                    //App.ThisApp.Client.ViewModel.ConnectionStatus = App.ThisApp.Client.ViewModel.ConnectionStatus + " Обработка окончена через:" + App.sw.ElapsedMilliseconds;
                    //App.sw.Reset();
        }

        internal static BlockCollection ExtractBlocks(MessageValue value)
        {
            FlowDocument fldoc = new FlowDocument();

            //Костыль - если в редакторе указать такие же параметры тексту, как и по умолчанию, то он их не применит
            // поэтому указываем "нереальные" параметры по умолчанию (дерьмо, но ниче лучше не придумал :( )
            //fldoc.FontSize = 1; 
            //fldoc.Foreground = new SolidColorBrush(Colors.Transparent);

            TextRange tr = new TextRange(fldoc.ContentEnd, fldoc.ContentEnd);

            switch (value.Kind)
            {
                case RoutedMessageKind.RichText:

                    //using (MemoryStream stream = new MemoryStream(Compressing.Decompress(value.Body)))
                    //{
                    //    tr.Load(stream, DataFormats.XamlPackage);
                    //}


                    //using (MemoryStream stream = new MemoryStream(Compressing.Decompress(value.Body)))
                    //using (System.Xaml.XamlReader reader =                     {
                    //    FlowDocument deserialized = System.Windows.Markup.XamlReader.Load as FlowDocument;
                    //}



                    using (StringReader stringReader = new StringReader(Encoding.UTF8.GetString(Compressing.Decompress(value.FormattedText))))
                    using (System.Xml.XmlReader xmlReader = System.Xml.XmlReader.Create(stringReader))
                    {
                        try
                        {
                            fldoc = XamlReader.Load(xmlReader) as FlowDocument;

                        }
                        catch (XamlParseException)
                        {
                            //tr.Text = value.Text ?? "";
                            //fldoc.Blocks.FirstBlock.ToolTip = "ОШИБКА ЧТЕНИЯ ФОРМАТА СООБЩЕНИЯ"; 
                            fldoc.Blocks.Add(new Paragraph(new Run(value.Text ?? "")) { ToolTip = "ОШИБКА ЧТЕНИЯ ФОРМАТА СООБЩЕНИЯ" });
                        }
                    }                   
                    //return deserialized.Blocks;
                    break;
                case RoutedMessageKind.Plaintext:
                    tr.Text = value.Text ?? "";
                    break;
            }
            return fldoc.Blocks;
        }

        /// <summary>
        /// Вставить новое сообщение blocks в окно чата conView
        /// </summary>
        /// <param name="blocks">BlockCollection нового сообщения</param>
        /// <param name="conView">ConversationView чата</param>
        //public static void AddNewMessage2(BlockCollection blocks, ConversationView conView, ClientPeer peer, DateTime time, string MessageID, string Text)
        public static void AddNewMessage(ConversationView conView, RoutedMessage message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            if (conView == null)
                throw new ArgumentNullException("conView");

            //Возможно мы уже загрузили это сообщение ранее, ищем по ID
            //int existing_message_index = conView.IndexByID.IndexOfKey(message.MessageID);

            //MessageForeground existing_message = conView.IndexByID.In
            MessageForeground existing_message;
            //Нашли
            //if (existing_message_index >= 0 && conView.MessagesListByID[existing_message_index].Type == newblock.Type)
            //if (existing_message_index >= 0)
            if (conView.IndexById.TryGetValue(message.MessageId, out existing_message))
            {
                //MessageForeground existing_message = conView.IndexByID.Values[existing_message_index];

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
                    LoadMessageForEditing(conView, newblock);
                };


                
                messageBackground = new MessageBackground();


                messageBackground.DataContext = newblock;
                BindingOperations.SetBinding(messageBackground.Avatar.Background, ImageBrush.ImageSourceProperty, new Binding("Peer.Avatar") { Source = peer, Converter = new NullImageConverter() });

                messageBackground.Avatar.HorizontalAlignment = peer.Peer.PeerId == App.ThisApp.CurrentPeer.Peer.PeerId ? HorizontalAlignment.Right : HorizontalAlignment.Left;


                InsertMessageIntoView(conView, messageBackground, newblock);


                conView.MessagesList.Add(newblock);
                conView.IndexById.Add(message.MessageId, newblock);
                conView.MessagesList.Sort(MessageForeground.ComparerByTime);
            }
        }

        private static void LoadMessageForEditing(ConversationView conView, MessageForeground MessageToEdit)
        {
            //Редактировали это же самое сообщение - отменяем режим редактирования
            if (conView.NewMessageBox.CurrentEditingMessage == MessageToEdit)
            {
                conView.NewMessageBox.IsEditingMode = false;
                MessageToEdit.IsEditing = false;
                conView.NewMessageBox.CurrentEditingMessage = null;

                conView.NewMessageBox.NewMessageTextBox.Document.Blocks.Clear();
            }
            else 
            {
                conView.NewMessageBox.IsEditingMode = true;
                MessageToEdit.IsEditing = true;
                conView.NewMessageBox.NewMessageTextBox.Document.Blocks.Clear();

                MessageValue currentValue = MessageToEdit.Message.Values[MessageToEdit.DisplayedVersion];

                switch (currentValue.Kind)
                {
                    case RoutedMessageKind.RichText:
                        conView.NewMessageBox.NewMessageTextBox.Document.Blocks.AddRange(ExtractBlocks(currentValue).ToList());
                        conView.NewMessageBox.IsRichText = true;

                        break;
                    case RoutedMessageKind.Plaintext:
                        conView.NewMessageBox.NewMessageTextBox.AppendText(currentValue.Text);

                        break;
                }

                //Если уже редактировали другое сообщение - то отменяем
                if (conView.NewMessageBox.CurrentEditingMessage != null) 
                    conView.NewMessageBox.CurrentEditingMessage.IsEditing = false;

                conView.NewMessageBox.CurrentEditingMessage = MessageToEdit;
            }
        }


        private static void InsertMessageIntoView(ConversationView conView, MessageBackground messageBackground, MessageForeground newblock)
        {

            if (conView.MessagesList.Count == 0) //Добавляем первое сообщение
            {
                conView.MessageArea.BackgroundStackPanel.Children.Add(messageBackground);
                conView.MessageArea.ChatBox.Document.Blocks.Add(newblock);

                conView.FirstMessageTime = newblock.MessageTime;
                conView.LastMessageTime = newblock.MessageTime;
            }
            else //Уже есть сообщения
            {
                //Сообщение позднее чем самое позднее (99% случаев), добавляем в конец
                if (newblock.MessageTime > conView.LastMessageTime)
                {
                    //Trace.WriteLine(DateTime.Now.ToString("HH:mm:ss.ffff") + " Вставляем");
                    conView.MessageArea.BackgroundStackPanel.Children.Add(messageBackground);
                    conView.MessageArea.ChatBox.Document.Blocks.Add(newblock);

                    conView.LastMessageTime = newblock.MessageTime;
                }
                //Сообщение более раннее чем самое раннее (если загружаем историю), добавляем в начало
                else if (newblock.MessageTime < conView.FirstMessageTime)
                {
                    conView.MessageArea.ChatBox.Document.Blocks.InsertBefore(conView.MessageArea.ChatBox.Document.Blocks.FirstBlock, newblock);
                    conView.MessageArea.BackgroundStackPanel.Children.Insert(0, messageBackground);

                    conView.FirstMessageTime = newblock.MessageTime;
                }
                //С какого то перепуга сообщение пришло прямо в середину переписки, возможно собеседник отправил его в момент отсутствия сети
                else
                {   //Ищем место куда втулить это сообщение
                    int? idx = conView.MessagesList.FindEqualOrAboveIndex(newblock, MessageForeground.ComparerByTime);

                    if (idx == null)
                    {
                        conView.MessageArea.BackgroundStackPanel.Children.Add(messageBackground);
                        conView.MessageArea.ChatBox.Document.Blocks.Add(newblock);

                        conView.LastMessageTime = newblock.MessageTime;
                    }
                    else
                    {
                        conView.MessageArea.ChatBox.Document.Blocks.InsertBefore(conView.MessagesList[(int)idx], newblock);
                        conView.MessageArea.BackgroundStackPanel.Children.Insert((int)idx, messageBackground);
                    }
                }
            }
        }


        private static byte[] GetMessageBody(FlowDocument source)
        {
            FlowDocument fldoc = new FlowDocument();

            //Костыль - если в редакторе указать такие же параметры тексту, как и по умолчанию, то он их не применит
            // поэтому указываем "нереальные" параметры по умолчанию (дерьмо, но ниче лучше не придумал :( )
            fldoc.FontSize = 1;
            fldoc.Foreground = new SolidColorBrush(Colors.Transparent);

            Block[] newblocks = new Block[source.Blocks.Count];

            source.Blocks.CopyTo(newblocks, 0);

            fldoc.Blocks.AddRange(newblocks);

            TextRange range = new TextRange(fldoc.ContentStart, fldoc.ContentEnd);

            string savedButton = System.Windows.Markup.XamlWriter.Save(fldoc);

            //var c = ValueSerializer.GetSerializerFor(typeof(System.Windows.Controls.Image));

            //var m = MarkupWriter.GetMarkupObjectFor(fldoc);

            //StringBuilder outstr = new StringBuilder();
            //XmlWriterSettings settings = new XmlWriterSettings();
            //settings.Indent = true;
            //settings.OmitXmlDeclaration = true;
            //XamlDesignerSerializationManager dsm = new XamlDesignerSerializationManager(XmlWriter.Create(outstr, settings));
            ////this string need for turning on expression saving mode 
            //dsm.XamlWriterMode = XamlWriterMode.Value;
            //XamlWriter.Save(fldoc, dsm);

            CimSerializer serializer = new CimSerializer();

            string xml = serializer.Serialize(fldoc);

            //using (FileStream fstream = new FileStream("debug_message2.txt", FileMode.Create))
            //{
            //    fstream.Write(Encoding.UTF8.GetBytes(outstr.ToString()), 0, outstr.Length);
            //}
            using (FileStream fstream = new FileStream("debug_message2.txt", FileMode.Create))
            {
                fstream.Write(Encoding.UTF8.GetBytes(xml), 0, xml.Length);
            }





            using (FileStream fstream = new FileStream("debug_message.txt", FileMode.Create))
            {
                fstream.Write(Encoding.UTF8.GetBytes(savedButton), 0, Encoding.UTF8.GetBytes(savedButton).Length);
            }

            //fldoc.Blocks.ForEach(
            //            isRecursive: true,
            //            action: (blc) =>
            //            {
            //                BlockUIContainer imageContainer;

            //                imageContainer = blc as BlockUIContainer;

            //                if (imageContainer != null)
            //                {
            //                    Image img = imageContainer.Child as Image;

            //                    if (img != null)
            //                    {
            //                        img.Loaded += (a, b) =>
            //                        {
            //                            img.SnapToPixels();
            //                        };


            //                        Viewbox imageBox = new Viewbox();

            //                        imageContainer.Child = imageBox;

            //                        imageBox.Child = img;
            //                        imageBox.HorizontalAlignment = img.HorizontalAlignment;

            //                        img.UseLayoutRounding = true;
            //                        img.SnapsToDevicePixels = true;

            //                        imageBox.MaxWidth = img.Width * 1 / App.DpiXScalingFactor;

            //                        //imageBox.MouseDown += (a, b) => { MessageBox.Show("SnapsToDevicePixels: " + tmp.SnapsToDevicePixels + " UseLayoutRounding: " + tmp.UseLayoutRounding + " X: " + tmp.PointToScreen(new Point(0d, 0d)).X + "Y: " + tmp.PointToScreen(new Point(0d, 0d)).Y); };

            //                        //imageContainer.Child.RenderTransform = new ScaleTransform(1 / App.DpiXScalingFactor, 1 / App.DpiYScalingFactor);
            //                        //imageContainer.Child = new Button() { Content = "hi there" };
            //                    }
            //                }
            //            });

            return Compressing.Compress(Encoding.UTF8.GetBytes(savedButton));


            //using (MemoryStream streamXAML = new MemoryStream())
            //{
            //    range.Save(streamXAML, DataFormats.XamlPackage, true);
            //    streamXAML.Position = 0;

            //    BinaryReader reader = new BinaryReader(streamXAML);
            //    //using (BinaryReader reader = new BinaryReader(streamXAML))
            //    //{
            //        return Compressing.Compress(reader.ReadBytes((int)streamXAML.Length));
            //    //}
            //}

        }

        internal static void SendMessage(ConversationView conView, ClientPeer Receiver)
        {
            if (conView == null)
                throw new ArgumentNullException("conView");
   
            FlowDocument source = conView.NewMessageBox.NewMessageTextBox.Document;

            TextRange tr = new TextRange(source.ContentStart, source.ContentEnd);

            if (!string.IsNullOrEmpty(tr.Text))
            {

                RoutedMessage newMessage = new RoutedMessage();
                MessageValue newValue = new MessageValue();

                if (conView.NewMessageBox.IsEditingMode)
                {
                    RoutedMessage oldMessage = conView.NewMessageBox.CurrentEditingMessage.Message;

                    newMessage.Receiver  = oldMessage.Receiver ;
                    newMessage.Sender    = oldMessage.Sender   ;
                    newMessage.SendTime  = oldMessage.SendTime ;
                    newMessage.MessageId = oldMessage.MessageId;

                    newValue.Version = oldMessage.Values.Last().Value.Version + 1;
                    newValue.ChangeTime = DateTime.UtcNow;

                    conView.NewMessageBox.CurrentEditingMessage.IsEditing = false;
                    conView.NewMessageBox.IsEditingMode = false;

                }
                else
                {
                    newMessage.Receiver  = Receiver.Peer.PeerId;
                    newMessage.Sender    = App.ThisApp.CurrentUserId;
                    newMessage.SendTime  = DateTime.UtcNow;
                    newMessage.MessageId = Guid.NewGuid().ToString("N");

                    newValue.Version = 0;
                    newValue.ChangeTime = newMessage.SendTime;
                }

                if (conView.NewMessageBox.IsRichText)
                {
                    newValue.Kind = RoutedMessageKind.RichText;
                    newValue.Text = tr.Text;
                    newValue.FormattedText = GetMessageBody(source); 

                    conView.NewMessageBox.IsRichText = false;
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

                AddNewMessage(conView, newMessage);

                source.Blocks.Clear();
                conView.NewMessageBox.CurrentEditingMessage = null;

                conView.MessageArea.ActualScrollViewer.ScrollToEnd();


            }

        }

        internal static void LoadMessages(ConversationView conView, List<RoutedMessage> entriesToLoad)
        {
            entriesToLoad.ForEach(historyEntry =>
            App.ThisApp.Dispatcher.Invoke(new Action(() =>
            {

                LoadMessage(conView, historyEntry);


            }), System.Windows.Threading.DispatcherPriority.Loaded));
        }

        private static void LoadMessage(ConversationView conView, RoutedMessage historyEntry)
        {
            //App.ThisApp.Client.ViewModel.ConnectionStatus += (" pause " + sw.ElapsedMilliseconds);
            //sw.Restart();

            //ConversationModel.AddNewMessage
            //    (blocks: ConversationModel.ExtractBlocks(historyEntry),   //Тело сообщения
            //     conView: conView,                                         //Окно беседы
            //     peer: App.FoundPeer(historyEntry.Sender),              //Собеседник, отправлявший сообщение
            //     time: historyEntry.SendTime,                           //Время отправки
            //     MessageID: historyEntry.MessageID,                          //ID
            //     Text: historyEntry.Text);                              //Текст
            ConversationModel.AddNewMessage
                (conView: conView,  
                 message: historyEntry);

            //App.ThisApp.Client.ViewModel.ConnectionStatus += (" loading " + sw.ElapsedMilliseconds);
            //sw.Restart();
        }

        internal static void LoadFewMessageFromHistory(ConversationView conView, int MessagesToLoad)
        {
            conView.MessageArea.IsBusy = true;

            string lastLoadedMessage = conView.MessagesList.Count > 0 ? conView.MessagesList.First().Message.MessageId : String.Empty;

            List<RoutedMessage> ExistingMessages;
            if (conView.Peer.Peer.PeerType == PeerType.Person)
                ExistingMessages = App.ThisApp.History.GetPrivateMessages(App.ThisApp.CurrentPeer.Peer.PeerId, conView.Peer.Peer.PeerId, lastLoadedMessage, MessagesToLoad);
            else
                ExistingMessages = App.ThisApp.History.GetRoomMessages(conView.Peer.Peer.PeerId, lastLoadedMessage, MessagesToLoad);
                            
            LoadMessages( 
            conView:        conView,
            entriesToLoad:  ExistingMessages
                        );

            conView.MessageArea.IsBusy = false;

            //Если получили из истории меньше чем просили - то отправим запрос на сервер

            int notLoaded = MessagesToLoad - ExistingMessages.Count;

            if (notLoaded > 0 && conView.Peer.Peer.PeerType == PeerType.Room)
            {

                conView.MessageArea.IsBusy = true;

                HistoryQuery query = new HistoryQuery();

                string lastStoredMessage = String.Empty;
                if (ExistingMessages.Count > 0)
                    lastStoredMessage = ExistingMessages.Last().MessageId;
                else
                    lastStoredMessage = lastLoadedMessage;

                query.PeerId = conView.Peer.Peer.PeerId;
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
                                                 conView: conView,
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

                            conView.MessageArea.IsBusy = false;

                        },
                        TimeoutAction = () =>
                        {
                            MessageBox.Show("Истекло время ожидания ответа от сервера"); 
                            conView.MessageArea.IsBusy = false;
                        }                        
                    }
                });


            }
  
        }
    }
}
