using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO;

namespace CorporateMessengerLibrary
{
    /*
    [Serializable]
    public class SerializableImage : ISerializable
    {
        public SerializableImage() { }


        protected SerializableImage(SerializationInfo info, StreamingContext context)
        {
            SerializedImage = (byte[])info.GetValue("SerializedImage", typeof(byte[]));
        }


        public byte[] SerializedImage { get; set; }

        [NonSerialized]
        public BitmapImage image;

        //public Image Image { get; set; }


        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("SerializedImage", SerializedImage);
        }


        [OnSerializing]
        private void OnSerializing(StreamingContext sc)
        {
            //BitmapImage image = Image.Source as BitmapImage;
            if (image != null)
            {
                MemoryStream stream = new MemoryStream();
                BmpBitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(stream);

                SerializedImage = stream.ToArray();

                stream.Close(); ;
            }
        }


        [OnDeserialized]
        private void OnDeserialized(StreamingContext sc)
        {
            if (SerializedImage != null)
            {
                using (MemoryStream stream = new MemoryStream(SerializedImage))
                {
                    //Image = new Image
                    //{
                    //    Source = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad)
                    //};

                    image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = stream;
                    image.EndInit();


                    //image = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad)

                    //stream.Close();
                }
            }
        }
    }
    */
}
