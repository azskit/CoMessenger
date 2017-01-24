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
using COMessengerClient.ChatFace;
using COMessengerClient.Tools;
using CorporateMessengerLibrary;
using CorporateMessengerLibrary.Messaging;
using CorporateMessengerLibrary.Tools;

namespace COMessengerClient.CustomControls
{
    //public class ImageDowloadingBannerViewModel : NotifyPropertyChanged
    //{
    //    private string loadingText;

    //    public string MyProperty
    //    {
    //        get { return loadingText; }
    //        set { loadingText = value; }
    //    }

    //}

    /// <summary>
    /// Логика взаимодействия для ImageDownloadingBanner.xaml
    /// </summary>
    public partial class ImageDownloadingBanner : UserControl
    {
        private Image replacedImage;
        private InlineUIContainer inlineContainer;
        private BlockUIContainer blockContainer;

        public bool IsDownloading
        {
            get { return (bool)GetValue(IsDownloadingProperty); }
            set { SetValue(IsDownloadingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsDownloading.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsDownloadingProperty =
            DependencyProperty.Register("IsDownloading", typeof(bool), typeof(ImageDownloadingBanner), new PropertyMetadata(false));

        public ImageDownloadingBanner()
        {
            InitializeComponent();

            Loaded += Root_Loaded;
        }

        internal ImageDownloadingBanner(Image image): this()
        {
            replacedImage = image;

            inlineContainer = image.Parent as InlineUIContainer;
            blockContainer = image.Parent as BlockUIContainer;

            Width = image.Width;
            Height = image.Height;
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Reload();
        }

        public void Reload()
        {
            this.SnapToPixels();

            //InlineUIContainer container = replacedImage.Parent as InlineUIContainer;

            BinarySource imageSource = BinaryCacheManager.GetCustomValue(replacedImage);

            IsDownloading = true;

            ConnectionManager.Client.PutOutgoingMessage(new CMMessage()
            {
                Kind = MessageKind.Query,
                Message = new QueryMessage()
                {
                    Kind = QueryMessageKind.Binary,
                    Message = imageSource.BinarySourceId,
                    MessageId = Guid.NewGuid().ToString(),
                    SuccessAction =
                    (answer) =>
                    {
                        byte[] binary = answer.Message as byte[];

                        if (binary != null)
                        {

                            binary = Compressing.Decompress(binary);

                            App.ThisApp.History.SaveBinary(binary);

                            imageSource.BinarySourceData = binary;


                            Application.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                if (replacedImage is AnimatedImage)
                                    ((AnimatedImage)replacedImage).AnimatedBitmap = imageSource.ToBitmap();
                                else
                                    replacedImage.Source = imageSource.ToImageSource();

                                if (inlineContainer != null)
                                    inlineContainer.Child = replacedImage;

                                if (blockContainer != null)
                                    blockContainer.Child = replacedImage;
                            }));
                        }
                        else
                        {
                            Application.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                IsDownloading = false;
                            }));
                        }
                    },
                    TimeoutAction =
                    () =>
                    {
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            IsDownloading = false;
                        }));
                    }

                }
            });
        }

        private void Root_Loaded(object sender, RoutedEventArgs e)
        {
            this.SnapToPixels();
        }
    }
}
