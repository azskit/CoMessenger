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

namespace COMessengerClient.ChatFace
{
    /// <summary>
    /// Логика взаимодействия для URLPanel.xaml
    /// </summary>
    public partial class UrlPanel : UserControl
    {
        public UrlPanel()
        {
            InitializeComponent();
            
        }

        public Button OkButton { get { return this.OK_Button; } }
    }
}
