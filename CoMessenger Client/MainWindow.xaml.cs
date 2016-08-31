using System;
using System.Collections.Generic;
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
using System.Threading;
using System.Windows.Markup;
using System.IO;

namespace COMessengerClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private CMClientClient client;

        public MainWindow()
        {
            InitializeComponent();

            client = App.ThisApp.Client;
        }

        public void NewMessagesIterator()
        {
            while (true)
            {
                client.ProcessQueue();

                if (client.InMessagesCount > 0)
                {

                    CMMessage mes = client.GetInMessage();
                    //string responseData = string.Empty;

                    switch (mes.Kind)
                    {
                        case MessageKind.Ping: break;
                        //case MessageKind.Text:



                            //Paragraph paragraph = new Paragraph();
                            //paragraph.Inlines.Add(new Bold(new Run("Строка для добавление")));
                            //ChatBox.Document.Blocks.Add(paragraph);


                            //responseData = mes.SearchResult as string;

                            //Console.WriteLine("Принято {0} :", DateTime.Now.TimeOfDay.TotalSeconds);

                            //Console.WriteLine("{0} says: {1}", client.tcp.Client.RemoteEndPoint.ToString(), responseData);

                            //ChatBox.Dispatcher.BeginInvoke(new Action<string>(delegate(string a)
                            //{
                            //    using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes((string)mes.SearchResult)))
                            //    {
                            //        TextRange tr = new TextRange(ChatBox.Document.ContentEnd, ChatBox.Document.ContentEnd);

                            //        tr.Load(stream, DataFormats.XamlPackage);
                            //    }
                            //}), responseData);

                            //break;
                        default: break;
                    }
                }
                Thread.Sleep(1);

            }
        }



        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            client.ConnectTo("localhost", 13000);
            ChatBox.AppendText("Connected to " + client.tcp.Client.RemoteEndPoint.ToString() + "\n");

            Thread ListenerThread = new Thread(new ThreadStart(NewMessagesIterator));
            ListenerThread.Start();

            //bool notStop = true;

            //Console.WriteLine("Connected to {0}...", App.ThisApp.client.tcp.Client.RemoteEndPoint.ToString());
            //while (notStop)
            //{
            //    string msg = Console.ReadLine();

            //    if (msg == "args")
            //        notStop = false;
            //    else if (!string.IsNullOrEmpty(msg))
            //    {
            //        Console.Write("{0} : ", DateTime.Now.TimeOfDay);
            //        App.ThisApp.client.PutOutMessage(new CMMessage() { Kind = MessageKind.Text, SearchResult = msg });
            //    }
            //}
        }

        private void NewMessageBox_KeyDown(object sender, KeyEventArgs e)
        {

            //Нажали Ctrl+Enter

            if (((e.KeyboardDevice.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) && (e.Key == Key.Enter))
                {

                    TextRange tr = new TextRange(NewMessageBox.Document.ContentStart, NewMessageBox.Document.ContentEnd);

                    if (!string.IsNullOrEmpty(tr.Text))
                    {
                        //Paragraph paragraph = new Paragraph();
                        //paragraph.Inlines.Add(new Bold(new Run("Строка для добавление")));

                        //foreach (Block block in NewMessageBox.Document.Blocks)
                        //{
                        //    Block newbl = block;
                        //    ChatBox.Document.Blocks.Add(newbl);
                        //}

                        Block[] newblocks = new Block[NewMessageBox.Document.Blocks.Count];

                        NewMessageBox.Document.Blocks.CopyTo(newblocks, 0);

                        //ChatBox.Document.Blocks.AddRange(newblocks);
/*
                        using (MemoryStream stream = new MemoryStream())
                        using (StreamReader reader = new StreamReader(stream))
                        {
                        //MemoryStream stream = new MemoryStream();
                        
                            //reader.

                            TextRange range = new TextRange(NewMessageBox.Document.ContentStart,
                                NewMessageBox.Document.ContentEnd);
                            range.Save(reader.BaseStream, DataFormats.XamlPackage);
                            reader.BaseStream.Position = 0;

                            // Чтение содержимого из потока и вывод его в текстовом поле. 
                            //using (StreamReader r = new StreamReader(stream))
                            //{
                            //    string line;
                            //    while ((line = r.ReadLine()) != null)
                            //        ChatBox.AppendText(line + "\n");
                            //}



                            App.ThisApp.Client.PutOutMessage(new CMMessage() { Kind = MessageKind.Text, SearchResult = reader.ReadToEnd() });
                        }
 */
                        //App.ThisApp.client.PutOutMessage(new CMMessage() { Kind = MessageKind.Text, SearchResult = tr.Text });
                        NewMessageBox.Document.Blocks.Clear();
                    }
                }
        }


        private void cmdFormatting_Click(object sender, RoutedEventArgs e)
        {
            // Ссылка на объект выделения
            TextSelection selection = NewMessageBox.Selection;

            // Если текст не найден, код интерпретирует
            // шрифт как обычный
            FontWeight weightState = FontWeights.Normal;
            FontStyle styleState = FontStyles.Normal;
            TextDecorationCollection currentState = null;

            if (e.OriginalSource == cmdBold)
            {
                // Проверка, выведен ли фрагмент полужирным цветом
                if (selection.GetPropertyValue(Run.FontWeightProperty) !=
                    DependencyProperty.UnsetValue)
                {
                    weightState = (FontWeight)selection.GetPropertyValue(
                        Run.FontWeightProperty);
                }

                if (weightState == FontWeights.Normal)
                {
                    selection.ApplyPropertyValue(Run.FontWeightProperty, FontWeights.Bold);
                }
                else
                {
                    selection.ApplyPropertyValue(Run.FontWeightProperty, FontWeights.Normal);
                }
            }

            if (e.OriginalSource == cmdItalic)
            {
                // Проверка, выведен ли фрагмент наклонным стилем
                if (selection.GetPropertyValue(Run.FontStyleProperty) != DependencyProperty.UnsetValue)
                    styleState = (FontStyle)selection.GetPropertyValue(Run.FontStyleProperty);

                if (styleState == FontStyles.Italic)
                {
                    selection.ApplyPropertyValue(Run.FontStyleProperty, FontStyles.Normal);
                }
                else
                {
                    selection.ApplyPropertyValue(Run.FontStyleProperty, FontStyles.Italic);
                }
            }

            if (e.OriginalSource == cmdUnder)
            {
                // Проверка, выведен ли фрагмент с подчеркиванием
                if (selection.GetPropertyValue(Run.TextDecorationsProperty) != DependencyProperty.UnsetValue)
                    currentState = (TextDecorationCollection)selection.GetPropertyValue(Run.TextDecorationsProperty);

                if (currentState != TextDecorations.Underline)
                {
                    selection.ApplyPropertyValue(Run.TextDecorationsProperty, TextDecorations.Underline);
                }
                else
                {
                    selection.ApplyPropertyValue(Run.TextDecorationsProperty, null);
                }
            }

            // Возврат фокуса полю, чтобы пользователь мог продолжить работу с ним
            NewMessageBox.Focus();
        }

    }
}
