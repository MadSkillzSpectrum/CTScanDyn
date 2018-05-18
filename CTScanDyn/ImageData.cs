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
        public Bitmap Source = null;
        private BitmapData bData = null;

        public byte[] Pixels { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public int Depth { get; set; }
        public List<Point> SignificantPoints { get; set; }
        public Point CenterOfMass { get; set; }
        public Point Offset { get; set; }
        public int RotationDgr { get; set; }
        public bool IsDisposed { get; set; }
        private IntPtr Iptr { get; set; }
        private int rSize { get; set; }
        public Mat Descriptors { get; set; }
        public VectorOfKeyPoint KeyPoints { get; set; }

        public ImageData(Bitmap source)
        {
            this.Source = source;
            SignificantPoints = new List<Point>();
            IsDisposed = false;
        }

        public Point GetCentroid()
        {
            for (int y = 0; y < Height; y += 4)
                for (int x = 0; x < Width; x += 4)
                {

                    if (Pixels[x * rSize + y] + Pixels[x * rSize + y + 1] + Pixels[x * rSize + y + 2] > 0)
                    {
                        SignificantPoints.Add(new Point(x, y));
                    }
                }
            CalculateCenter();
            return CenterOfMass;
        }

        private void CalculateCoVar(ImageData image2)
        {
            foreach (Point p in SignificantPoints)
            {
                                
            }
        }

        private void CalculateCenter()
        {
            float x = 0f;
            float y = 0f;
            foreach (Point p in SignificantPoints)
            {
                x += p.X;
                y += p.Y;
            }
            x = x / SignificantPoints.Count;
            y = y / SignificantPoints.Count;
            CenterOfMass = new Point((int)x, (int)y);
        }

        public void LockBits()
        {
            try
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(typeof(ImageData).Name);

                Width = Source.Width;
                Height = Source.Height;
                Rectangle rect = new Rectangle(0, 0, Width, Height);
                Depth = Bitmap.GetPixelFormatSize(Source.PixelFormat);

                if (Depth != 8 && Depth != 24 && Depth != 32)
                {
                    throw new ArgumentException("Only 8, 24 and 32 bpp images are supported.");
                }

                bData = Source.LockBits(rect, ImageLockMode.ReadWrite, Source.PixelFormat);
                rSize = bData.Stride < 0 ? -bData.Stride : bData.Stride;
                Pixels = new byte[Height * rSize];
                Iptr = bData.Scan0;
                for (int y = 0; y < Height; y++)
                {
                    Marshal.Copy(IntPtr.Add(Iptr, y * bData.Stride), Pixels, y * rSize, rSize);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void UnlockBits()
        {
            try
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(typeof(ImageData).Name);
                if (bData == null)
                    throw new InvalidOperationException("Image is not locked.");

                // Copy data from byte array to pointer
                //Marshal.Copy(Pixels, 0, Iptr, Pixels.Length);
                for (int y = 0; y < Height; y++)
                {
                    Marshal.Copy(Pixels, y * rSize, IntPtr.Add(Iptr, y * bData.Stride), rSize);
                }

                // Unlock bitmap data
                Source.UnlockBits(bData);
                bData = null;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Translate(Point diff)
        {
            if (diff.X != 0 && diff.Y != 0)
            {
                var newPoints = new List<Point>();
                foreach (var p in SignificantPoints)
                {
                    var newX = p.X + diff.X;
                    var newY = p.Y + diff.Y;
                    if (newX > 0 && newX < Width && newY > 0 && newY < Height)
                        newPoints.Add(new Point(newX, newY));
                }
                SignificantPoints = newPoints;
            }
        }
        public static Image Subtract(ImageData image1, ImageData image2, int diffX, int diffY)
        {
            Bitmap res = new Bitmap(image1.Width, image1.Height);
            for (int y = 0; y < image1.Height; y++)
            {
                for (int x = 0; x < image1.Width; x++)
                {
                    if (x + diffX < image1.Width && y - diffY < image1.Height && y - diffY >= 0 && x + diffX >= 0)
                    {
                        Color c1 = image1.GetPixel(x + diffX, y - diffY);
                        Color c2 = image2.GetPixel(x, y);
                        res.SetPixel(x, y, SubtractPixel(image2.GetPixel(x, y), image1.GetPixel(x + diffX, y - diffY)));
                    }
                }
            }
            return res;
        }

        private static Color SubtractPixel(Color c1, Color c2)
        {
            //Color c3 = Color.FromArgb(Math.Abs(c2.R - c1.R), Math.Abs(c2.G - c1.G), Math.Abs(c2.B - c1.B));
            if (c1.R - c2.R < 0 || c1.G - c2.G < 0 || c1.B - c2.B < 0)
                return Color.Red;//пропало
            else if (c1.R - c2.R > 0 && c1.G - c2.G > 0 && c1.B - c2.B > 0)
                return Color.Green; //добавилось
            else
                return Color.Black; //не изменилось
            //return c3;
        }

        public Color GetPixel(int x, int y)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(typeof(ImageData).Name);

            Color clr = Color.Empty;

            // Get color components count
            int cCount = Depth / 8;

            // Get start index of the specified pixel
            int i = (y * rSize) + (x * cCount);

            if (i > Pixels.Length - cCount)
                throw new IndexOutOfRangeException();

            if (Depth == 32) // For 32 bpp get Red, Green, Blue and Alpha
            {
                byte b = Pixels[i];
                byte g = Pixels[i + 1];
                byte r = Pixels[i + 2];
                byte a = Pixels[i + 3]; // a
                clr = Color.FromArgb(a, r, g, b);
            }
            if (Depth == 24) // For 24 bpp get Red, Green and Blue
            {
                byte b = Pixels[i];
                byte g = Pixels[i + 1];
                byte r = Pixels[i + 2];
                clr = Color.FromArgb(r, g, b);
            }
            if (Depth == 8)
            // For 8 bpp get color value (Red, Green and Blue values are the same)
            {
                byte c = Pixels[i];
                clr = Color.FromArgb(c, c, c);
            }
            return clr;
        }

        public void SetPixel(int x, int y, Color color)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(typeof(ImageData).Name);

            // Get color components count
            int cCount = Depth / 8;

            // Get start index of the specified pixel
            int i = (y * rSize) + (x * cCount);
            if (i > Pixels.Length - cCount)
                throw new IndexOutOfRangeException();

            if (Depth == 32) // For 32 bpp set Red, Green, Blue and Alpha
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
                Pixels[i + 3] = color.A;
            }
            if (Depth == 24) // For 24 bpp set Red, Green and Blue
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
            }
            if (Depth == 8)
            // For 8 bpp set color value (Red, Green and Blue values are the same)
            {
                Pixels[i] = color.B;
            }
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
            if (bData != null)
            {
                Source.UnlockBits(bData);
                bData = null;
            }
            Source = null;
            IsDisposed = true;
        }

        #endregion
    }
}
