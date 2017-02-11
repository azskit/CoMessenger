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
using CorporateMessengerLibrary;
using CorporateMessengerLibrary.Tools;

namespace COMessengerClient.Tools
{


    internal class BinaryCacheManager : DependencyObject
    {

        //public static Dictionary<string, byte[]> BinaryCache = new Dictionary<string, byte[]>();

        internal static readonly DependencyProperty BinarySourceProperty = DependencyProperty.RegisterAttached(
            "BinarySource", typeof(BinarySource), typeof(Image), new PropertyMetadata(null));

        internal static void SetCustomValue(DependencyObject element, BinarySource value)
        {
            element.SetValue(BinarySourceProperty, value);
        }

        internal static BinarySource GetCustomValue(DependencyObject element)
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

        internal static BinarySource CreateFromImage(Image image)
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

            newBinarySource.BinarySourceId = Sha1Helper.GetHash(newBinarySource.BinarySourceData);

            return newBinarySource;
        }


        internal static Dictionary<string, BitmapImage> ImageSourceCache = new Dictionary<string, BitmapImage>();

        internal ImageSource ToImageSource()
        {
            BitmapImage newBitmaImage;

            if (!ImageSourceCache.TryGetValue(BinarySourceId, out newBitmaImage))
            {
                newBitmaImage = new BitmapImage();
                using (MemoryStream stream = new MemoryStream(BinarySourceData))
                {

                    newBitmaImage.BeginInit();

                    newBitmaImage.StreamSource = stream;

                    newBitmaImage.CacheOption = BitmapCacheOption.OnLoad;

                    newBitmaImage.EndInit();
                }
                ImageSourceCache.Add(BinarySourceId, newBitmaImage);
            }
            return newBitmaImage;
        }

        internal static BinarySource CreateFromAnimatedImage(AnimatedImage animatedImage)
        {
            BinarySource newBinarySource = new BinarySource();


            using (MemoryStream ms = new MemoryStream())
            {
                animatedImage.AnimatedBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
                ms.Position = 0;
                newBinarySource.BinarySourceData = new byte[ms.Length];
                ms.Read(newBinarySource.BinarySourceData, 0, newBinarySource.BinarySourceData.Length);
            }

            newBinarySource.BinarySourceId = Sha1Helper.GetHash(newBinarySource.BinarySourceData);

            return newBinarySource;
        }


        internal static Dictionary<string, System.Drawing.Bitmap> BitmapCache = new Dictionary<string, System.Drawing.Bitmap>();

        internal System.Drawing.Bitmap ToBitmap()
        {
            System.Drawing.Bitmap retval = null;

            if (!BitmapCache.TryGetValue(BinarySourceId, out retval))
            {
                retval = System.Drawing.Image.FromStream(new MemoryStream(BinarySourceData)) as System.Drawing.Bitmap;
                BitmapCache.Add(BinarySourceId, retval);
            }
            return retval;
        }
    }


    internal class CimSerializer
    {


        private XmlWriter writer;
        private StringBuilder sb;
        private List<BinarySource> binaries;

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
                    new SerializableProperty(Image.HorizontalAlignmentProperty),
                    new SerializableProperty(BinaryCacheManager.BinarySourceProperty)
                }.ToDictionary(s => s.Property.Name));
            #endregion

            #region AnimatedImage
            propertiesOfType.Add(

                key: typeof(AnimatedImage),

                value: new List<SerializableProperty>()
                {
                    new SerializableProperty(AnimatedImage.WidthProperty  ),
                    new SerializableProperty(AnimatedImage.HeightProperty ),
                    new SerializableProperty(AnimatedImage.StretchProperty),
                    new SerializableProperty(AnimatedImage.HorizontalAlignmentProperty),
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

        }

        public CimSerializer()
        {
            sb = new StringBuilder();
            writer = XmlWriter.Create(sb , new XmlWriterSettings() {  Indent = false, OmitXmlDeclaration = true });

            binaries = new List<BinarySource>();
            controlsToLoadBinary = new List<FrameworkElement>();


            initPropertiesOfType();
        }

        #region Serialize Methods

        //internal string Serialize(FlowDocument document)
        //{
        //    MarkupObject markup = MarkupWriter.GetMarkupObjectFor(document);

        //    WriteMarkup(markup);

        //    writer.Flush();
        //    return sb.ToString();
        //}

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

            if (content != null && (content.StringValue.EndsWith(" ", StringComparison.OrdinalIgnoreCase) || content.StringValue.StartsWith(" ", StringComparison.OrdinalIgnoreCase)))
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

        internal string Serialize2(FlowDocument document, out List<BinarySource> newBinaries)
        {
            binaries.Clear();

            //var markup = MarkupWriter.GetMarkupObjectFor(document);

            //foreach (var item in markup.Properties)
            //{
            //    Console.WriteLine(item.StringValue);

            //    if (item.Value is IXmlSerializable)
            //    {
            //        var i = 1;
            //    }
            //}



            writer.WriteStartElement("FlowDocument");

            foreach (Block property in document.Blocks)
            {
                WriteBlock(property);
            }

            newBinaries = new List<BinarySource>(binaries);

            writer.WriteEndElement();
            writer.Flush();
            return sb.ToString();


        }

        private void WriteBlock(Block property)
        {
            //writer.WriteStartElement(property.GetType().Name);

            #region Paragraph
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

            #endregion

            #region Table
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

            #endregion


            #region BlockUIContainer
            BlockUIContainer blockUIContainer = property as BlockUIContainer;

            if (blockUIContainer != null)
            {
                WriteUIPart(blockUIContainer.Child);
            }

            #endregion
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
                if (run.Text.EndsWith(" ", StringComparison.OrdinalIgnoreCase) || run.Text.StartsWith(" ", StringComparison.OrdinalIgnoreCase))
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

                return;
            }

            Run run = inline as Run;

            if (run != null)
            {

                writer.WriteStartElement("Run");
                WriteAttributes(run, typeof(Run));


                writer.WriteString(run.Text);

                writer.WriteEndElement();

                return;
            }

            InlineUIContainer inlineUiContainer = inline as InlineUIContainer;

            if (inlineUiContainer != null)
            {
                WriteUIPart(inlineUiContainer.Child);
            }
        } 

        private void WriteUIPart(UIElement element)
        {

            AnimatedImage animatedImage = element as AnimatedImage;

            if (animatedImage != null)
            {
                BinarySource imageSource = BinarySource.CreateFromAnimatedImage(animatedImage);

                BinaryCacheManager.SetCustomValue(animatedImage, imageSource);

                writer.WriteStartElement("AnimatedImage");
                WriteAttributes(animatedImage, typeof(AnimatedImage));

                binaries.Add(imageSource);

                writer.WriteEndElement();

                return;
            }

            Image image = element as Image;

            if (image != null)
            {
                BinarySource imageSource = BinarySource.CreateFromImage(image);

                BinaryCacheManager.SetCustomValue(image, imageSource);

                writer.WriteStartElement("Image");
                WriteAttributes(image, typeof(Image));


                //if (!BinaryCacheManager.BinaryCache.ContainsKey(imageSource.BinarySourceId))
                //    BinaryCacheManager.BinaryCache.Add(imageSource.BinarySourceId, imageSource.BinarySourceData);

                binaries.Add(imageSource);

                //writer.WriteAttributeString("BinarySource", binaryDataId);

                writer.WriteEndElement();

                return;
            }

            Viewbox viewbox = element as Viewbox;
            if (viewbox != null)
                WriteUIPart(viewbox.Child);
        }


        #endregion



        private XmlReader reader;
        private List<FrameworkElement> controlsToLoadBinary;
        //FrameworkContentElement currentElement;

        public FlowDocument Deserialize(Stream stream, out List<FrameworkElement> needLoadBinary)
        {
            controlsToLoadBinary.Clear();

            reader = XmlReader.Create(stream);

            reader.ReadStartElement("FlowDocument");

            FlowDocument fldoc = new FlowDocument();

            do
            {
                ReadBlock(fldoc.Blocks);
            } while (reader.Read());

            needLoadBinary = new List<FrameworkElement>(controlsToLoadBinary);

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


                case "Image":
                    Image image = new Image();

                    ReadAttributes(image);

                    controlsToLoadBinary.Add(image);

                    collection.Add(new BlockUIContainer(image));

                    break;

                case "AnimatedImage":
                    AnimatedImage animatedImage = new AnimatedImage();

                    ReadAttributes(animatedImage);

                    controlsToLoadBinary.Add(animatedImage);

                    collection.Add(new BlockUIContainer(animatedImage));

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

                    controlsToLoadBinary.Add(image);

                    collection.Add(new InlineUIContainer(image));

                    break;

                case "AnimatedImage":
                    AnimatedImage animatedImage = new AnimatedImage();

                    ReadAttributes(animatedImage);

                    controlsToLoadBinary.Add(animatedImage);

                    collection.Add(new InlineUIContainer(animatedImage));

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
