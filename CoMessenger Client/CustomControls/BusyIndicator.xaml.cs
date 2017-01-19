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

namespace COMessengerClient.CustomControls
{
    /// <summary>
    /// Interaction logic for BusyIndicator.xaml
    /// </summary>
    public partial class BusyIndicator : UserControl
    {


        public double Radius
        {
            get { return (double)GetValue(RadiusProperty); }
            set { SetValue(RadiusProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Radius.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RadiusProperty =
            DependencyProperty.Register("Radius", typeof(double), typeof(BusyIndicator), new PropertyMetadata((double)0, RadiusChanged));

        private static void RadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BusyIndicator indicator = d as BusyIndicator;
            indicator.rectangle.SetValue(Border.CornerRadiusProperty, new CornerRadius(0, (double)e.NewValue, (double)e.NewValue, (double)e.NewValue));
        }

        public BusyIndicator()
        {
            InitializeComponent();
        }
    }
}
