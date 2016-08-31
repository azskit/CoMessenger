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

namespace COMessengerClient.CustomControls
{
    /// <summary>
    /// Логика взаимодействия для InputBox.xaml
    /// </summary>
    public partial class InputBox : Window
    {

        public string Text 
        {
            get { return Input.Text; }
            set { Input.Text = value; }
        }

        public InputBox(string text, string caption)
        {
            InitializeComponent();

            Title = caption;
            Question.Text = text;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
