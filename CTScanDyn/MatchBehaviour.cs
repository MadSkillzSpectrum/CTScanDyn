using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Flann;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace CTScanDyn
{
    abstract class MatchBehaviour
    {
        protected ImageData Image1;
        protected ImageData Image2;
        protected VectorOfVectorOfDMatch Matches;

        protected MatchBehaviour(ImageData image1, ImageData image2, VectorOfVectorOfDMatch match)
        {
            Image1 = image1;
            Image2 = image2;
            Matches = match;
        }

        protected virtual Mat GetMask()
        {
            return new Mat(Matches.Size, 1, DepthType.Cv8U, 1);
        }
        protected virtual DescriptorMatcher GetDescritorMatcher()
        {
            var ip = new LinearIndexParams();
            var sp = new SearchParams();
            return new FlannBasedMatcher(ip, sp);
        }

        public Image Match(List<ImageDataResult> resultList)
        {
            DescriptorMatcher matcher = GetDescritorMatcher();
            matcher.Add(Image1.Descriptors);
            matcher.KnnMatch(Image2.Descriptors, Matches, 2, null);
            var mask = GetMask();
            Mat homography = GetHomography(Image1.KeyPoints, Image2.KeyPoints, Matches, mask);
            Mat result = new Mat();
            Features2DToolbox.DrawMatches(Image1.CvMaterial, Image1.KeyPoints, Image2.CvMaterial, Image2.KeyPoints,
                Matches, result, new MCvScalar(255, 255, 255), new MCvScalar(255, 255, 255), mask);
            Mat regged = new Mat();
            if (homography != null)
            {
                Rectangle rect = new System.Drawing.Rectangle(Point.Empty, Image1.CvMaterial.Size);
                PointF[] pts =
                {
                        new PointF(rect.Left, rect.Bottom),
                        new PointF(rect.Right, rect.Bottom),
                        new PointF(rect.Right, rect.Top),
                        new PointF(rect.Left, rect.Top)
                    };
                pts = CvInvoke.PerspectiveTransform(pts, homography);

                Point[] points = Array.ConvertAll(pts, Point.Round);
                using (VectorOfPoint vp = new VectorOfPoint(points))
                {
                    CvInvoke.Polylines(result, vp, true, new MCvScalar(255, 0, 0, 255));
                }
                CvInvoke.WarpPerspective(Image1.CvOriginal, regged, homography, Image1.Size);
                var image = ImageData.Subtract(regged.Bitmap, Image2.CvOriginal.Bitmap);
                resultList.Add(new ImageDataResult((Bitmap)image, result, regged));
                return image;
            }
            return ImageData.Subtract(Image1.CvOriginal.Bitmap, Image2.CvOriginal.Bitmap);
        }

        protected Mat GetHomography(VectorOfKeyPoint keyPoints1, VectorOfKeyPoint keyPoints2, VectorOfVectorOfDMatch matches, Mat mask)
        {
            Mat homography = null;
            mask.SetTo(new MCvScalar(255));
            Features2DToolbox.VoteForUniqueness(matches, 0.9, mask);
            int i = CvInvoke.CountNonZero(mask);
            if (i >= 4)
            {
                i = Features2DToolbox.VoteForSizeAndOrientation(keyPoints1, keyPoints2, matches, mask, 1.5, 20);
                if (i >= 4)
                    homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(keyPoints1, keyPoints2, matches, mask, 2);
            }
            return homography;
        }
    }
}
