using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;

namespace CTScanDyn
{
    class ImageDataResult
    {
        public Mat Difference = null;
        public Bitmap Source = null;
        public ImageDataResult(Bitmap source, Mat mat)
        {
            Difference = mat;
            Source = source;
        }
    }
}
