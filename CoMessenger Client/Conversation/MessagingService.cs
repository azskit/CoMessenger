using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CorporateMessengerLibrary;
using System.Windows.Documents;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;
using COMessengerClient.CustomControls;
using COMessengerClient.CustomControls.CustomConverters;
using System.Windows.Markup;
using System.ComponentModel;
//using System.Drawing;
using COMessengerClient.Tools;
using CorporateMessengerLibrary.Messaging;
using CorporateMessengerLibrary.History;
using CorporateMessengerLibrary.Tools;

namespace COMessengerClient.Conversation
{
    //public static class ListBinarySearch
    //{




    //}

    //static class EditorHelper
    //{
        //public static void Register<T, TC>()
        //{
        //    Attribute[] attr = new Attribute[1];
        //    TypeConverterAttribute vConv = new TypeConverterAttribute(typeof(TC));
        //    attr[0] = vConv;
        //    var i = TypeDescriptor.AddAttributes(typeof(T), attr);

           

            
        //}
        //public static void RegisterVS<T, TC>()
        //{
        //    Attribute[] attr = new Attribute[1];
        //    ValueSerializerAttribute vConv = new ValueSerializerAttribute(typeof(TC));
        //    attr[0] = vConv;
        //    var i = TypeDescriptor.AddAttributes(typeof(T), attr);




        //}

    //}

    //class  CustomImageValueSerializer : ImageSourceValueSerializer
    //{
    //    public override bool CanConvertToString(object value, IValueSerializerContext context)
    //    {
            
    //        if (value is AnimatedImage)
    //            return false;
    //        else if (value is System.Windows.Controls.Image)
    //            return true;
    //        return base.CanConvertToString(value, context);
    //    }

    //    public override string ConvertToString(object value, IValueSerializerContext context)
    //    {

    //        return base.ConvertToString(value, context);
    //    }
    //}


    //class CustomImageConvertor : TypeConverter
    //{
    //    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    //    {
    //        if (destinationType == typeof(MarkupExtension))
    //        {

    //            return true;
    //        }
    //        else return false;
    //    }
    //    public override object ConvertTo(ITypeDescriptorContext context,
    //                    System.Globalization.CultureInfo culture, object value, Type destinationType)
    //    {
    //        if (destinationType == typeof(MarkupExtension))
    //        {
    //            BindingExpression bindingExpression = value as BindingExpression;
    //            if (bindingExpression == null)
    //                throw new Exception();
    //            return bindingExpression.ParentBinding;
    //        }

    //        return base.ConvertTo(context, culture, value, destinationType);
    //    }
    //}



    public static class MessagingService
    {
        //private static TaskFactory factory = new TaskFactory();

        //private ConversationModel() { }

        internal static T FindEqualOrAbove<T>(this List<T> theList, T key, IComparer<T> comparer)
        {
            if (theList == null)
                throw new ArgumentNullException("theList");

            if (theList.Count == 0) return default(T);
            //if (theList.Count == 1) return theList[0];

            int idx = theList.BinarySearch(key, comparer);

            if (idx >= 0)
                return theList[idx];
            else
            {
                if (-(idx) > theList.Count)
                    return default(T);
                else
                    return theList[-(idx + 1)];
            }
        }

        internal static int? FindEqualOrAboveIndex<T>(this List<T> theList, T key, IComparer<T> comparer)
        {
            if (theList == null)
                throw new ArgumentNullException("theList");

            if (theList.Count == 0) return null;
            //if (theList.Count == 1) return theList[0];

            int idx = theList.BinarySearch(key, comparer);

            if (idx >= 0)
                return idx;
            else
            {
                if (-(idx) > theList.Count)
                    return null;
                else
                    return -(idx + 1);
            }
        }


        internal static BlockCollection ExtractBlocks(MessageValue value)
        {
            FlowDocument fldoc = new FlowDocument();

            //Костыль - если в редакторе указать такие же параметры тексту, как и по умолчанию, то он их не применит
            // поэтому указываем "нереальные" параметры по умолчанию (дерьмо, но ниче лучше не придумал :( )
            //fldoc.FontSize = 1; 
            //fldoc.Foreground = new SolidColorBrush(Colors.Transparent);

            TextRange tr = new TextRange(fldoc.ContentEnd, fldoc.ContentEnd);

            switch (value.Kind)
            {
                case RoutedMessageKind.RichText:

                    //using (MemoryStream stream = new MemoryStream(Compressing.Decompress(value.Body)))
                    //{
                    //    tr.Load(stream, DataFormats.XamlPackage);
                    //}


                    //using (MemoryStream stream = new MemoryStream(Compressing.Decompress(value.Body)))
                    //using (System.Xaml.XamlReader reader =                     {
                    //    FlowDocument deserialized = System.Windows.Markup.XamlReader.Load as FlowDocument;
                    //}

                    CimSerializer serializer = new CimSerializer();
                    FlowDocument secondFlDoc = null;

                    List<FrameworkElement> images;

                    using (MemoryStream stream = new MemoryStream(Compressing.Decompress(value.FormattedText)))
                    {
                        secondFlDoc = serializer.Deserialize(stream, out images);
                    }

                    foreach (Image image in images)
                    {
                        BinarySource imageSource = BinaryCacheManager.GetCustomValue(image);

                        byte[] imageData;

                        imageData = App.ThisApp.History.RestoreBinary(imageSource.BinarySourceId);

                        if (imageData != null)
                        {
                            imageSource.BinarySourceData = imageData;

                            AnimatedImage animated = image as AnimatedImage;

                            if (animated != null)
                                animated.AnimatedBitmap = imageSource.ToBitmap();
                            else
                                image.Source = imageSource.ToImageSource();
                        }
                        else
                        {

                            ImageDownloadingBanner banner = new ImageDownloadingBanner(image);

                            {
                                InlineUIContainer container = image.Parent as InlineUIContainer;
                                if (container != null)
                                {
                                    container.Child = banner;
                                }
                            }

                            {
                                BlockUIContainer container = image.Parent as BlockUIContainer;
                                if (container != null)
                                {
                                    container.Child = banner;
                                }
                            }

                            banner.Reload();
                        }
                    }


                        return secondFlDoc.Blocks;



                    //using (StringReader stringReader = new StringReader(Encoding.UTF8.GetString(Compressing.Decompress(value.FormattedText))))
                    //using (System.Xml.XmlReader xmlReader = System.Xml.XmlReader.Create(stringReader))
                    //{
                    //    try
                    //    {
                    //        fldoc = XamlReader.Load(xmlReader) as FlowDocument;

                    //    }
                    //    catch (XamlParseException)
                    //    {
                    //        //tr.Text = value.Text ?? "";
                    //        //fldoc.Blocks.FirstBlock.ToolTip = "ОШИБКА ЧТЕНИЯ ФОРМАТА СООБЩЕНИЯ"; 
                    //        fldoc.Blocks.Add(new Paragraph(new Run(value.Text ?? "")) { ToolTip = "ОШИБКА ЧТЕНИЯ ФОРМАТА СООБЩЕНИЯ" });
                    //    }
                    //}                   
                    //return deserialized.Blocks;
                    //break;
                case RoutedMessageKind.Plaintext:
                    tr.Text = value.Text ?? "";
                    break;
            }
            return fldoc.Blocks;
        }








        internal static void Decompose(FlowDocument source, out string format, out List<BinarySource> binaries)
        {
            CimSerializer serializer = new CimSerializer();

            format = serializer.Serialize2(source, out binaries);

            //using (FileStream fstream = new FileStream("debug_message2.txt", FileMode.Create))
            //{
            //    fstream.Write(Encoding.UTF8.GetBytes(outstr.ToString()), 0, outstr.Length);
            //}
            using (FileStream fstream = new FileStream("debug_message3.txt", FileMode.Create))
            {
                fstream.Write(Encoding.UTF8.GetBytes(format), 0, Encoding.UTF8.GetBytes(format).Length);
            }


        }

        internal static byte[] GetMessageBody(FlowDocument source)
        {
            FlowDocument fldoc = new FlowDocument();

            //Костыль - если в редакторе указать такие же параметры тексту, как и по умолчанию, то он их не применит
            // поэтому указываем "нереальные" параметры по умолчанию (дерьмо, но ниче лучше не придумал :( )
            fldoc.FontSize = 1;
            fldoc.Foreground = new SolidColorBrush(Colors.Transparent);

            Block[] newblocks = new Block[source.Blocks.Count];

            source.Blocks.CopyTo(newblocks, 0);

            fldoc.Blocks.AddRange(newblocks);

            //TextRange range = new TextRange(fldoc.ContentStart, fldoc.ContentEnd);

            string savedButton = System.Windows.Markup.XamlWriter.Save(fldoc);

            //var c = ValueSerializer.GetSerializerFor(typeof(System.Windows.Controls.Image));

            //var m = MarkupWriter.GetMarkupObjectFor(fldoc);

            //StringBuilder outstr = new StringBuilder();
            //XmlWriterSettings settings = new XmlWriterSettings();
            //settings.Indent = true;
            //settings.OmitXmlDeclaration = true;
            //XamlDesignerSerializationManager dsm = new XamlDesignerSerializationManager(XmlWriter.Create(outstr, settings));
            ////this string need for turning on expression saving mode 
            //dsm.XamlWriterMode = XamlWriterMode.Value;
            //XamlWriter.Save(fldoc, dsm);

            CimSerializer serializer = new CimSerializer();

            //string xml = serializer.Serialize(fldoc);

            //using (FileStream fstream = new FileStream("debug_message2.txt", FileMode.Create))
            //{
            //    fstream.Write(Encoding.UTF8.GetBytes(outstr.ToString()), 0, outstr.Length);
            //}
            //using (FileStream fstream = new FileStream("debug_message2.txt", FileMode.Create))
            //{
            //    fstream.Write(Encoding.UTF8.GetBytes(xml), 0, Encoding.UTF8.GetBytes(xml).Length);
            //}
            //serializer = new CimSerializer();

            List<BinarySource> binaries;

            string xml = serializer.Serialize2(fldoc, out binaries);

            //using (FileStream fstream = new FileStream("debug_message2.txt", FileMode.Create))
            //{
            //    fstream.Write(Encoding.UTF8.GetBytes(outstr.ToString()), 0, outstr.Length);
            //}
            using (FileStream fstream = new FileStream("debug_message3.txt", FileMode.Create))
            {
                fstream.Write(Encoding.UTF8.GetBytes(xml), 0, Encoding.UTF8.GetBytes(xml).Length);
            }






            using (FileStream fstream = new FileStream("debug_message.txt", FileMode.Create))
            {
                fstream.Write(Encoding.UTF8.GetBytes(savedButton), 0, Encoding.UTF8.GetBytes(savedButton).Length);
            }

            //fldoc.Blocks.ForEach(
            //            isRecursive: true,
            //            action: (blc) =>
            //            {
            //                BlockUIContainer imageContainer;

            //                imageContainer = blc as BlockUIContainer;

            //                if (imageContainer != null)
            //                {
            //                    Image img = imageContainer.Child as Image;

            //                    if (img != null)
            //                    {
            //                        img.Loaded += (a, b) =>
            //                        {
            //                            img.SnapToPixels();
            //                        };


            //                        Viewbox imageBox = new Viewbox();

            //                        imageContainer.Child = imageBox;

            //                        imageBox.Child = img;
            //                        imageBox.HorizontalAlignment = img.HorizontalAlignment;

            //                        img.UseLayoutRounding = true;
            //                        img.SnapsToDevicePixels = true;

            //                        imageBox.MaxWidth = img.Width * 1 / App.DpiXScalingFactor;

            //                        //imageBox.MouseDown += (a, b) => { MessageBox.Show("SnapsToDevicePixels: " + tmp.SnapsToDevicePixels + " UseLayoutRounding: " + tmp.UseLayoutRounding + " X: " + tmp.PointToScreen(new Point(0d, 0d)).X + "Y: " + tmp.PointToScreen(new Point(0d, 0d)).Y); };

            //                        //imageContainer.Child.RenderTransform = new ScaleTransform(1 / App.DpiXScalingFactor, 1 / App.DpiYScalingFactor);
            //                        //imageContainer.Child = new Button() { Content = "hi there" };
            //                    }
            //                }
            //            });

            //return Compressing.Compress(Encoding.UTF8.GetBytes(savedButton));
            return Compressing.Compress(Encoding.UTF8.GetBytes(xml));


            //using (MemoryStream streamXAML = new MemoryStream())
            //{
            //    range.Save(streamXAML, DataFormats.XamlPackage, true);
            //    streamXAML.Position = 0;

            //    BinaryReader reader = new BinaryReader(streamXAML);
            //    //using (BinaryReader reader = new BinaryReader(streamXAML))
            //    //{
            //        return Compressing.Compress(reader.ReadBytes((int)streamXAML.Length));
            //    //}
            //}

        }







    }
}
