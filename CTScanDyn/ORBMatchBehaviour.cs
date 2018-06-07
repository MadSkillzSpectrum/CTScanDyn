using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Flann;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace CTScanDyn
{
    class ORBMatchBehaviour : MatchBehaviour
    {
        public ORBMatchBehaviour(ImageData image1, ImageData image2, VectorOfVectorOfDMatch match) : base(image1,
            image2, match)
        {
            
        }

        protected override Mat GetMask()
        {
           return new Mat(Matches.Size, 1, DepthType.Cv8U, 1);
        }

        protected override DescriptorMatcher GetDescritorMatcher()
        {
            return new BFMatcher(DistanceType.Hamming);
        }
    }
}
