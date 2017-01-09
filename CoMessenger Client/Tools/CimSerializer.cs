using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup.Primitives;
using System.Windows.Media;
using System.Xml;

namespace COMessengerClient.Tools
{
    internal class CimSerializer
    {
        private XmlWriter writer;
        private StringBuilder sb;

        public CimSerializer()
        {
            sb = new StringBuilder();
            writer = XmlWriter.Create(sb , new XmlWriterSettings() { Indent = true, OmitXmlDeclaration = true });
        }

        internal string Serialize(FlowDocument document)
        {
            var markup = MarkupWriter.GetMarkupObjectFor(document);

            //Scope scope = new Scope(null);
            //scope.RecordMapping("", NamespaceCache.GetNamespaceUriFor(item.ObjectType));

            //string uri = scope.MakeAddressable(markup.ObjectType);

            writer.WriteStartElement(markup.ObjectType.Name);
            //writer.Wri

            foreach (Block property in document.Blocks)
            {
                writer.WriteStartElement(property.GetType().Name);

                Paragraph paragraph = property as Paragraph;

                if (paragraph != null)
                {
                    foreach (Inline inline in paragraph.Inlines)

                    {
                        Run run = inline as Run;

                        if (run != null)
                        {
                            bool plainText = true;


                            foreach (DependencyProperty dp in new List<DependencyProperty>()
                            {
                                TextElement.FontFamilyProperty,
                                TextElement.FontSizeProperty,
                                TextElement.FontStretchProperty,
                                TextElement.FontStyleProperty,
                                TextElement.FontWeightProperty,
                                TextElement.ForegroundProperty,
                                TextElement.BackgroundProperty,

                            })
                            {
                                object value = run.GetValue(dp);

                                if (value != run.Parent.GetValue(dp))
                                {
                                    if (plainText)
                                    {
                                        writer.WriteStartElement("Run");
                                        plainText = false;
                                    }

                                    if (value is IEnumerable<object>)
                                    {
                                    }
                                    else
                                        writer.WriteAttributeString(dp.Name, value.ToString());
                                }
                            }

                            TextEffectCollection textEffects = run.GetValue(TextElement.TextEffectsProperty) as TextEffectCollection;
                            if (textEffects != null)
                            {
                                if (plainText)
                                {
                                    writer.WriteStartElement("Run");
                                    plainText = false;
                                }

                                writer.WriteStartElement("Run." + TextElement.TextEffectsProperty.Name);
                                foreach (var val in textEffects)
                                {
                                    writer.WriteStartElement(val.GetType().Name);
                                    writer.WriteAttributeString(TextEffect.TransformProperty.Name, val.GetValue(TextEffect.TransformProperty).ToString());
                                    writer.WriteEndElement();
                                }
                                writer.WriteEndElement();
                            }
                            TextDecorationCollection textDecorations = run.GetValue(Inline.TextDecorationsProperty) as TextDecorationCollection;

                            if (!plainText)
                                if (run.Text.EndsWith(" ") || run.Text.StartsWith(" "))
                                    writer.WriteAttributeString("xml","space", "", "preserve");


                            writer.WriteString(run.Text);

                            if (!plainText)
                                writer.WriteEndElement();
                        }
                    }
                }

                //foreach (var prop in property.GetType().GetProperties())
                //{
                //    App.Log.Add(
                //property.GetType().Name,
                //prop.Name + " " + prop.GetValue(property, null)
                //);
                //}
                writer.WriteEndElement();
            }


                writer.WriteEndElement();
            writer.Flush();
            return sb.ToString();


        }

    }
}
