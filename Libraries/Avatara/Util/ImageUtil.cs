using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;

namespace KeplerCMS.Avatara.Util
{
    public class ImageUtil
    {
        public static Image<Rgba32> Trim(Image<Rgba32> image)
        {
            int w = image.Width;
            int h = image.Height;

            Func<int, bool> allWhiteRow = r =>
            {
                for (int i = 0; i < w; ++i)
                    if ((image[i, r].A != 0))
                        return false;
                return true;
            };

            Func<int, bool> allWhiteColumn = c =>
            {
                for (int i = 0; i < h; ++i)
                    if ((image[c, i].A != 0))
                        return false;
                return true;
            };

            int topmost = 0;
            for (int row = 0; row < h; ++row)
            {
                if (!allWhiteRow(row))
                    break;
                topmost = row;
            }

            int bottommost = 0;
            for (int row = h - 1; row >= 0; --row)
            {
                if (!allWhiteRow(row))
                    break;
                bottommost = row;
            }

            int leftmost = 0, rightmost = 0;
            for (int col = 0; col < w; ++col)
            {
                if (!allWhiteColumn(col))
                    break;
                leftmost = col;
            }

            for (int col = w - 1; col >= 0; --col)
            {
                if (!allWhiteColumn(col))
                    break;
                rightmost = col;
            }

            if (rightmost == 0) rightmost = w; // As reached left
            if (bottommost == 0) bottommost = h; // As reached top.

            int croppedWidth = rightmost - leftmost;
            int croppedHeight = bottommost - topmost;

            if (croppedWidth == 0) // No border on left or right
            {
                leftmost = 0;
                croppedWidth = w;
            }

            if (croppedHeight == 0) // No border on top or bottom
            {
                topmost = 0;
                croppedHeight = h;
            }

            try
            {
                return image.Clone(ctx => ctx.Crop(new Rectangle(leftmost, topmost, croppedWidth, croppedHeight)));
            }
            catch (Exception ex)
            {
                throw new Exception(
                  string.Format("Values are topmost={0} btm={1} left={2} right={3} croppedWidth={4} croppedHeight={5}", topmost, bottommost, leftmost, rightmost, croppedWidth, croppedHeight),
                  ex);
            }
        }

        public static Image<Rgba32> Trim(Image<Rgba32> image, params Rgba32[] colors)
        {
            int w = image.Width;
            int h = image.Height;


            int topmost = 0;
            for (int row = 0; row < h; ++row)
            {
                if (!allWhiteRow(image, row, colors))
                    break;
                topmost = row;
            }

            int bottommost = 0;
            for (int row = h - 1; row >= 0; --row)
            {
                if (!allWhiteRow(image, row, colors))
                    break;
                bottommost = row;
            }

            int leftmost = 0, rightmost = 0;
            for (int col = 0; col < w; ++col)
            {
                if (!allWhiteCol(image, col, colors))
                    break;
                leftmost = col;
            }

            for (int col = w - 1; col >= 0; --col)
            {
                if (!allWhiteCol(image, col, colors))
                    break;
                rightmost = col;
            }

            if (rightmost == 0) rightmost = w; // As reached left
            if (bottommost == 0) bottommost = h; // As reached top.

            int croppedWidth = rightmost - leftmost;
            int croppedHeight = bottommost - topmost;

            if (croppedWidth == 0) // No border on left or right
            {
                leftmost = 0;
                croppedWidth = w;
            }

            if (croppedHeight == 0) // No border on top or bottom
            {
                topmost = 0;
                croppedHeight = h;
            }

            try
            {
                return image.Clone(ctx => ctx.Crop(new Rectangle(leftmost, topmost, croppedWidth, croppedHeight)));
            }
            catch (Exception ex)
            {
                throw new Exception(
                  string.Format("Values are topmost={0} btm={1} left={2} right={3} croppedWidth={4} croppedHeight={5}", topmost, bottommost, leftmost, rightmost, croppedWidth, croppedHeight),
                  ex);
            }
        }

        private static bool allWhiteRow(Image<Rgba32> image, int r, params Rgba32[] colors)
        {
            int w = image.Width;

            for (int i = 0; i < w; ++i)
            {
                var pixel = image[i, r];

                if (!ColorEquals(colors, pixel))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool allWhiteCol(Image<Rgba32> image, int c, params Rgba32[] colors)
        {
            int h = image.Height;

            for (int i = 0; i < h; ++i)
            {
                var pixel = image[c, i];

                if (!ColorEquals(colors, pixel))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ColorEquals(Rgba32[] colors, Rgba32 pixel)
        {
            foreach (var color in colors)
            {
                if (pixel.R == color.R && pixel.G == color.G && pixel.B == color.B && pixel.A == color.A)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
