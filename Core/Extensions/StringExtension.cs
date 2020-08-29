using System;
using System.Drawing;

namespace Core.Extensions
{
    public static class StringExtension
    {
        public static int CalculateWidthToPixels(this string str, int pxFontSize)
        {
            SizeF size;
            using (var graphics = Graphics.FromImage(new Bitmap(1,1)))
            {
                size = graphics.MeasureString(str, new Font("Arial", pxFontSize, FontStyle.Regular, GraphicsUnit.Pixel));
            }
            return (int) size.Width;
        }
        public static int CalculateHeightToPixels(this string str, int pxFontSize, double lineHeight)
        {
            int lineCount = str.Split(new string[] { "<br/>", "\n" }, StringSplitOptions.None).Length;
            return (int)Math.Round(pxFontSize * lineHeight * lineCount); //css line-height: 1.1
        }
    }
}
