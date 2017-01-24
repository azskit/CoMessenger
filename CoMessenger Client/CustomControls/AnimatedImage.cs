using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace COMessengerClient.CustomControls
{
    //stolen from... не помню откуда
    public class AnimatedImage : System.Windows.Controls.Image
    {
        private BitmapSource[] _BitmapSources = null;
        private int _nCurrentFrame = 0;
        private static int count = 0;
        public static int Count { get { return count; }
            set
            {
                count = value;

                //((StartScreen.StartScreenView)App.ThisApp.MainWindow).ConnectionStatusBar.Items.Clear();
                //((StartScreen.StartScreenView)App.ThisApp.MainWindow).ConnectionStatusBar.Items.Add(new TextBlock(new System.Windows.Documents.Run(count.ToString())));
            }
        }

        private bool _bIsAnimating = false;

        public bool IsAnimating
        {
            get { return _bIsAnimating; }
        }

        static AnimatedImage()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AnimatedImage), new FrameworkPropertyMetadata(typeof(AnimatedImage)));
        }

        public AnimatedImage()
        {
            Loaded += AnimatedImage_Loaded;
            Unloaded += AnimatedImage_Unloaded;
        }

        private void AnimatedImage_Loaded(object sender, RoutedEventArgs e)
        {
            StartAnimate();
        }

        private void AnimatedImage_Unloaded(object sender, RoutedEventArgs e)
        {
            StopAnimate();
        }

        public Bitmap AnimatedBitmap
        {
            get { return (Bitmap)GetValue(AnimatedBitmapProperty); }
            set { StopAnimate(); SetValue(AnimatedBitmapProperty, value); }
        }

        /// <summary>
        /// Identifies the Value dependency property.
        /// </summary>
        public static readonly DependencyProperty AnimatedBitmapProperty =
            DependencyProperty.Register(
                "AnimatedBitmap", typeof(Bitmap), typeof(AnimatedImage),
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnAnimatedBitmapChanged)));

        private static void OnAnimatedBitmapChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            AnimatedImage control = (AnimatedImage)obj;

            control.UpdateAnimatedBitmap();

            RoutedPropertyChangedEventArgs<Bitmap> e = new RoutedPropertyChangedEventArgs<Bitmap>(
                (Bitmap)args.OldValue, (Bitmap)args.NewValue, AnimatedBitmapChangedEvent);
            control.OnAnimatedBitmapChanged(e);
        }

        /// <summary>
        /// Identifies the ValueChanged routed event.
        /// </summary>
        public static readonly RoutedEvent AnimatedBitmapChangedEvent = EventManager.RegisterRoutedEvent(
            "AnimatedBitmapChanged", RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<Bitmap>), typeof(AnimatedImage));

        /// <summary>
        /// Occurs when the Value property changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<Bitmap> AnimatedBitmapChanged
        {
            add { AddHandler(AnimatedBitmapChangedEvent, value); }
            remove { RemoveHandler(AnimatedBitmapChangedEvent, value); }
        }

        /// <summary>
        /// Raises the ValueChanged event.
        /// </summary>
        /// <param name="args">Arguments associated with the ValueChanged event.</param>
        protected virtual void OnAnimatedBitmapChanged(RoutedPropertyChangedEventArgs<Bitmap> args)
        {
            RaiseEvent(args);
        }
        private void UpdateAnimatedBitmap()
        {
            int nTimeFrames = AnimatedBitmap.GetFrameCount(System.Drawing.Imaging.FrameDimension.Time);
            _nCurrentFrame = 0;
            if (nTimeFrames > 0)
            {

                _BitmapSources = new BitmapSource[nTimeFrames];

                for (int i = 0; i < nTimeFrames; i++)
                {

                    AnimatedBitmap.SelectActiveFrame(System.Drawing.Imaging.FrameDimension.Time, i);
                    Bitmap bitmap = new Bitmap(AnimatedBitmap);
                    bitmap.MakeTransparent();

                    _BitmapSources[i] = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        bitmap.GetHbitmap(),
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                }
                //StartAnimate();
            }
        }
        private delegate void VoidDelegate();

        private void OnFrameChanged(object o, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Render, new VoidDelegate(delegate { ChangeSource(); }));

        }
        void ChangeSource()
        {
            App.Log.Add("animation", Name + " animated");

            Source = _BitmapSources[_nCurrentFrame++];
            _nCurrentFrame = _nCurrentFrame % _BitmapSources.Length;
            ImageAnimator.UpdateFrames();

        }

        public void StopAnimate()
        {
            if (_bIsAnimating)
            {
                //Count--;

                App.Log.Add("animation", Name + " removed");

                ImageAnimator.StopAnimate(AnimatedBitmap, new EventHandler(this.OnFrameChanged));
                _bIsAnimating = false;
            }
        }

        public void StartAnimate()
        {
            if (!_bIsAnimating)
            {
                Name = "anim" + Count++.ToString();
                App.Log.Add("animation", Name + " added");
                //((StartScreen.StartScreenView)App.ThisApp.MainWindow).ConnectionStatusBar.Items.Clear();
                //((StartScreen.StartScreenView)App.ThisApp.MainWindow).ConnectionStatusBar.Items.Add(new TextBlock(new System.Windows.Documents.Run()));

                ImageAnimator.Animate(AnimatedBitmap, new EventHandler(this.OnFrameChanged));
                _bIsAnimating = true;
                Source = _BitmapSources[_nCurrentFrame++];
            }
        }
    }
    
}
