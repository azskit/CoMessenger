using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls.Primitives;

namespace COMessengerClient.ChatFace
{
    public static class EditingCommandsExtended
    {
        #region Routed commands
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RoutedUICommand ToggleStrikethrough   = RegisterNewCommand("Strikeout",               "ToggleStrikeout",       EditingCommandsEx_ToggleStrikethrough_Executed,   EditingCommandsEx_CanExecute);

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RoutedUICommand SelectFontFamily      = RegisterNewCommand("Select font family",      "SelectFontFamily",      EditingCommandsEx_SelectFontFamily_Executed,      EditingCommandsEx_CanExecute);

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RoutedUICommand SelectFontSize        = RegisterNewCommand("Select font size",        "SelectFontSize",        EditingCommandsEx_SelectFontSize_Executed,        EditingCommandsEx_CanExecute);

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RoutedUICommand SelectBackgroundColor = RegisterNewCommand("Select background color", "SelectBackgroundColor", EditingCommandsEx_SelectBackgroundColor_Executed, EditingCommandsEx_CanExecute);

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RoutedUICommand SelectForegroundColor = RegisterNewCommand("Select foreground color", "SelectForegroundColor", EditingCommandsEx_SelectForegroundColor_Executed, EditingCommandsEx_CanExecute);

        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly RoutedUICommand MakeUrl               = RegisterNewCommand("Select foreground color", "SelectForegroundColor", EditingCommandsEx_MakeUrl_Executed,               EditingCommandsEx_MakeUrl_CanExecute);


        #endregion // Routed commands

        #region Initialization

        public static RoutedUICommand RegisterNewCommand(string commandDescription, string commandName, ExecutedRoutedEventHandler execHandler, CanExecuteRoutedEventHandler canExecHandler)
        {
            RoutedUICommand NewCommand = new RoutedUICommand(commandDescription, commandName, typeof(EditingCommandsExtended));

            CommandManager.RegisterClassCommandBinding( type:           typeof(RichTextBox),
                                                        commandBinding: new CommandBinding( command:    NewCommand, 
                                                                                            executed:   new ExecutedRoutedEventHandler(execHandler),
                                                                                            canExecute: new CanExecuteRoutedEventHandler(canExecHandler)));

            return NewCommand;
        }

        #endregion // Initialization

        #region Command handlers implementation
        
        private static void EditingCommandsEx_CanExecute(Object sender, CanExecuteRoutedEventArgs e)
        {
            var editor = sender as RichTextBox;
            e.CanExecute = editor != null;
        }

        private static void EditingCommandsEx_MakeUrl_CanExecute(Object sender, CanExecuteRoutedEventArgs e)
        {
            var editor = sender as RichTextBox;

            //Нельзя забацать ссылку если выделенный текст не располагается целиком в одном параграфе
            e.CanExecute = (editor != null && (editor.Selection.IsEmpty || (editor.Selection.Start.Paragraph == editor.Selection.End.Paragraph)));
        }

        private static void EditingCommandsEx_ToggleStrikethrough_Executed(Object sender, ExecutedRoutedEventArgs e)
        {
            var editor = sender as RichTextBox;
            if (e.Handled = editor != null)
            {
                ToggleSelectionFormattingProperty<TextDecorationCollection>(editor.Selection,
                    Inline.TextDecorationsProperty,
                    TextDecorations.Strikethrough, null,
                    (item1, item2) => TextDecorationCollection.Equals(item1, item2));
            }
        }

        private static void EditingCommandsEx_SelectFontFamily_Executed(Object sender, ExecutedRoutedEventArgs e)
        {
            var editor = sender as RichTextBox;
            if (e.Handled = (editor != null && e.Parameter is FontFamily))
            {

                ApplyNewValueToFormattingProperty<FontFamily>(
                    editor.Selection, Paragraph.FontFamilyProperty, (FontFamily)e.Parameter,
                    (item1, item2) => FontFamily.Equals(item1, item2));
            }
        }

        private static void EditingCommandsEx_SelectFontSize_Executed(Object sender, ExecutedRoutedEventArgs e)
        {
            var editor = sender as RichTextBox;
            Double? size = GetSelectionFontSize(e.Parameter);

            if (e.Handled = (editor != null && size != null))
            {
                ApplyNewValueToFormattingProperty<Double>(
                                    editor.Selection, Paragraph.FontSizeProperty, (Double)size,
                                    (item1, item2) => Double.Equals(item1, item2));
            }
        }

        private static void EditingCommandsEx_SelectForegroundColor_Executed(Object sender, ExecutedRoutedEventArgs e)
        {
            var editor = sender as RichTextBox;
            Color? color = GetSelectionColor(e.Parameter, Colors.Black);

            if (e.Handled = (editor != null && color != null))
            {
                ApplyNewValueToFormattingProperty<SolidColorBrush>(
                    editor.Selection, TextElement.ForegroundProperty, new SolidColorBrush((Color)color),
                    (item1, item2) => SolidColorBrush.Equals(item1, item2));
            }
        }

        private static void EditingCommandsEx_SelectBackgroundColor_Executed(Object sender, ExecutedRoutedEventArgs e)
        {
            var editor = sender as RichTextBox;
            Color? color = GetSelectionColor(e.Parameter, Colors.White);

            if (e.Handled = (editor != null && color != null))
            {
                ApplyNewValueToFormattingProperty<SolidColorBrush>(
                    editor.Selection, TextElement.BackgroundProperty, new SolidColorBrush((Color)color),
                    (item1, item2) => SolidColorBrush.Equals(item1, item2));
            }
        }

        private static void EditingCommandsEx_MakeUrl_Executed(Object sender, ExecutedRoutedEventArgs e)
        {
            var NewMessageTextBox = sender as RichTextBox;

            string url_text = String.Empty;
            string url_label = String.Empty;

            Popup url_popup = new Popup();

            UrlPanel url_panel = new UrlPanel();

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
                (urlpanel, clickevent) =>
                {
                    url_label = url_panel.Label_text.Text;
                    url_text = url_panel.URL_text.Text;

                    if (Uri.IsWellFormedUriString(url_text, UriKind.Absolute) && !String.IsNullOrWhiteSpace(url_label))
                    {

                        //Если ссылка привязана к тексту, то меняем и текст
                        if (url_run != null)
                            url_run.Text = url_label;

                        hlink.NavigateUri = new Uri(url_text);
                        hlink.ToolTip = url_text;

                        url_popup.IsOpen = false;
                        url_popup = null;
                    }
                };
            }
            else //Иначе смотрим какой текст выделен
            {
                url_label = NewMessageTextBox.Selection.Text;

                if (Uri.IsWellFormedUriString(url_label, UriKind.Absolute))
                    url_text = url_label;
                else
                    url_text = "http://";

                //И по нажатию ОК вставляем новую ссылку с выделенным текстом
                url_panel.OK_Button.Click +=
                (urlpanel, clickevent) =>
                {
                    url_label = url_panel.Label_text.Text;
                    url_text = url_panel.URL_text.Text;

                    if (Uri.IsWellFormedUriString(url_text, UriKind.Absolute) && !String.IsNullOrWhiteSpace(url_label))
                    {

                        if (NewMessageTextBox.Selection.IsEmpty)
                            NewMessageTextBox.Selection.Text = url_label;

                        new Hyperlink(NewMessageTextBox.Selection.Start, NewMessageTextBox.Selection.End) { NavigateUri = new Uri(url_text), ToolTip = url_text };

                        url_popup.IsOpen = false;
                        url_popup = null;
                    }
                };

            }

            //При загрузке панельки редактирования ссылки заполняем поля тем что нашли
            url_panel.Loaded +=
                (urlpanel, loadedvent) =>
                {
                    url_panel.Label_text.Text = url_label;
                    url_panel.URL_text.Text = url_text;

                };

            url_popup.Child = url_panel;
            url_popup.StaysOpen = false;
            url_popup.Placement = PlacementMode.Mouse;
            url_popup.PlacementTarget = NewMessageTextBox;

            url_popup.IsOpen = true;

        }

        #endregion // Command handlers implementation

        internal delegate bool Equaler<T>(T item1, T item2);

        internal static Object GetSelectionPropertyValue(TextSelection selection, DependencyProperty formattingProperty)
        {
            // This is necessary due to the emergence of unexpected errors 
            // when pasting text from other editors, such as a Microsoft Word

            Object propertyValue;
            try
            {
                propertyValue = selection.GetPropertyValue(formattingProperty);
            }
            catch (NullReferenceException) // including NullReferenceException if selection is null
            {
                propertyValue = null;
            }

            return propertyValue;
        }

        internal static Double? GetSelectionFontSize(Object o)
        {
            Double value = Double.NaN;

            if (o is Double)
            {
                value = (Double)o;
            }
            else
            {
                String so = o as String;
                if (so != null)
                {
                    if (!Double.TryParse(so, out value))
                        value = Double.NaN;
                }
            }

            if (value > 0 && value < Double.MaxValue)
                return value;

            return null;
        }

        internal static Color? GetSelectionColor(Object o, Color defaultColor)
        {
            if (o == null)
                return defaultColor;

            if (o == DependencyProperty.UnsetValue)
                return null;

            SolidColorBrush so = o as SolidColorBrush;
            if (so != null)
                return so.Color;

            return o as Color?;
        }

        internal static void ApplyNewValueToFormattingProperty<T>(TextSelection selection,
                                                                  DependencyProperty formattingProperty,
                                                                  Object newValue,
                                                                  Equaler<T> equals)
        {
            Object propertyValue = GetSelectionPropertyValue(selection, formattingProperty);

            Boolean shouldUpdate;
            if (propertyValue == null)
                shouldUpdate = newValue != null;
            else if (propertyValue == DependencyProperty.UnsetValue)
                shouldUpdate = newValue != DependencyProperty.UnsetValue;
            else if (propertyValue is T)
                shouldUpdate = !equals((T)propertyValue, (T)newValue);
            else
                throw new InvalidOperationException();

            if (shouldUpdate)
                selection.ApplyPropertyValue(formattingProperty, newValue);
        }

        internal static void ToggleSelectionFormattingProperty<T>(TextSelection selection, 
                                                                  DependencyProperty formattingProperty,
                                                                  Object valueOn,
                                                                  Object valueOff,
                                                                  Equaler<T> equals)
        {
            Object propertyValue = GetSelectionPropertyValue(selection, formattingProperty);

            if (propertyValue is T)
            {
                propertyValue =
                    equals((T)propertyValue, (T)valueOn)
                    ? valueOff
                    : valueOn;
            }
            else if (propertyValue == null
                || propertyValue == DependencyProperty.UnsetValue)
            {
                propertyValue = valueOn;
            }
            else
            {
                throw new InvalidOperationException();
            }

            selection.ApplyPropertyValue(formattingProperty, propertyValue);
        }
        /// <summary>
        /// Выполнить действие для всех элементов BlockCollection
        /// </summary>
        /// <param name="collection">Коллекция</param>
        /// <param name="isRecursive">Если True, то для элементов, содержащих BlockCollection действие будет выполнятся и для их дочерних элементов</param>
        /// <param name="action">Действие</param>
        public static void ForEach(this BlockCollection collection, bool isRecursive, Action<Block> action)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            if (action == null)
                throw new ArgumentNullException("action");

            Block current = collection.FirstBlock;
            while (current != null)
            {
                Block NextBlock = current.NextBlock;

                action(current);

                Section sec = current as Section;
                if (isRecursive && sec != null)
                    sec.Blocks.ForEach(isRecursive, action);

                current = NextBlock;
            }
        }

        /// <summary>
        /// Удалить все элементы, удовлетворяющие условию condition
        /// </summary>
        /// <param name="collection">Коллекция</param>
        /// <param name="isRecursive">Если True, то для элементов, содержащих BlockCollection удалятся будут и их дочерние элементы</param>
        /// <param name="condition">Действие</param>
        public static void RemoveAll(this BlockCollection collection, bool isRecursive, Func<Block,bool> condition)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            if (condition == null)
                throw new ArgumentNullException("condition");

            Block current = collection.FirstBlock;
            while (current != null)
            {
                Block NextBlock = current.NextBlock;

                if (condition(current))
                    collection.Remove(current);

                Section sec = current as Section;
                if (isRecursive && sec != null)
                    sec.Blocks.RemoveAll(isRecursive, condition);

                current = NextBlock;
            }
        }

        /// <summary>
        /// Выровнять элемент по пиксельной сетке
        /// </summary>
        /// <param name="element"></param>
        public static void SnapToPixels(this UIElement element)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            Point pixelOffset = new Point();

            PresentationSource ps = PresentationSource.FromVisual(element);
            if (ps != null)
            {
                Visual rootVisual = ps.RootVisual;

                pixelOffset = element.TransformToAncestor(rootVisual).Transform(pixelOffset);
                pixelOffset = ps.CompositionTarget.TransformToDevice.Transform(pixelOffset);

                Point roundedOffset = new Point();

                roundedOffset.X = Math.Round(pixelOffset.X);
                roundedOffset.Y = Math.Round(pixelOffset.Y);

                if (roundedOffset.X != pixelOffset.X || roundedOffset.Y != pixelOffset.Y)
                {
                    //MessageBox.Show("Rounding " + pixelOffset.X.ToString() + " to " + roundedOffset.X.ToString()
                    //                      + " " + pixelOffset.Y.ToString() + " to " + roundedOffset.Y.ToString());
                    roundedOffset = ps.CompositionTarget.TransformFromDevice.Transform(roundedOffset);
                    roundedOffset = rootVisual.TransformToDescendant(element).Transform(roundedOffset);

                    element.RenderTransform = new TranslateTransform(roundedOffset.X * App.DpiXScalingFactor, roundedOffset.Y * App.DpiYScalingFactor);

                }
            }
        }

    } 

}
