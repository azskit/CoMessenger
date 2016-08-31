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
using System.Windows.Shapes;
using COMessengerClient.Conversation;

namespace COMessengerClient.CustomControls
{
    /// <summary>
    /// Логика взаимодействия для SearchListWindow.xaml
    /// </summary>
    public partial class SearchListWindow : Window
    {
        public SearchListWindow()
        {
            InitializeComponent();
        }

        public SearchListWindow(ConversationView conView) :this()
        {
            SearchList.Init(conView);
        }
    }
}
