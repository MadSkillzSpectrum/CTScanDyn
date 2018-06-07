﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Util;

namespace CTScanDyn
{
    class SIFTMatchBehaviour : MatchBehaviour
    {
        public SIFTMatchBehaviour(ImageData image1, ImageData image2, VectorOfVectorOfDMatch match) : base(image1,
            image2, match)
        {
            
        }
    }
}
