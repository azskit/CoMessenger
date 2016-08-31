using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Windows;
using System.ComponentModel;
using System.Windows.Media;
using System.Globalization;

namespace COMessengerClient.Properties
{
    public class WindowSizeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string s_value = value as string;

            if (s_value != null)
            {
                string[] parts = s_value.Split(new char[] { ',' });
                WindowSize winsize = new WindowSize();
                winsize.Height = Convert.ToDouble(parts[0], CultureInfo.InvariantCulture);
                winsize.Width = Convert.ToDouble(parts[1], CultureInfo.InvariantCulture);
                return winsize;
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                WindowSize winsize = value as WindowSize;
                return string.Format(CultureInfo.InvariantCulture, "{0},{1}", winsize.Height, winsize.Width);
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    [TypeConverter(typeof(WindowSizeConverter))]
    [SettingsSerializeAs(SettingsSerializeAs.String)]
    public class WindowSize
    {
        public double Height { get; set; }
        public double Width { get; set; }
    }

    public class FontInfoConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string s_value = value as string;

            if (s_value != null)
            {
                string[] parts = s_value.Split(new char[] { ',' });
                FontInfo fontfnfo = new FontInfo();
                fontfnfo.Family = new FontFamily(parts[0]);
                fontfnfo.Size = Convert.ToDouble(parts[1], CultureInfo.InvariantCulture);
                fontfnfo.FontColor = Color.FromRgb(Convert.ToByte(parts[2], CultureInfo.InvariantCulture), Convert.ToByte(parts[3], CultureInfo.InvariantCulture), Convert.ToByte(parts[4], CultureInfo.InvariantCulture));
                return fontfnfo;
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                FontInfo fontfnfo = value as FontInfo;
                return string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4}", fontfnfo.Family.ToString(), fontfnfo.Size, fontfnfo.FontColor.R, fontfnfo.FontColor.G, fontfnfo.FontColor.B);
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }


    [TypeConverter(typeof(FontInfoConverter))]
    [SettingsSerializeAs(SettingsSerializeAs.String)]
    public class FontInfo : INotifyPropertyChanged
    {
        private double _size;

        public double Size
        {
            get { return _size; }
            set
            {
                if (_size != value)
                {
                    _size = value;
                    NotifyPropertyChanged("Size");
                }
            }
        }

        private FontFamily _family;

        public FontFamily Family
        {
            get { return _family; }
            set
            {
                if (_family != value)
                {
                    _family = value;
                    NotifyPropertyChanged("Family");
                }
            }
        }

        private Color _fontcolor;

        public Color FontColor
        {
            get { return _fontcolor; }
            set
            {
                if (_fontcolor != value)
                {
                    _fontcolor = value;
                    NotifyPropertyChanged("FontColor");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
