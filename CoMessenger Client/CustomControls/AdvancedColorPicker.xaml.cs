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
using System.Diagnostics;

namespace COMessengerClient.CustomControls
{
    /// <summary>
    /// Логика взаимодействия для CustomColor.xaml
    /// </summary>
    public partial class AdvancedColorSelector : UserControl
    {
        public AdvancedColorSelector()
        {
            InitializeComponent();
            this.DataContext = this;
        }



        public Color MainColor
        {
            get { return (Color)GetValue(MainColorProperty); }
            set { SetValue(MainColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MainColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MainColorProperty =
            DependencyProperty.Register("MainColor", typeof(Color), typeof(AdvancedColorSelector), new UIPropertyMetadata(Colors.Red));



        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Color), typeof(AdvancedColorSelector), new UIPropertyMetadata(Colors.Red));

        private void slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            float r = 1f, g = 0f, b = 0f;


            if (e.NewValue >= 0 && e.NewValue < 0.2)
            {
                r = 1f;
                g = 1f*((float)e.NewValue - 0)/0.2f;
                b = 0;
            }
            else if (e.NewValue >= 0.2 && e.NewValue <= 0.4)
            {
                r = 1f * (-(float)e.NewValue + 0.4f) / 0.2f;
                g = 1f;
                b = 0;
            }
            else if (e.NewValue >= 0.4 && e.NewValue <= 0.6)
            {
                r = 0;
                g = 1f;
                b = 1f * ((float)e.NewValue - 0.4f) / 0.2f;
            }
            else if (e.NewValue >= 0.6 && e.NewValue <= 0.8)
            {
                r = 0;
                g = 1f * (-(float)e.NewValue + 0.8f) / 0.2f;
                b = 1f;
            }
            else if (e.NewValue >= 0.8 && e.NewValue <= 1.0)
            {
                r = 1f * ((float)e.NewValue - 0.8f) / 0.2f;
                g = 0;
                b = 1f;
            }

            MainColor = new Color() { ScR = r, ScG = g, ScB = b, ScA = 1 };

            SelectedColor = GetColorByPivotPosition();
        }

        private bool DragMode;

        private void Canvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //Debug.WriteLine(args.GetPosition((IInputElement)sender));

            DragMode = true;
        }

        private void ColorCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (DragMode)
            {
                IInputElement RelativeElement = sender as IInputElement;

                if (RelativeElement != null)
                {
                    Point MousePosition = e.GetPosition(RelativeElement);

                    Double X = MousePosition.X >= (ColorCanvas.ActualWidth) ? (ColorCanvas.ActualWidth) : MousePosition.X;
                    Double Y = MousePosition.Y >= (ColorCanvas.ActualHeight) ? (ColorCanvas.ActualHeight) : MousePosition.Y;

                    X = X <= 0 ? 0 : MousePosition.X;
                    Y = Y <= 0 ? 0 : MousePosition.Y;

                    Pivot.SetValue(Canvas.LeftProperty, X - Pivot.ActualWidth / 2);
                    Pivot.SetValue(Canvas.TopProperty, Y - Pivot.ActualHeight / 2);

                    SelectedColor = GetColorByPivotPosition();

                    e.Handled = true;
                }
            }
        }

        private void ColorCanvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            DragMode = false;
        }

        private Color GetColorByPivotPosition()
        {
            Double X = (Double)Pivot.GetValue(Canvas.LeftProperty) - Pivot.ActualWidth / 2;
            Double Y = (Double)Pivot.GetValue(Canvas.TopProperty) - Pivot.ActualHeight / 2;

            float r = 0f, g = 0f, b = 0f;

            float Darkness = 1f * ((float)X / (float)ColorCanvas.ActualWidth - 0.5f) + 1f * ((float)Y / (float)ColorCanvas.ActualHeight - 0.5f);
            float Strength = (float)X / (float)ColorCanvas.ActualWidth;

            r = MainColor.ScR * Strength - Darkness;
            g = MainColor.ScG * Strength - Darkness;
            b = MainColor.ScB * Strength - Darkness;

            if (r > 1) r = 1;
            if (g > 1) g = 1;
            if (b > 1) b = 1;

            if (r < 0) r = 0;
            if (g < 0) g = 0;
            if (b < 0) b = 0;

            return Color.FromScRgb(SelectedColorAlpha/255f, r, g, b);
        }

        public byte SelectedColorAlpha
        {
            get { return (byte)GetValue(SelectedColorAlphaProperty); }
            set { SetValue(SelectedColorAlphaProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedColorAlpha.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedColorAlphaProperty =
            DependencyProperty.Register("SelectedColorAlpha", typeof(byte), typeof(AdvancedColorSelector), new UIPropertyMetadata((byte)255));



        public byte SelectedColorRed
        {
            get { return (byte)GetValue(SelectedColorRedProperty); }
            set { SetValue(SelectedColorRedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedColorRed.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedColorRedProperty =
            DependencyProperty.Register("SelectedColorRed", typeof(byte), typeof(AdvancedColorSelector), new UIPropertyMetadata((byte)0));

        public byte SelectedColorGreen
        {
            get { return (byte)GetValue(SelectedColorGreenProperty); }
            set { SetValue(SelectedColorGreenProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedColorGreen.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedColorGreenProperty =
            DependencyProperty.Register("SelectedColorGreen", typeof(byte), typeof(AdvancedColorSelector), new UIPropertyMetadata((byte)0));



        public byte SelectedColorBlue
        {
            get { return (byte)GetValue(SelectedColorBlueProperty); }
            set { SetValue(SelectedColorBlueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedColorBlue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedColorBlueProperty =
            DependencyProperty.Register("SelectedColorBlue", typeof(byte), typeof(AdvancedColorSelector), new UIPropertyMetadata((byte)0));


        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            switch (e.Property.Name)
            {
                case "SelectedColorAlpha": SelectedColor = Color.FromArgb((byte)e.NewValue, SelectedColor.R, SelectedColor.G, SelectedColor.B); OpacityValue = ((255 - (byte)e.NewValue) / 255.0 * 100); break;
                case "SelectedColorRed": SelectedColor = Color.FromArgb(SelectedColor.A, (byte)e.NewValue, SelectedColor.G, SelectedColor.B); break;
                case "SelectedColorGreen": SelectedColor = Color.FromArgb(SelectedColor.A, SelectedColor.R, (byte)e.NewValue, SelectedColor.B); break;
                case "SelectedColorBlue": SelectedColor = Color.FromArgb(SelectedColor.A, SelectedColor.R, SelectedColor.G, (byte)e.NewValue); break;
                case "SelectedColor": SelectedColorAlpha = SelectedColor.A;
                                      SelectedColorRed = SelectedColor.R;
                                      SelectedColorGreen = SelectedColor.G;
                                      SelectedColorBlue = SelectedColor.B;
                                      OpacityValue = ((255 - SelectedColorAlpha) / 255.0 * 100);
                     break;
                default:
                    break;
            }
            base.OnPropertyChanged(e);
        }



        public double OpacityValue
        {
            get { return (double)GetValue(OpacityValueProperty); }
            set { SetValue(OpacityValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for OpacityValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OpacityValueProperty =
            DependencyProperty.Register("OpacityValue", typeof(double), typeof(AdvancedColorSelector), new UIPropertyMetadata(0d));

        


    }
}
