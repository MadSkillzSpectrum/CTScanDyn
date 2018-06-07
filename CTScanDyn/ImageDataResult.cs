using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;

namespace CTScanDyn
{
    class ImageDataResult : IDisposable
    {
        public Mat Difference = null;
        public Bitmap Source = null;
        public Mat Registered = null;
        public bool IsDisposed { get; set; }

        public ImageDataResult(Bitmap source, Mat mat, Mat regged)
        {
            Difference = mat;
            Source = source;
            Registered = regged;
            IsDisposed = false;
        }

        #region IDisposable Members

        ~ImageDataResult()
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
            Difference = null;
            Registered = null;
            IsDisposed = true;
        }

        #endregion
    }
}
