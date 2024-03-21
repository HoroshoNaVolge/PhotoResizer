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

        public static (int, int) GetResolution(int index)
        {
            return index switch
            {
                0 => (640, 480),
                1 => (800, 600),
                2 => (1024, 768),
                3 => (1280, 1024),
                _ => (640, 480),
            };
        }

        public static int GetFontSize(int index)
        {
            return index switch
            {
                0 => 10,
                1 => 12,
                2 => 14,
                3 => 16,
                4 => 18,
                5 => 22,
                _ => 14
            };
        }
    }
}