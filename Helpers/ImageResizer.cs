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

        public static (int, int) GetResolution(int comboboxIndex)
        {
            return comboboxIndex switch
            {
                1 => (640, 480),
                2 => (1280, 1024),
                _ => (0, 0),
            };
        }

        public static int GetFontSize(int comboboxIndex)
        {
            return comboboxIndex switch
            {
                1 => 10,
                2 => 12,
                3 => 14,
                4 => 16,
                5 => 18,
                6 => 22,
                _ => 14
            } ;
        }
    }
}