using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoPreparation.Helpers
{
    public static class ImageResizer
    {
        public static Bitmap ResizeImage(Image image, int maxWidth, int maxHeight)
        {
            var ratio = Math.Min((double)maxWidth / image.Width, (double)maxHeight / image.Height);
            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            var newImage = new Bitmap(newWidth, newHeight);
            using var g = Graphics.FromImage(newImage);
            g.DrawImage(image, 0, 0, newWidth, newHeight);

            return newImage;
        }
    }
}