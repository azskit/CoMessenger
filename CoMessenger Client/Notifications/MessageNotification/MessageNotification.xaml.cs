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
using CorporateMessengerLibrary.Messaging;

namespace COMessengerClient.Notifications.MessageNotification
{
    /// <summary>
    /// Логика взаимодействия для MessageNotification.xaml
    /// </summary>
    public partial class MessageNotification : Window
    {
        public Action<string> TypedTextHandler { get; private set; }

        public Action ClickHandler { get; private set; }

        public MessageNotification()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Показать всплывающее сообщение
        /// </summary>
        /// <param name="peer">Отправитель</param>
        /// <param name="message">Сообщение</param>
        public static void Emit(ClientPeer peer, MessageValue message, Action<string> typedTextHandler, Action clickHandler)
        {
            MessageNotification newNotification = new MessageNotification();

            newNotification.DataContext = new MessageNotificationVM() { Peer = peer, Message = message };

            newNotification.TypedTextHandler = typedTextHandler;
            newNotification.ClickHandler     = clickHandler;

            newNotification.Show();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ClickHandler?.Invoke();
            Close();
        }

        private void TextBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ClickHandler?.Invoke();
            Close();
        }

        private void quickButton_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(quickAnswer.Text) && TypedTextHandler != null)
            {
                TypedTextHandler.Invoke(quickAnswer.Text);
                Close();
            }
        }

        private void quickAnswer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter
                 && ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                 && !String.IsNullOrWhiteSpace(quickAnswer.Text)
                 && TypedTextHandler != null
                 )
            {
                TypedTextHandler.Invoke(quickAnswer.Text);
                Close();
            }
        }
    }
}
