using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using CorporateMessengerLibrary;
using System.Windows.Controls;
using System.Globalization;
using System.IO;
using COMessengerClient.Conversation;
using COMessengerClient.ChatFace;
using System.Collections.ObjectModel;
using System.Windows.Media.Animation;

namespace COMessengerClient.CustomControls
{



    public enum MessageDirection
    {
        Income,
        Outcome
    }

    internal class MessageForegroundComparerByTime : IComparer<MessageForeground>
    {
        public int Compare(MessageForeground x, MessageForeground y)
        {
            if (x == null) throw new ArgumentNullException("x");
            if (y == null) throw new ArgumentNullException("y");

            int res = x.MessageTime.CompareTo(y.MessageTime);

            if (res == 0 && x.Direction != y.Direction)
                res = x.Direction == MessageDirection.Income ? 1 : -1;

            return res;
        }
    }

    //internal class MessageForegroundComparerByID : IComparer<MessageForeground>
    //{
    //    public int Compare(MessageForeground x, MessageForeground y)
    //    {
    //        if (x == null) throw new ArgumentNullException("x");
    //        if (y == null) throw new ArgumentNullException("y");
    //        return String.Compare(x.Message.MessageID, y.Message.MessageID, StringComparison.Ordinal);
    //        //return x.ID.CompareTo(y.ID);
    //    }
    //}

    public partial class MessageForeground : Section, INotifyPropertyChanged
    {

        internal static readonly MessageForegroundComparerByTime ComparerByTime = new MessageForegroundComparerByTime();
        //internal static readonly MessageForegroundComparerByID ComparerById = new MessageForegroundComparerByID();

        private UIElement ParagraphHeaderBegin = new UIElement();
        private UIElement ParagraphHeaderEnd = new UIElement();
        private UIElement ParagraphBegin = new UIElement();
        private UIElement ParagraphEnd = new UIElement();
        private double Old_Height = 0;
        private double Old_HeaderHeight = 0;

        public double Top 
        {
            get
            {
                return ParagraphHeaderBegin.PointToScreen(new Point(0d,0d)).Y;
            }
        }

        public MessageDirection Direction { get; private set; }

        public DateTime MessageTime { get; set; }

        public RoutedMessage Message { get; set; }

        public int DisplayedVersion { get; set; }

        private ObservableCollection<VersionViewModel> versions;

        //public string ID { get; set; }
        //public string Text { get; set; }

        #region DependencyProps
        public Color BackgroundColor1
        {
            get { return (Color)GetValue(BackgroundColor1Property); }
            set { SetValue(BackgroundColor1Property, value); }
        }

        // Using a DependencyProperty as the backing store for BackgroundColor1.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BackgroundColor1Property =
            DependencyProperty.Register("BackgroundColor1", typeof(Color), typeof(MessageForeground), new UIPropertyMetadata(Colors.Transparent));

        public Color BackgroundColor2
        {
            get { return (Color)GetValue(BackgroundColor2Property); }
            set { SetValue(BackgroundColor2Property, value); }
        }

        // Using a DependencyProperty as the backing store for BackgroundColor2.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BackgroundColor2Property =
            DependencyProperty.Register("BackgroundColor2", typeof(Color), typeof(MessageForeground), new UIPropertyMetadata(Colors.Transparent));

        public string SenderName
        {
            get { return (string)GetValue(SenderNameProperty); }
            set { SetValue(SenderNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Header.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SenderNameProperty =
            DependencyProperty.Register("SenderName", typeof(string), typeof(MessageForeground), new UIPropertyMetadata(String.Empty));


        public Thickness TextPadding
        {
            get { return (Thickness)GetValue(TextPaddingProperty); }
            set { SetValue(TextPaddingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TextPadding.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextPaddingProperty =
            DependencyProperty.Register("TextPadding", typeof(Thickness), typeof(MessageForeground), new UIPropertyMetadata(new Thickness(0)));

        public bool IsEditing
        {
            get { return (bool)GetValue(IsEditingProperty); }
            set { SetValue(IsEditingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsEditing.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsEditingProperty =
            DependencyProperty.Register("IsEditing", typeof(bool), typeof(MessageForeground), new UIPropertyMetadata(false));




        #endregion


        public MessageForeground(RoutedMessage message)
        {
            if (message == null)
                throw new ArgumentNullException("message");

            versions = new ObservableCollection<VersionViewModel>();

            ParagraphBegin.LayoutUpdated +=
                (sender, e) =>
                {
                    RecalculateHeight();
                };

            Message = message;

            message.Values.ToList().ForEach(pair =>
            {
                versions.Add(new VersionViewModel() 
                {
                    Version = pair.Value.Version,
                    ChangeTime = pair.Value.ChangeTime.ToLocalTime()
                });
            });

            TextPadding = new Thickness(3);



            //this.Background = new SolidColorBrush(new Color() { ScA = 0.1f, ScR = 1f, ScB = 0f, ScG = 0f });
        }

        public void RecalculateHeight()
        {

            height = CalcHeight(ParagraphBegin, ParagraphEnd);
            if (Old_Height != Height)
            {
                RaisePropertyChanged("Height");
                RaisePropertyChanged("HeaderAndBackgroundHeight");
                Old_Height = Height;
            }
            headerHeight = CalcHeight(ParagraphHeaderBegin, ParagraphHeaderEnd);
            if (Old_HeaderHeight != HeaderHeight)
            {
                RaisePropertyChanged("HeaderHeight");
                RaisePropertyChanged("HeaderAndBackgroundHeight");
                Old_Height = Height;
            }
        }

        private static double CalcHeight(UIElement one, UIElement two)
        {



            if (PresentationSource.FromVisual(one) != null && PresentationSource.FromVisual(two) != null)
            {
                return (two.PointToScreen(new Point(0d, 0d)).Y - one.PointToScreen(new Point(0d, 0d)).Y) / App.DpiYScalingFactor;
            }
            else
            {
                return 0d;
            }
        }


        public void PrepareMessage()
        {
            //if (conView == null)
            //    throw new ArgumentNullException("conView");

            ClientPeer peer = App.FoundPeer(Message.Sender);

            Direction = peer.Peer.PeerId == App.ThisApp.CurrentPeer.Peer.PeerId ? MessageDirection.Outcome : MessageDirection.Income;

            switch (Direction)
            {
                case MessageDirection.Income:

                    this.SetBinding(Block.PaddingProperty, new Binding("IncomePadding") { Source = Properties.Settings.Default });

                    this.SetBinding(MessageForeground.BackgroundColor1Property, new Binding("IncomingMessageBackgroundColor1") { Source = Properties.Settings.Default });
                    this.SetBinding(MessageForeground.BackgroundColor2Property, new Binding("IncomingMessageBackgroundColor2") { Source = Properties.Settings.Default });

                    break;
                case MessageDirection.Outcome:

                    this.SetBinding(Block.PaddingProperty, new Binding("OutcomePadding") { Source = Properties.Settings.Default });

                    this.SetBinding(MessageForeground.BackgroundColor1Property, new Binding("OutcomingMessageBackgroundColor1") { Source = Properties.Settings.Default });
                    this.SetBinding(MessageForeground.BackgroundColor2Property, new Binding("OutcomingMessageBackgroundColor2") { Source = Properties.Settings.Default });

                    break;
                default:
                    throw new InvalidOperationException();
            }

            MessageTime = Message.SendTime;

            this.SetBinding(MessageForeground.SenderNameProperty, new Binding("Peer.DisplayName") { Source = peer });

            this.Blocks.Add(new BlockUIContainer(ParagraphHeaderBegin));

            //Заголовок сообщения
            Paragraph headerParagraph = new Paragraph();

            //Отправитель и время
            headerParagraph.Inlines.Add(new Run(String.Format(CultureInfo.InvariantCulture, App.ThisApp.Locally.LocaleStrings["{0} {1}"], MessageTime.ToLocalTime().ToString(Properties.Settings.Default.UserCultureUIInfo), SenderName)));

            EditPanel = new EditMessagePanel();
            EditPanel.DataContext = new MessageHeaderViewModel(versions);
            EditPanel.VersionSelected += (a, b) => { DisplayVersion(b.Version.Version); };

            //Кнопка "Редактировать"
            if (Direction == MessageDirection.Outcome)
            {
                EditPanel.EditButton.Visibility = Visibility.Visible;
                EditPanel.EditButton.Click += (a, b) => { OnEditClick(); };

            }

            //if (conView.Peer.Peer.Type == PeerType.Person || Direction == MessageDirection.Outcome)
            if (Direction == MessageDirection.Outcome)
            {
                EditPanel.Opacity = 0;

                headerParagraph.Foreground = new SolidColorBrush();

                BindingOperations.SetBinding(headerParagraph.Foreground, SolidColorBrush.ColorProperty, new Binding("ChatBoxFont.FontColor") { Source = Properties.Settings.Default, Mode = BindingMode.OneWay });

                headerParagraph.Foreground.Opacity = 0;

                this.MouseEnter += (a, b) =>
                {
                    EditPanel.BeginStoryboard(App.ThisApp.Resources["Appear"] as Storyboard);
                    headerParagraph.BeginStoryboard(App.ThisApp.Resources["AppearParagraph"] as Storyboard);
                };
                this.MouseLeave += (a, b) =>
                {
                    EditPanel.BeginStoryboard(App.ThisApp.Resources["Disappear"] as Storyboard);
                    headerParagraph.BeginStoryboard(App.ThisApp.Resources["DisappearParagraph"] as Storyboard);
                };

            }

            headerParagraph.Inlines.Add(new InlineUIContainer(EditPanel));

            this.Blocks.Add(headerParagraph);
            this.Blocks.Add(new BlockUIContainer(ParagraphHeaderEnd));

            //Содержание
            this.Blocks.Add(new BlockUIContainer(ParagraphBegin));
            Content = new Section();
            Content.SetBinding(Block.PaddingProperty, new Binding("TextPadding") { Source = this });
            this.Blocks.Add(Content);
            this.Blocks.Add(new BlockUIContainer(ParagraphEnd));

        }

        private Section Content { get; set; }

        public void DisplayVersion(int version)
        {
            MessageValue Value = Message.Values[version];

            Content.Blocks.Clear();
            Content.Blocks.AddRange(ConversationModel.ExtractBlocks(Value).ToList());

            Content.Blocks.ForEach(
                        isRecursive: true,
                        action: (blc) =>
                        {
                            BlockUIContainer imageContainer;

                            imageContainer = blc as BlockUIContainer;

                            if (imageContainer != null)
                            {
                                Image img = imageContainer.Child as Image;

                                if (img != null)
                                {
                                    img.Loaded += (a, b) =>
                                    {
                                        img.SnapToPixels();
                                    };


                                    Viewbox imageBox = new Viewbox();

                                    imageContainer.Child = imageBox;

                                    imageBox.Child = img;
                                    imageBox.HorizontalAlignment = img.HorizontalAlignment;

                                    img.UseLayoutRounding = true;
                                    img.SnapsToDevicePixels = true;

                                    imageBox.MaxWidth = img.Width * 1 / App.DpiXScalingFactor;

                                    //imageBox.MouseDown += (a, b) => { MessageBox.Show("SnapsToDevicePixels: " + tmp.SnapsToDevicePixels + " UseLayoutRounding: " + tmp.UseLayoutRounding + " X: " + tmp.PointToScreen(new Point(0d, 0d)).X + "Y: " + tmp.PointToScreen(new Point(0d, 0d)).Y); };

                                    //imageContainer.Child.RenderTransform = new ScaleTransform(1 / App.DpiXScalingFactor, 1 / App.DpiYScalingFactor);
                                    //imageContainer.Child = new Button() { Content = "hi there" };
                                }
                            }
                        });

            versions[DisplayedVersion].IsCurrent = false;

            DisplayedVersion = Value.Version;

            versions[Value.Version].IsCurrent = true;
        }

        public void Update(RoutedMessage message)
        {
            message.Values.ToList().ForEach(pair =>
            {
                if (!this.Message.Values.ContainsKey(pair.Key))
                {
                    this.Message.Values.Add(pair.Key, pair.Value);
                    versions.Add(new VersionViewModel() 
                    { 
                        Version = pair.Value.Version,
                        ChangeTime = pair.Value.ChangeTime.ToLocalTime()
                    });
                }
            });
        }

        public double HeaderAndBackgroundHeight
        {
            get
            {
                return HeaderHeight + Height;
            }
        }

        private double headerHeight;
        public double HeaderHeight
        {
            get
            {
                return headerHeight;
            }
        }

        private double height;
        public double Height
        {
            get
            {
                return height;
            }
        }



        void RaisePropertyChanged(string propName)
        {
            var e = PropertyChanged;
            if (e != null)
            {
                e(this, new PropertyChangedEventArgs(propName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event RoutedEventHandler EditClick;

        protected virtual void OnEditClick()
        {
            if (EditClick != null)
                EditClick(this, new RoutedEventArgs());
        }


        public EditMessagePanel EditPanel { get; set; }
    }
}
