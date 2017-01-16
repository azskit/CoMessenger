using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace COMessengerClient.CustomControls
{

    /// <summary>
    /// Interaction logic for MessagesCanvas.xaml
    /// </summary>
    public partial class MessagesCanvas : UserControl
    {
        public event EventHandler ScrollViewerStartPositionReached;

        public ScrollViewer ActualScrollViewer { get; private set; }
        public StackPanel BackgroundStackPanel { get; private set; }

        private object dummy = new object();

        public bool IsBusy
        {
            get { return BusyIndicator.Visibility == System.Windows.Visibility.Visible; }
            set
            {
                if (value == true)
                {
                    App.ThisApp.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        BusyIndicator.Visibility = Visibility.Visible;
                    }));
                }
                else
                {
                    App.ThisApp.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        BusyIndicator.Visibility = Visibility.Hidden;
                    }));
                }
            }
        }

        public MessagesCanvas()
        {
            InitializeComponent();

            BackgroundStackPanel = new StackPanel();

            BackgroundStackPanel.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            BackgroundStackPanel.Background= new SolidColorBrush(Colors.Transparent);

        }

        #region Обработчики
        /// <summary>
        /// Выравниваем положение ползунка скроллинга для того чтобы избежать полупиксельного рендеринга (иначе будем наблюдать размытие изображений)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ChatBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            //if (ScrollViewerStartPositionReached != null && e.VerticalOffset == 0 && e.VerticalChange < 0)
            //    ScrollViewerStartPositionReached(this, new EventArgs());

            double resultOffset = e.VerticalOffset;

            if (e.VerticalOffset != Math.Floor(e.VerticalOffset))
            {
                resultOffset = Math.Floor(e.VerticalOffset);
                ActualScrollViewer.ScrollToVerticalOffset(resultOffset);
            }

            //Background_scroll.ScrollToVerticalOffset(resultOffset);
        }

        /// <summary>
        /// Проброс прокрутки колеса мыши на задний скроллер
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ChatBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {

            //Если скроллинг стоит в начале и мы пытаемся мышкой прокрутить еще выше
            if (ScrollViewerStartPositionReached != null && ActualScrollViewer.VerticalOffset == 0 && e.Delta > 0)
                ScrollViewerStartPositionReached(this, new EventArgs());

        }

        #endregion

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            lock (dummy)
            {
                if (ActualScrollViewer == null) //Первичная загрузка
                {
                    ActualScrollViewer = ScrollViewerHelper.GetChild<ScrollViewer>(ChatBox);

                    //ActualScrollViewer.CanContentScroll = false;

                    //Вставка фона
                    UIElement flowdoc = ActualScrollViewer.Content as UIElement;

                    ActualScrollViewer.Content = null;

                    Grid newgrid = new Grid();

                    //Background_scroll.Content = null;

                    newgrid.Children.Add(BackgroundStackPanel);
                    newgrid.Children.Add(flowdoc);

                    BindingOperations.SetBinding(flowdoc, FrameworkElement.WidthProperty, new Binding("ViewportWidth") { Source = ActualScrollViewer });

                    ActualScrollViewer.Content = newgrid;

                    //ActualScrollViewer.


                    //Перестановка скроллбара
                    Grid grd = ScrollViewerHelper.GetChild<Grid>(ActualScrollViewer);

                    MainGrid.Children.Remove(Header);

                    //Меняем описание ширины колонок
                    grd.ColumnDefinitions[0].Width = GridLength.Auto;
                    grd.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);



                    //Ставим контент в правую колонку
                    grd.Children[1].SetValue(Grid.ColumnProperty, 1);

                    //ScrollContentPresenter content = grd.Children[1] as ScrollContentPresenter;

                    //FrameworkElement ele = content.Content as FrameworkElement;

                    //Grid doc = content.Content as Grid;

                    //FlowDocumentView doc.Children[1]


                    //doc.Background = new SolidColorBrush(Colors.AliceBlue);

                    //А скроллбар в левую
                    grd.Children[2].SetValue(Grid.ColumnProperty, 0);


                    //System.Windows.Controls.Primitives.ScrollBar
                    grd.Children.Insert(2, Header);

                    Header.SetValue(Grid.ColumnProperty, 1);
                }
            }
        }

    }



    public class ScrollViewerHelper : DependencyObject
    {
        //public static readonly DependencyProperty ScrollPositionProperty =
        //    DependencyProperty.RegisterAttached("ScrollPosition", typeof(double), typeof(ScrollViewerHelper), new FrameworkPropertyMetadata(ScrollPositionChangedCallback));


        //public static void ScrollPositionChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    var fdScrollViewer = d as FlowDocumentScrollViewer;
        //    var top = (double)e.NewValue;

        //    var scrollViewer = GetChild<ScrollViewer>(fdScrollViewer);

        //    scrollViewer.ScrollToVerticalOffset(top);
        //}

        public static T GetChild<T>(DependencyObject obj) where T : DependencyObject
        {
            //if (VisualTreeHelper.GetChildrenCount(obj) == 0)
            //    return null;

            T Retval;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject res = VisualTreeHelper.GetChild(obj, i);

                Retval = res as T;

                if (Retval != null)
                {
                    Trace.WriteLine(DateTime.Now.ToString() + ": 1 Найден " + typeof(T).Name + " thread = " + Thread.CurrentThread.Name);

                    return Retval;
                }

                if (VisualTreeHelper.GetChildrenCount(res) > 0)
                {
                    res = GetChild<T>(res);
                    Retval = res as T;

                    if (Retval != null)
                    {
                        Trace.WriteLine(DateTime.Now.ToString() + ": 2 Найден " + typeof(T).Name + " thread = " + Thread.CurrentThread.Name);
                        return Retval;
                    }
                }
            }
            throw new InvalidOperationException("Such child is not found");
        }

    }




}
