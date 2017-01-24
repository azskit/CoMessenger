using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Reflection;
//using Xceed.Wpf.Toolkit;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.Windows.Controls.Primitives;
using COMessengerClient.CustomControls;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace COMessengerClient.ChatFace
{
    /// <summary>
    /// Interaction logic for NewMessageEditorView.xaml
    /// </summary>
    public partial class NewMessageEditorView : UserControl
    {

        private bool isXamlPasted;



        public bool IsEditingMode
        {
            get { return (bool)GetValue(IsEditingModeProperty); }
            set { SetValue(IsEditingModeProperty, value); }
        }

        private bool isRichText = false;
        public bool IsRichText { get { return isRichText; } set { isRichText = value; } }

        private MessageForeground currentEditingMessage = null;
        public MessageForeground CurrentEditingMessage
        { get { return currentEditingMessage; } set { currentEditingMessage = value; } }

        // Using a DependencyProperty as the backing store for IsEditingMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsEditingModeProperty =
            DependencyProperty.Register("IsEditingMode", typeof(bool), typeof(NewMessageEditorView), new UIPropertyMetadata(false));

        System.Drawing.Image img;


        public NewMessageEditorView()
        {
            InitializeComponent();

            img = (System.Drawing.Bitmap)System.Drawing.Image.FromFile(@"C:\secret.gif");

            //using (System.IO.FileStream fs = new System.IO.FileStream(@"C:\secret.gif", System.IO.FileMode.Open))
            //{
            //    img = (System.Drawing.Bitmap)System.Drawing.Image.FromStream(fs);
            //}


            //При вставке проверяем - xaml ли это
            DataObject.AddPastingHandler(NewMessageTextBox, new DataObjectPastingEventHandler(
                (sender, args) => 
                {
                    if (args.DataObject.GetDataPresent(DataFormats.Xaml))
                        isXamlPasted = true;

                    IsRichText = true;
                }));

            //Если событие происходит после вставки xaml, то 
            NewMessageTextBox.TextChanged += (sent, args) =>
            {

                if (isXamlPasted)
                {
                    isXamlPasted = false;

                    //Отсекаем отступы
                    NewMessageTextBox.Document.Blocks.ForEach(
                    isRecursive: true, 
                    action:   (blc) =>
                    {
                        blc.Padding = new Thickness(0);
                    });

                    //Выпиливаем параграфы пустышки
                    NewMessageTextBox.Document.Blocks.RemoveAll(
                    isRecursive: true,
                    condition: (blc) =>
                    {
                        Paragraph paragraph = blc as Paragraph;
                        return (
                               paragraph != null
                            && paragraph.Inlines.Count == 1
                            && paragraph.Inlines.FirstInline is Run
                            && ((Run)paragraph.Inlines.FirstInline).Text == " "
                                );
                    });
                }
            };


        }


        /// <summary>
        /// Скрыть показать панель инструментов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void EditToolbarSwitch_Click(object sender, RoutedEventArgs e)
        {
            EditToolbar.Visibility = EditToolbar.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void _cbFontFamily_SelectionChanged(Object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;

            if (cb.IsEnabled)
                SetFontFamily(cb.SelectedValue as FontFamily);
        }

        private void _cbFontSize_SelectionChanged(Object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;

            if (cb.IsEnabled)
                SetFontSize(cb.SelectedValue);
        }

        private void _cbFontSize_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;

            if (cb.IsEnabled && e.Key == Key.Return)
            {
                e.Handled = SetFontSize(cb.Text);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private void _cbBackgroundColors_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {
            SetColor(e.NewValue, EditingCommandsExtended.SelectBackgroundColor);
            NewMessageTextBox.Focus();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private void _cbForegroundColors_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {
            SetColor(e.NewValue, EditingCommandsExtended.SelectForegroundColor);
            NewMessageTextBox.Focus();
        }

        private Boolean SetFontFamily(FontFamily fontFamily)
        {
            System.Windows.Controls.RichTextBox editor = NewMessageTextBox;

            if (editor != null && fontFamily != null)
            {
                EditingCommandsExtended.SelectFontFamily.Execute(
                    fontFamily, editor);

                editor.Focus();
                //this.IsOverflowOpen = false;

                return true;
            }

            return false;
        }

        private Boolean SetFontSize(Object fontSize)
        {
            System.Windows.Controls.RichTextBox editor = NewMessageTextBox;

            Double? newSize = null;

            if (fontSize is Double)
            {
                newSize = (Double)fontSize;
            }
            else 
            {
                String s_fontSize = fontSize as String;

                if (s_fontSize != null)
                {

                    Double tmp;
                    if (!Double.TryParse(s_fontSize, out tmp))
                        newSize = Double.NaN;
                    else
                        newSize = tmp;
                }
            }

            if (editor != null && newSize != null)
            {
                EditingCommandsExtended.SelectFontSize.Execute(
                    (Double)newSize, editor);

                editor.Focus();
                //this.IsOverflowOpen = false;

                return true;
            }

            return false;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private Boolean SetColor(Object color, RoutedCommand cmd)
        {
            System.Windows.Controls.RichTextBox editor = NewMessageTextBox;
            Color newColor;

            PropertyInfo pinfo = color as PropertyInfo;

            if (pinfo != null)
            {
                newColor = (Color)pinfo.GetValue(color, null);
            }
            else if (color is Color)
            {
                newColor = (Color)color;
            }
            else
            {
                throw new ArgumentException("Parameter should has type \"Color\" or \"PropertyInfo\"", "color");
            }

            if (cmd != null && editor != null && newColor != null)
            {
                cmd.Execute((Color)newColor, editor);

                editor.Focus();

                return true;
            }

            return false;
        }

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        //private void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    SolidColorBrush brush = ((Button)sender).Background as SolidColorBrush;

        //    SetColor(brush.Color, EditingCommandsExtended.SelectBackgroundColor);
        //}


        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode"), Obsolete]
        //private void btnInsertPic_Click(object sender, RoutedEventArgs e)
        //{
        //    var dlg = new OpenFileDialog { InitialDirectory = @"c:\" };// fusk damn 'My Documents'
        //    if (!(bool)dlg.ShowDialog()) return;

        //    var old = Clipboard.ContainsText(TextDataFormat.Rtf) ? Clipboard.GetText(TextDataFormat.Rtf) : null;
        //    Clipboard.SetImage(new BitmapImage(new Uri(dlg.FileName, UriKind.RelativeOrAbsolute)));
        //    NewMessageTextBox.Paste();
        //    if (old != null) Clipboard.SetText(old, TextDataFormat.Rtf);
        //}

        private void AddHyperlink(object sender, RoutedEventArgs e)
        {
            string url_text = String.Empty;
            string url_label = String.Empty;

            //Popup a = UrlPanelPopup;

            UrlPanel url_panel = UrlPanelPopup.Child as UrlPanel;

            //Сперва проверим, возможно курсор уже стоит на ссылке, тогда откроем в режиме редактирования
            Hyperlink hlink = ((FrameworkContentElement)NewMessageTextBox.Selection.End.Parent).FindParent<Hyperlink>();
            if (hlink != null)
            {
                url_text = hlink.NavigateUri.AbsoluteUri;
                Run url_run = hlink.Inlines.FirstInline as Run;
                if (url_run != null)
                    url_label = url_run.Text;

                //По кнопке нажатию ОК меняем путь ссылки
                url_panel.OK_Button.Click += 
                (urlpanel, clickevent) => {
                    url_label = url_panel.Label_text.Text;
                    url_text = url_panel.URL_text.Text;

                    //Если ссылка привязана к тексту, то меняем и текст
                    if (url_run != null)
                        url_run.Text = url_label;

                    hlink.NavigateUri = new Uri(url_text);
                    hlink.ToolTip = url_text;

                    UrlPanelPopup.IsOpen = false;
                };
            }
            else //Иначе смотрим какой текст выделен
            {
                url_label = NewMessageTextBox.Selection.Text;

                //И по нажатию ОК вставляем новую ссылку с выделенным текстом
                url_panel.OK_Button.Click += 
                (urlpanel, clickevent) => {
                    url_label = url_panel.Label_text.Text;
                    url_text = url_panel.URL_text.Text;

                    if (NewMessageTextBox.Selection.IsEmpty)
                        NewMessageTextBox.Selection.Text = url_label;

                    new Hyperlink(NewMessageTextBox.Selection.Start, NewMessageTextBox.Selection.End) { NavigateUri = new Uri(url_text), ToolTip=url_text };

                    UrlPanelPopup.IsOpen = false;
                };

            }

            //При загрузке панельки редактирования ссылки заполняем поля тем что нашли
            url_panel.Loaded += 
                (urlpanel, loadedvent) =>
            {
                url_panel.Label_text.Text = url_label;
                url_panel.URL_text.Text = url_text;

            };
            
            //a.Child = url_panel;
            //a.StaysOpen = false;
            //a.Placement = PlacementMode.Bottom;
            //EditToolbar.Children.Add(a);

            UrlPanelPopup.IsOpen = true;

        }

        private void EditToolbar_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show ("clicked");
            IsRichText = true;
        }

        private void EditToolbar_Loaded(object sender, RoutedEventArgs e)
        {
            //((FrameworkElement)cmdAlignLeft.Content).SnapToPixels();

            //При нестандартном размере шрифта Windows применяем обратное машстабирование
            if (App.DpiXScalingFactor > 1 && App.DpiYScalingFactor > 1)
            {
                ScaleTransform scalingfix = new ScaleTransform(1 / App.DpiXScalingFactor, 1 / App.DpiYScalingFactor);

                cmdAlignCenter.LayoutTransform = scalingfix;
                cmdAlignLeft.LayoutTransform = scalingfix;
                cmdAlignRight.LayoutTransform = scalingfix;
            }
        }

        //private Image SelectImage()
        //{
        //        string filePath = "C:\\secret.gif";
        //        BitmapImage bitmap = new BitmapImage(new Uri(filePath, UriKind.Absolute));
        //        Image image = new Image();
        //        image.Source = bitmap;
        //        image.Width = bitmap.Width;
        //        image.Height = bitmap.Height;
        //        return image;
        //}

        private void NewMessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (NewMessageTextBox.CaretPosition.Paragraph == null)
                    return;

                TextRange tr = new TextRange(NewMessageTextBox.CaretPosition.Paragraph.ContentStart, NewMessageTextBox.CaretPosition);

                if (!string.IsNullOrEmpty(tr.Text))
                {
                    if (tr.Text.EndsWith(":)", StringComparison.Ordinal))
                    {
                        TextPointer start = tr.End.GetPositionAtOffset(-(":)".Length), LogicalDirection.Backward);

                        //Run curRun = start.Parent as Run;

                        start.DeleteTextInRun(":)".Length);


                        AnimatedImage amg = new AnimatedImage()
                        {
                            AnimatedBitmap = img as System.Drawing.Bitmap,
                            Stretch = Stretch.None,
                            Name = "smile",
                            Tag = img
                        };

                        //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

                        //sw.Start();

                        new InlineUIContainer(amg, start);

                        //sw.Stop();

                        //MessageBox.Show("Добавление смайлика заняло " + sw.ElapsedMilliseconds.ToString() + " миллисекунд");

                        //curRun.

                        //TextRange smile = new TextRange(start, tr.End);

                        //NewMessageTextBox.Selection.Select(start, tr.End);

                        //NewMessageTextBox.Selection.Text = String.Empty;
                        //Image image = SelectImage();



                        //if (image != null)
                        //{
                        //TextPointer tp = NewMessageTextBox.CaretPosition.GetInsertionPosition(LogicalDirection.Forward);
                        //    Floater floater = new Floater(new BlockUIContainer(image), tp);
                        //}

                        //NewMessageTextBox.Selection.Start.InsertLineBreak();
                        //tp.Paragraph.Inlines.Add(new MediaElement()
                        //{
                        //    UnloadedBehavior = MediaState.Manual,
                        //    Source = new Uri("file://C:\\secret.gif"),
                        //    LoadedBehavior = MediaState.Play,
                        //    Stretch = Stretch.None
                        //});

                        NewMessageTextBox.CaretPosition = NewMessageTextBox.CaretPosition.GetPositionAtOffset(3) ?? NewMessageTextBox.CaretPosition;

                        IsRichText = true;

                        //((Paragraph)NewMessageTextBox.Document.Blocks.LasteBlock).Blocks.Add(new Run("Hello bitch"));
                        //NewMessageTextBox.Document.Blocks.Add(new BlockUIContainer(new TextBlock(new Run("biiiiiiiiiiiiitch!"))));
                        //NewMessageTextBox.Selection.

                        //MessageBox.Show(smile.Text);

                        //NewMessageTextBox.CaretPosition = new TextPointer();
                        //NewMessageTextBox.Selection.Select();
                    }
                }
            }

        }
    }

    public static class FindParentAttachedMethod
    {
        public static T FindParent<T>(this FrameworkContentElement startElement) where T : DependencyObject
        {
            if (startElement == null)
                throw new ArgumentNullException("startElement");

            FrameworkContentElement parent = startElement.Parent as FrameworkContentElement;
            if (parent == null)
                return null;

            T typedParent = parent as T;
            if (typedParent != null)
            {
                return typedParent;
            }

            return FindParent<T>(parent);
        }
    }

}
