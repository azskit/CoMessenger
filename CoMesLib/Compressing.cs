using System.IO;
using System.IO.Compression;

namespace CorporateMessengerLibrary
{
    public static class Compressing
    {
        public static byte[] Compress(byte[] data)
        {
            if (data == null)
                throw new System.ArgumentNullException("data");

            using (MemoryStream output = new MemoryStream())
            {
                using (DeflateStream dstream = new DeflateStream(output, CompressionMode.Compress))
                {
                    dstream.Write(data, 0, data.Length);
                }
                return output.ToArray();
            }
        }

        public static byte[] Decompress(byte[] data)
        {
            using (MemoryStream input = new MemoryStream(data))
            using (MemoryStream output = new MemoryStream())
            using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
                return output.ToArray();
            }
        }
    }
}
