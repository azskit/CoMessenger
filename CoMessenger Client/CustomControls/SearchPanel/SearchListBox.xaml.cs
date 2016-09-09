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
using COMessengerClient.Conversation;
using CorporateMessengerLibrary;

namespace COMessengerClient.CustomControls.SearchPanel
{
    /// <summary>
    /// Логика взаимодействия для SearchListBox.xaml
    /// </summary>
    public partial class SearchListBox : UserControl
    {

        private SearchPanelViewModel viewModel;

        public event EventHandler<MessageDoubleClickedEventArgs> MessageDoubleClicked;

        public SearchListBox()
        {
            InitializeComponent();
        }

        public void Init(ConversationView conView)
        {
            viewModel = new SearchPanelViewModel(conView);

            DataContext = viewModel;

            MessageDoubleClicked += (s, args) => {if (args.SearchResult.Message != null) conView.LookAt(args.SearchResult.Message);};

        }

        private void DoubleClicked(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem item = sender as ListBoxItem;

            OnMessageDoubleClicked(item.Content as SearchResultItem);

        }

        protected void OnMessageDoubleClicked(SearchResultItem message)
        {
            if (MessageDoubleClicked != null)
                MessageDoubleClicked(this, new MessageDoubleClickedEventArgs(message));
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;

                string substring = SearchTextBox.Text.ToLower(Properties.Settings.Default.UserCultureUIInfo);
                
                viewModel.Status = "Поиск...";

                //List<MessageForeground> ResultSet = viewModel.Converstaion.MessagesList.FindAll(searchResult => searchResult.Text.Contains(Text));

                viewModel.Conversation.MessagesList.Reverse<MessageForeground>().ToList().ForEach(message =>
                    {
                        foreach (MessageValue value in message.Message.Values.Values)
                        {
                            if (value.Text != null && value.Text.ToLower(Properties.Settings.Default.UserCultureUIInfo).Contains(substring))
                            {
                                viewModel.FoundMessages.Add(new SearchResultItem()
                                {
                                    Text = value.Text,
                                    Header = message.SenderName,
                                    UncMessageTime = message.MessageTime,
                                    Message = message
                                });
                            }
                        }
                    });

                //Ищем в истории
                //HistoryFile hFile = viewModel.Converstaion.Peer.History.HistoryFiles.First(file => !file.Loaded);

                /*Пока не ищем - переход на индексированную историю

                viewModel.TotalMaximum = viewModel.Converstaion.Peer.History.HistoryFiles.Count;
                viewModel.TotalValue = 0;

                foreach (HistoryFile hFile in viewModel.Converstaion.Peer.History.HistoryFiles)
                {
                    viewModel.CurrentValue = 0;
                    viewModel.TotalValue++;
                    if (hFile.Loaded) continue;

                    if (hFile.Entries == null)
                        viewModel.Converstaion.Peer.History.ReadHistoryFromDisc(hFile);

                    viewModel.CurrentMaximum = hFile.Entries.Count;

                    viewModel.Status = "Поиск в " + hFile.Date.ToShortDateString();
                    
                    hFile.Entries.SkipWhile(entry => entry.Loaded).ToList().ForEach(historyEntry =>
                    {
                        viewModel.CurrentValue++;

                        //Tuple<BlockCollection, string> extracted = ConversationModel.ExtractBlocks(historyEntry.routedMessage);

                        App.ThisApp.Dispatcher.Invoke(new Action(() =>
                        {
                            if (historyEntry.routedMessage.Text.ToLower(Properties.Settings.Default.UserCultureUIInfo).Contains(substring))
                            {
                                viewModel.FoundMessages.Add(new SearchResultItem() { Text = historyEntry.routedMessage.Text, Header = App.FoundPeer(historyEntry.routedMessage.Sender).Peer.DisplayName, UncMessageTime = historyEntry.routedMessage.SendTime });
                            }
                        }), System.Windows.Threading.DispatcherPriority.Background);
                    });
                }
                */
                viewModel.Status = "Поиск завершен";
            }
        }
    }

    public class MessageDoubleClickedEventArgs : EventArgs
    {
        public SearchResultItem SearchResult { get; private set; }
        public MessageDoubleClickedEventArgs(SearchResultItem searchResult)
        {
            SearchResult = searchResult;
        }
    }

}
