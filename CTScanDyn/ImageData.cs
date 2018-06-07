using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Util;

namespace CTScanDyn
{
    class ImageData : IDisposable
    {
        public Mat Descriptors { get; set; }
        public Mat CvMaterial { get; set; }
        public Mat CvOriginal { get; set; }
        public VectorOfKeyPoint KeyPoints { get; set; }

        public int Height { get; set; }
        public int Width { get; set; }
        public Size Size { get { return new Size(Width, Height); } }
        
        public bool IsDisposed { get; set; }

        public ImageData(Mat cvOriginal, Mat cvMaterial)
        {
            CvOriginal = cvOriginal;
            CvMaterial = cvMaterial;
            IsDisposed = false;
        }

        public static Image Subtract(Bitmap image1, Bitmap image2)
        {
            Bitmap res = new Bitmap(image1.Width, image1.Height);
            for (int y = 0; y < image1.Height; y++)
            {
                for (int x = 0; x < image1.Width; x++)
                {
                    Color c1 = image1.GetPixel(x, y);
                    Color c2 = image2.GetPixel(x, y);
                    res.SetPixel(x, y, SubtractPixel(c1,c2));
                }
            }
            return res;
        }

        private static Color SubtractPixel(Color c1, Color c2)
        {
            if (c1.R - c2.R < -100 || c1.G - c2.G < -100 || c1.B - c2.B < -100)
                return Color.Red;
            if (c1.R - c2.R > 100 && c1.G - c2.G > 100 && c1.B - c2.B > 100)
                return Color.Green;
                return c2;

        }

        #region IDisposable Members

        ~ImageData()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            CvOriginal = null;
            CvMaterial = null;
            IsDisposed = true;
        }

        #endregion
    }
}
