﻿using System;
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
        public Bitmap Source = null;
        public Bitmap Bones = null;

        public int Height { get; set; }
        public int Width { get; set; }
        public Size Size { get { return new Size(Width, Height); } }

        public int Depth { get; set; }
        public bool IsDisposed { get; set; }
        public Mat Descriptors { get; set; }
        public Mat CvMaterial { get; set; }
        public VectorOfKeyPoint KeyPoints { get; set; }

        public ImageData(Bitmap source, Bitmap bones)
        {
            this.Source = source;
            this.Bones = bones;
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
                    res.SetPixel(x, y, SubtractPixel(c2,c1));
                }
            }
            return res;
        }

        private static Color SubtractPixel(Color c1, Color c2)
        {
            //Color c3 = Color.FromArgb(Math.Abs(c2.R - c1.R), Math.Abs(c2.G - c1.G), Math.Abs(c2.B - c1.B));
            if (c1.R - c2.R < 0 || c1.G - c2.G < 0 || c1.B - c2.B < 0)
                return Color.Red;//пропало
            if (c1.R - c2.R > 0 && c1.G - c2.G > 0 && c1.B - c2.B > 0)
                return Color.Green; //добавилось
            return Color.Black; //не изменилось
            //return c3;
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
            Source = null;
            Bones = null;
            IsDisposed = true;
        }

        #endregion
    }
}
