using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Markup.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Serialization;
using COMessengerClient.CustomControls;

namespace COMessengerClient.Tools
{


    public class BinaryCacheManager : DependencyObject
    {

        public static Dictionary<string, byte[]> BinaryCache = new Dictionary<string, byte[]>();

        public static readonly DependencyProperty BinarySourceProperty = DependencyProperty.RegisterAttached(
            "BinarySource", typeof(BinarySource), typeof(Image), new PropertyMetadata(null));

        public static void SetCustomValue(DependencyObject element, BinarySource value)
        {
            element.SetValue(BinarySourceProperty, value);
        }

        public static BinarySource GetCustomValue(DependencyObject element)
        {
            return (BinarySource)element.GetValue(BinarySourceProperty);
        }



    }


    public class BinarySourceConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return new BinarySource() { BinarySourceId = value as string };
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return ((BinarySource)value).BinarySourceId;
        }
    }


    [TypeConverter(typeof(BinarySourceConverter))]
    public class BinarySource
    {
        public string BinarySourceId { get; set; }
        public byte[] BinarySourceData { get; set; }

        public static BinarySource CreateFromImage(Image image)
        {

            BinarySource newBinarySource = new BinarySource();


            using (MemoryStream ms = new MemoryStream())
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image.Source as BitmapSource));
                encoder.Save(ms);
                ms.Position = 0;
                newBinarySource.BinarySourceData = new byte[ms.Length];
                ms.Read(newBinarySource.BinarySourceData, 0, newBinarySource.BinarySourceData.Length);
            }

            newBinarySource.BinarySourceId = HashImage(newBinarySource.BinarySourceData);

            return newBinarySource;
        }

        private static string HashImage(byte[] imageData)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                return ByteArrayToString(sha1.ComputeHash(imageData));
            }
        }

        //stolen from http://stackoverflow.com/questions/311165/how-do-you-convert-byte-array-to-hexadecimal-string-and-vice-versa
        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        internal ImageSource ToImageSource()
        {

            //Image img = element as Image;

            //img.Source = new BitmapImage() { StreamSource = new MemoryStream(BinaryCache[reader.Value]) };

            using (MemoryStream stream = new MemoryStream(BinarySourceData))
            {
                PngBitmapDecoder decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

                if (decoder.Frames != null && decoder.Frames.Count > 0)
                    return decoder.Frames[0];
            }

            throw new InvalidOperationException("Something went wrong");
        }
    }


    internal class CimSerializer
    {


        private XmlWriter writer;
        private StringBuilder sb;

        private Dictionary<Type, Dictionary<string, SerializableProperty>> propertiesOfType;
        //private Dictionary<DependencyProperty, PropertyDescriptor> propertyDescriptors;

        private class SerializableProperty
        {
            internal DependencyProperty Property { get; set; }
            internal DependencyPropertyDescriptor Descriptor { get; set; }
            internal TypeConverter Converter { get; set; }

            public SerializableProperty(DependencyProperty property)
            {
                Property = property;

                //DependencyPropertyDescriptor.FromProperty(FrameworkElement.VisibilityProperty, typeof(FrameworkElement));

                Descriptor = DependencyPropertyDescriptor.FromProperty(property, property.OwnerType);

                Converter = Descriptor?.Converter;

                if (Converter == null)
                    //Converter = Activator.CreateInstance(property.PropertyType.GetCustomAttributes(typeof(TypeConverterAttribute), false).FirstOrDefault() as Type) as TypeConverter;

                    Converter = TypeDescriptor.GetConverter(property.PropertyType);


                //Descriptor = TypeDescriptor.GetProperties(property.OwnerType)[property.Name] as DependencyPropertyDescriptor;

                //TypeConverterAttribute attr = Descriptor.Attributes[typeof(TypeConverterAttribute)] as TypeConverterAttribute;

                //Converter = Activator.CreateInstance(Type.GetType(attr.ConverterTypeName)) as TypeConverter;
            }
        }


        private void initPropertiesOfType()
        {
            propertiesOfType = new Dictionary<Type, Dictionary<string, SerializableProperty>>();

            #region TableCell
            propertiesOfType.Add(

                key: typeof(TableCell),

                value: new List<SerializableProperty>()
                {
                        new SerializableProperty(TableCell.BorderBrushProperty         ),
                        new SerializableProperty(TableCell.BorderThicknessProperty     ),
                        new SerializableProperty(TableCell.ColumnSpanProperty          ),
                        new SerializableProperty(TableCell.FlowDirectionProperty       ),
                        new SerializableProperty(TableCell.LineHeightProperty          ),
                        new SerializableProperty(TableCell.LineStackingStrategyProperty),
                        new SerializableProperty(TableCell.PaddingProperty             ),
                        new SerializableProperty(TableCell.RowSpanProperty             ),
                        new SerializableProperty(TableCell.TextAlignmentProperty       )
                }.ToDictionary(s => s.Property.Name));
            #endregion

            #region TableColumn
            propertiesOfType.Add(

                key: typeof(TableColumn),

                value: new List<SerializableProperty>()
                {
                            new SerializableProperty(TableColumn.WidthProperty     ),
                            new SerializableProperty(TableColumn.BackgroundProperty)
                }.ToDictionary(s => s.Property.Name));
            #endregion

            #region Table
            propertiesOfType.Add(

                key: typeof(Table),

                value: new List<SerializableProperty>()
                {
                            new SerializableProperty(Table.CellSpacingProperty),
                            new SerializableProperty(Table.MarginProperty     )
                }.ToDictionary(s => s.Property.Name));
            #endregion

            #region Paragraph
            propertiesOfType.Add(

                key: typeof(Paragraph),

                value: new List<SerializableProperty>()
                {
                            new SerializableProperty(Paragraph.KeepTogetherProperty   ),
                            new SerializableProperty(Paragraph.KeepWithNextProperty   ),
                            new SerializableProperty(Paragraph.MinOrphanLinesProperty ),
                            new SerializableProperty(Paragraph.MinWidowLinesProperty  ),
                            new SerializableProperty(Paragraph.TextDecorationsProperty),
                            new SerializableProperty(Paragraph.TextIndentProperty     ),
                            new SerializableProperty(Paragraph.MarginProperty         ),
                            new SerializableProperty(Paragraph.TextAlignmentProperty  ),
                            new SerializableProperty(Paragraph.FontFamilyProperty     ),
                            new SerializableProperty(Paragraph.FontSizeProperty       )

                }.ToDictionary(s => s.Property.Name));
            #endregion

            #region Run
            propertiesOfType.Add(

                key: typeof(Run),

                value: new List<SerializableProperty>()
                {
                    new SerializableProperty(Run.FontFamilyProperty     ),
                    new SerializableProperty(Run.FontSizeProperty       ),
                    new SerializableProperty(Run.FontStretchProperty    ),
                    new SerializableProperty(Run.FontStyleProperty      ),
                    new SerializableProperty(Run.FontWeightProperty     ),
                    new SerializableProperty(Run.ForegroundProperty     ),
                    new SerializableProperty(Run.BackgroundProperty     ),
                    new SerializableProperty(Run.TextDecorationsProperty),
                    new SerializableProperty(Run.TextEffectsProperty    )
                }.ToDictionary(s => s.Property.Name));
            #endregion

            #region Span
            propertiesOfType.Add(

                key: typeof(Span),

                value: new List<SerializableProperty>()
                {
                    new SerializableProperty(Span.FontFamilyProperty     ),
                    new SerializableProperty(Span.FontSizeProperty       ),
                    new SerializableProperty(Span.FontStretchProperty    ),
                    new SerializableProperty(Span.FontStyleProperty      ),
                    new SerializableProperty(Span.FontWeightProperty     ),
                    new SerializableProperty(Span.ForegroundProperty     ),
                    new SerializableProperty(Span.BackgroundProperty     ),
                    new SerializableProperty(Span.TextDecorationsProperty),
                    new SerializableProperty(Span.TextEffectsProperty    )
                }.ToDictionary(s => s.Property.Name));
            #endregion

            #region Image
            propertiesOfType.Add(

                key: typeof(Image),

                value: new List<SerializableProperty>()
                {
                    new SerializableProperty(Image.WidthProperty  ),
                    new SerializableProperty(Image.HeightProperty ),
                    new SerializableProperty(Image.StretchProperty),
                    new SerializableProperty(BinaryCacheManager.BinarySourceProperty)
                }.ToDictionary(s => s.Property.Name));
            #endregion

            #region TextDecoration
            propertiesOfType.Add(

                key: typeof(TextDecoration),

                value: new List<SerializableProperty>()
                {
                    new SerializableProperty(TextDecoration.LocationProperty  )
                }.ToDictionary(s => s.Property.Name));
            #endregion


            //propertyDescriptors = new Dictionary<DependencyProperty, PropertyDescriptor>();

            //foreach (List<DependencyProperty> list in propertiesOfType.Values)
            //{
            //    foreach (DependencyProperty dp in list)
            //    {
            //        if (!propertyDescriptors.ContainsKey(dp))
            //        {
            //            propertyDescriptors.Add(dp, TypeDescriptor.GetProperties(dp.OwnerType)[dp.Name]);
            //        }
            //    }
            //}
        }

        public CimSerializer()
        {
            sb = new StringBuilder();
            writer = XmlWriter.Create(sb , new XmlWriterSettings() {  Indent = false, OmitXmlDeclaration = true });
            

            initPropertiesOfType();
        }

        #region Serialize Methods

        internal string Serialize(FlowDocument document)
        {
            MarkupObject markup = MarkupWriter.GetMarkupObjectFor(document);

            WriteMarkup(markup);

            writer.Flush();
            return sb.ToString();
        }

        private void WriteMarkup(MarkupObject markup)
        {
            ContentPropertyAttribute contentPropertyAttribute = markup.Attributes[typeof(ContentPropertyAttribute)] as ContentPropertyAttribute;

            writer.WriteStartElement(markup.ObjectType.Name);

            List<MarkupProperty> attributes = new List<MarkupProperty>();
            List<MarkupProperty> composites = new List<MarkupProperty>();
            MarkupProperty content = null;

            foreach (MarkupProperty item in markup.Properties)
            {
                if (item.IsComposite)
                {
                    composites.Add(item);
                    continue;
                }

                if (contentPropertyAttribute != null && contentPropertyAttribute.Name == item.Name)
                {
                    content = item;
                    continue;
                }

                writer.WriteAttributeString(item.Name, item.StringValue);
            }

            if (content != null && (content.StringValue.EndsWith(" ") || content.StringValue.StartsWith(" ")))
                writer.WriteAttributeString("xml", "space", "", "preserve");

            foreach (MarkupProperty item in composites)
            {
                foreach (MarkupObject subItem in item.Items)
                {
                    WriteMarkup(subItem);
                }
            }

            if (content != null)
            {
                writer.WriteString(content.StringValue);
            }

            writer.WriteEndElement();
        }

        internal string Serialize2(FlowDocument document)
        {
            var markup = MarkupWriter.GetMarkupObjectFor(document);

            foreach (var item in markup.Properties)
            {
                Console.WriteLine(item.StringValue);

                if (item.Value is IXmlSerializable)
                {
                    var i = 1;
                }
            }



            writer.WriteStartElement(markup.ObjectType.Name);

            foreach (Block property in document.Blocks)
            {
                WriteBlock(property);
            }


            writer.WriteEndElement();
            writer.Flush();
            return sb.ToString();


        }

        private void WriteBlock(Block property)
        {
            //writer.WriteStartElement(property.GetType().Name);

            Paragraph paragraph = property as Paragraph;

            if (paragraph != null)
            {
                writer.WriteStartElement("Paragraph");
                WriteAttributes(paragraph, typeof(Paragraph));
                #region Inlines
                foreach (Inline inline in paragraph.Inlines)

                {
                    WriteInline(inline);
                }
                #endregion

                writer.WriteEndElement();
            }

            Table table = property as Table;

            if (table != null)
            {
                writer.WriteStartElement("Table");
                WriteAttributes(table, typeof(Table));

                #region Columns
                writer.WriteStartElement("Table.Columns");

                foreach (TableColumn column in table.Columns)
                {
                    writer.WriteStartElement("TableColumn");
                    WriteAttributes(column, typeof(TableColumn));
                    //writer.WriteAttributeString("Width", column.GetValue(TableColumn.WidthProperty).ToString());
                    //writer.WriteAttributeString("Background", column.GetValue(TableColumn.BackgroundProperty).ToString());
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                #endregion

                #region RowGroups
                foreach (TableRowGroup rowGroup in table.RowGroups)
                {
                    writer.WriteStartElement("TableRowGroup");

                    foreach (TableRow row in rowGroup.Rows)
                    {
                        writer.WriteStartElement("TableRow");

                        foreach (TableCell cell in row.Cells)
                        {
                            writer.WriteStartElement("TableCell");
                            WriteAttributes(cell, typeof(TableCell));
                            foreach (Block block in cell.Blocks)
                            {
                                WriteBlock(block);
                            }
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
                #endregion

                writer.WriteEndElement();

            }
        }

        private void WriteAttributes(DependencyObject element, Type type)
        {

            List<DependencyProperty> collections = new List<DependencyProperty>();

            foreach (SerializableProperty property in propertiesOfType[type].Values)
            {
                if (property.Descriptor == null || property.Descriptor.ShouldSerializeValue(element))
                {
                    object value = element.GetValue(property.Property);

                    if (value is ICollection)
                    {
                        collections.Add(property.Property);
                        continue;
                    }

                    FrameworkContentElement contentElement = element as FrameworkContentElement;

                    if (contentElement == null || (value != contentElement.Parent.GetValue(property.Property)))
                    {
                        writer.WriteAttributeString(property.Property.Name, property.Converter.ConvertToInvariantString(value));
                    }
                }
            }

            Run run = element as Run;
            if (run != null)
            {
                if (run.Text.EndsWith(" ") || run.Text.StartsWith(" "))
                    writer.WriteAttributeString("xml", "space", "", "preserve");
            }

            foreach (DependencyProperty collectionProperty in collections)
            {
                writer.WriteStartElement(type.Name + "." + collectionProperty.Name);

                ICollection value = element.GetValue(collectionProperty) as ICollection;

                foreach (var item in value)
                {
                    WriteMarkup(MarkupWriter.GetMarkupObjectFor(item));
                }

                writer.WriteEndElement();
            }
        }

        //private IEnumerable<KeyValuePair<string, string>> DifferAttributes(TextElement textElement)
        //{
        //    foreach (DependencyProperty dp in new List<DependencyProperty>()
        //                    {
        //                        TextElement.FontFamilyProperty,
        //                        TextElement.FontSizeProperty,
        //                        TextElement.FontStretchProperty,
        //                        TextElement.FontStyleProperty,
        //                        TextElement.FontWeightProperty,
        //                        TextElement.ForegroundProperty,
        //                        TextElement.BackgroundProperty,

        //                    })
        //    {
        //        object value = textElement.GetValue(dp);

        //        if (value != textElement.Parent.GetValue(dp))
        //        {
        //            yield return new KeyValuePair<string, string>(dp.Name, value.ToString());
        //        }
        //    }
        //}

        private void WriteInline(Inline inline)
        {
            Span span = inline as Span;

            if (span != null)
            {
                writer.WriteStartElement("Span");
                WriteAttributes(span, typeof(Span));

                foreach (Inline spanInline in span.Inlines)
                {
                    WriteInline(spanInline);
                }
                writer.WriteEndElement();
            }

            Run run = inline as Run;

            if (run != null)
            {

                writer.WriteStartElement("Run");
                WriteAttributes(run, typeof(Run));


                writer.WriteString(run.Text);

                writer.WriteEndElement();
            }

            InlineUIContainer inlineUiContainer = inline as InlineUIContainer;

            if (inlineUiContainer != null)
            {
                Image image = inlineUiContainer.Child as Image;

                if (image != null)
                {
                    BinarySource imageSource = BinarySource.CreateFromImage(image);

                    BinaryCacheManager.SetCustomValue(image, imageSource);

                    writer.WriteStartElement("Image");
                    WriteAttributes(image, typeof(Image));


                    if (!BinaryCacheManager.BinaryCache.ContainsKey(imageSource.BinarySourceId))
                        BinaryCacheManager.BinaryCache.Add(imageSource.BinarySourceId, imageSource.BinarySourceData);
                    
                    //writer.WriteAttributeString("BinarySource", binaryDataId);

                    writer.WriteEndElement();

                }
            }
        } 
        #endregion



        private XmlReader reader;
        //FrameworkContentElement currentElement;

        public FlowDocument Deserialize(Stream stream)
        {
            reader = XmlReader.Create(stream);

            reader.ReadStartElement("FlowDocument");

            FlowDocument fldoc = new FlowDocument();

            do
            {
                ReadBlock(fldoc.Blocks);
            } while (reader.Read());

            return fldoc;
            
        }

        private void ReadBlock(BlockCollection collection)
        {
            switch (reader.Name)
            {
                case "Paragraph":
                    Paragraph paragraph = new Paragraph();

                    bool HasInlines = !reader.IsEmptyElement;

                    ReadAttributes(paragraph);

                    if (HasInlines)
                    {
                        while (reader.Read() && reader.NodeType == XmlNodeType.Element)
                        {
                            ReadInline(paragraph.Inlines);
                        }
                    }

                    collection.Add(paragraph);

                    break;
                default:
                    break;
            }
        }

        private void ReadInline(InlineCollection collection)
        {
            switch (reader.Name)
            {
                case "Run":
                    Run run = new Run();

                    ReadAttributes(run);
                    
                    //Если есть сложные свойства, читаем сначала их
                    while (reader.Read() && reader.NodeType == XmlNodeType.Element)
                    {
                        string propertyname = reader.Name.Split('.')[1];

                        if (!reader.IsEmptyElement)
                        {
                            switch (propertyname)
                            {
                                case "TextDecorations":

                                    while (reader.Read() && reader.Name != "Run." + propertyname)
                                    {
                                        switch (reader.Name)
                                        {
                                            case "TextDecoration":

                                                TextDecoration td = new TextDecoration();
                                                ReadAttributes(td);

                                                run.TextDecorations.Add(td);

                                                break;

                                            default:
                                                reader.Skip();

                                                break;
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                    //В конце концов должен быть текст
                    if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.SignificantWhitespace)
                        run.Text = reader.ReadString();

                    collection.Add(run);
                    break;

                case "Span":
                    Span span = new Span();

                    ReadAttributes(span);

                    //Если есть сложные свойства, читаем сначала их
                    while (reader.Read() && reader.NodeType == XmlNodeType.Element)
                    {
                        ReadInline(span.Inlines);
                    }

                    collection.Add(span);
                    break;

                case "Image":
                    Image image = new Image();

                    ReadAttributes(image);

                    BinarySource imageSource = BinaryCacheManager.GetCustomValue(image);

                    byte[] imageData;

                    if (BinaryCacheManager.BinaryCache.TryGetValue(imageSource.BinarySourceId, out imageData))
                    {
                        imageSource.BinarySourceData = imageData;
                        image.Source = imageSource.ToImageSource();

                        collection.Add(new InlineUIContainer(image));

                    }
                    else
                    {
                        Border imageDownloadingBanner = new Border() { CornerRadius = new CornerRadius(5.0), BorderThickness = new Thickness(1) };

                        imageDownloadingBanner.Background = new SolidColorBrush(Colors.BlanchedAlmond);

                        imageDownloadingBanner.Width = image.Width;
                        imageDownloadingBanner.Height = image.Height;

                        BusyIndicator busyIndicator = new BusyIndicator();

                        //busyIndicator.MaxHeight = 25;
                        //busyIndicator.MaxWidth = 25;

                        busyIndicator.MinHeight = 25;
                        busyIndicator.MinWidth = 25;

                        busyIndicator.VerticalAlignment = VerticalAlignment.Center;
                        busyIndicator.HorizontalAlignment = HorizontalAlignment.Center;

                        imageDownloadingBanner.Child = busyIndicator;
                        collection.Add(new InlineUIContainer(imageDownloadingBanner));
                    }


                    break;

                default:
                    break;
            }
        }


        private void ReadAttributes(DependencyObject element)
        {

            while (reader.MoveToNextAttribute())
            {
                if (reader.Name == "xml:space")
                    continue;

                SerializableProperty property = propertiesOfType[element.GetType()][reader.Name];

                    element.SetValue(property.Property, property.Converter.ConvertFromInvariantString(reader.Value));
            }

            
        }
    }
}
