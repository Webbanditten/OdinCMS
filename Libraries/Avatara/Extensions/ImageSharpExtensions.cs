using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;

namespace KeplerCMS.Avatara.Extensions
{
    public static class ImageSharpExtensions
    {
        public static byte[] ToByteArray<TPixel>(this Image<TPixel> image) where TPixel : unmanaged, IPixel<TPixel>
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.SaveAsPng(ms);

                return ms.ToArray();
            }
        }
    }
}
