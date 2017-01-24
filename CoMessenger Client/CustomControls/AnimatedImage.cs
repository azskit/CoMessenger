using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using COMessengerClient.ChatFace;

namespace COMessengerClient.CustomControls
{
    //stolen from... не помню откуда
    public class AnimatedImage : System.Windows.Controls.Image
    {
        const int PropertyTagFrameDelay = 0x5100;

        private BitmapSource[] _BitmapSources = null;

        private int currentFrame = 0;
        private int frameTimer = 0;
        private int currentFrameDelay;
        private int frameCount;
        private int[] frameDelays;
        private bool theFrameHasBeenChanged;

        private bool isAnimating = false;

        public bool IsAnimating
        {
            get { return isAnimating; }
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
            this.SnapToPixels();
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

        /// <summary>
        /// Инициализация данных для анимации
        /// </summary>
        private void UpdateAnimatedBitmap()
        {
            frameCount = AnimatedBitmap.GetFrameCount(FrameDimension.Time);

            if (frameCount > 0)
            {
                PropertyItem frameDelayItem = AnimatedBitmap.GetPropertyItem(PropertyTagFrameDelay);

                //Считываем временные отрезки жизни кадров
                if (frameDelayItem != null)
                {
                    frameDelays = new int[frameCount];

                    byte[] values = frameDelayItem.Value;
                    for (int i = 0; i < frameCount; ++i)
                    {
                        frameDelays[i] = values[i * 4] + 256 * values[i * 4 + 1] + 256 * 256 * values[i * 4 + 2] + 256 * 256 * 256 * values[i * 4 + 3];
                    }
                }

                //Разбираем изображение на кадры
                _BitmapSources = new BitmapSource[frameCount];

                for (int i = 0; i < frameCount; i++)
                {

                    AnimatedBitmap.SelectActiveFrame(FrameDimension.Time, i);
                    Bitmap bitmap = new Bitmap(AnimatedBitmap);
                    bitmap.MakeTransparent();

                    _BitmapSources[i] = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        bitmap.GetHbitmap(),
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }
            }
        }
        private delegate void VoidDelegate();

        private void OnFrameChanged()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Render, new VoidDelegate(delegate { ChangeSource(); }));

        }

        void ChangeSource()
        {
            Source = _BitmapSources[currentFrame];
            theFrameHasBeenChanged = false;
        }

        public void StopAnimate()
        {
            if (isAnimating)
            {
                lock (currentlyAnimating)
                {
                    currentlyAnimating.Remove(this);
                }

                isAnimating = false;
            }
        }

        public void StartAnimate()
        {
            if (!isAnimating)
            {
                isAnimating = true;

                lock (currentlyAnimating)
                {
                    currentlyAnimating.Add(this);
                }

                currentFrame = 0;
                Source = _BitmapSources[currentFrame];
                frameTimer = 0;
                currentFrameDelay = frameDelays[currentFrame];
            }

            if (animationThread == null)
            {
                animationThread = new Thread(new ThreadStart(AnimationRoutine));
                animationThread.Name = "Animation Thread";
                animationThread.IsBackground = true;
                animationThread.Start();
            }
        }


        /// <summary>
        /// Отдельный поток для отсчета времени жизни кадров (один для всех изображений)
        /// </summary>
        static Thread animationThread;

        /// <summary>
        /// Набор анимируемых в данный момент изображений
        /// </summary>
        private static List<AnimatedImage> currentlyAnimating = new List<AnimatedImage>();

        ///Интервал отсчета времени жизни кадра, 5\100 секунды 
        private const int interval = 5;

        /// <summary>
        /// Поток для отсчета времени жизни кадров. Создается вместе с 1-ым изображением и заканчивает работу, когда удаляется последнее
        /// </summary>
        static void AnimationRoutine()
        {

            while (currentlyAnimating.Count > 0)
            {

                List<AnimatedImage> snapshot;
                lock (currentlyAnimating)
                {
                    snapshot = new List<AnimatedImage>(currentlyAnimating);
                }


                foreach (AnimatedImage currentImage in snapshot)
                {

                    currentImage.frameTimer += interval;
                    if (currentImage.frameTimer >= currentImage.currentFrameDelay)
                    {

                        currentImage.currentFrame = ++currentImage.currentFrame % currentImage.frameCount;
                        currentImage.currentFrameDelay = currentImage.frameDelays[currentImage.currentFrame];

                        currentImage.frameTimer = 0;

                        //Если UI поток не успел сменить кадр, то пропускаем
                        if (!currentImage.theFrameHasBeenChanged)
                        {
                            currentImage.theFrameHasBeenChanged = true;
                            currentImage.OnFrameChanged();
                        }
                    }
                }
                Thread.Sleep(interval*10);
            }
            animationThread = null;
        }
    }
}
