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
using System.Collections.ObjectModel;
using System.Globalization;

namespace COMessengerClient.CustomControls
{
    /// <summary>
    /// Interaction logic for ColorPicker.xaml
    /// </summary>
    public partial class ColorPicker : UserControl
    {
        public ColorPicker()
        {
            InitializeComponent();

            this.DataContext = this;

            AdvancedPicker.SetBinding(AdvancedColorSelector.SelectedColorProperty, new Binding("SelectedColor") { Source = this, Mode = BindingMode.TwoWay });

            SetPalette(StandartPalette);
            //SelectedColor = Colors.Violet;

            //Popup.IsMouseCaptureWithinChanged += Popup_IsMouseCaptureWithinChanged;

        }

        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }
        public static readonly DependencyProperty SelectedColorProperty =
      DependencyProperty.Register("SelectedColor", typeof(Color),
              typeof(ColorPicker), new PropertyMetadata(Colors.Transparent));




        public Collection<ColorItem> ColorSet
        {
            get { return (Collection<ColorItem>)GetValue(ColorSetProperty); }
        }

        // Using a DependencyProperty as the backing store for ColorSet.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ColorSetProperty =
            DependencyProperty.Register("ColorSet", typeof(Collection<ColorItem>), typeof(ColorPicker), new UIPropertyMetadata(new Collection<ColorItem>()));

        private void ColorSelected(object sender, RoutedEventArgs e)
        {
            Color OldColor = SelectedColor;
            SelectedColor = ((SolidColorBrush)((StackPanel)((Button)sender).Content).Background).Color;

            if (SelectedColorChanged != null)
                SelectedColorChanged.Invoke(this, new RoutedPropertyChangedEventArgs<Color>(OldColor, SelectedColor));

            Popup.IsOpen = false;

        }

        public event RoutedPropertyChangedEventHandler<Color> SelectedColorChanged;

        public static Collection<ColorItem> StandartPalette
        {
            get
            {
                Collection<ColorItem> palette = new Collection<ColorItem>();

                Color[] solidcolors = { Colors.LightGray, Colors.Gray, Colors.IndianRed, Colors.Red, Colors.Orange, Colors.Yellow, Colors.LightGreen, Colors.Green, Colors.LightBlue, Colors.Blue };

                float[] tones = { 100.0f, 80.0f, 50.0f };

                foreach (float tone in tones)
                {
                    foreach (Color col in solidcolors)
                    {

                        palette.Add(new ColorItem(color: Color.FromScRgb(tone / 100, col.ScR, col.ScG, col.ScB),
                                                  name: col.ToString() + " " + tone.ToString(CultureInfo.CurrentCulture)));
                    }
                }


                palette.Add(new ColorItem(color: Colors.Black, name: "Черный"));
                palette.Add(new ColorItem(color: Colors.White, name: "Белый"));
                palette.Add(new ColorItem(color: Colors.Transparent, name: "Нет цвета"));

                return palette;
            }
        }

        public void SetPalette(Collection<ColorItem> colorCollection)
        {
            ColorSet.Clear();

            if (colorCollection != null)
            {
                foreach (ColorItem NewColor in colorCollection)
                {
                    Button n_btn = new Button();

                    n_btn.MinHeight = 16;
                    n_btn.MinWidth = 16;

                    n_btn.Style = this.Resources["HighLight"] as Style;

                    n_btn.Content = new StackPanel() { Background = new SolidColorBrush(NewColor.Color) };
                    n_btn.ToolTip = NewColor.Name;
                    n_btn.Click += this.ColorSelected;

                    Palette.Children.Add(n_btn);
                }

            }

        }

    }

    public class ColorItem
    {
        public ColorItem(Color color, string name)
        {
            Color = color;
            Name = name;
        }

        public Color Color { get; set; }
        public string Name { get; set; }

    }

}
