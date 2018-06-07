using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dicom.Imaging;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Flann;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.XFeatures2D;

namespace CTScanDyn
{
    class CTMatcher
    {
        public enum Algo { SIFT, SURF, ORB }

        private Algo algorithm = Algo.SIFT;
        private List<ImageData> ImageSetBefore = new List<ImageData>();
        private List<ImageData> ImageSetAfter = new List<ImageData>();
        private List<ImageDataResult> ImageSetResult = new List<ImageDataResult>();
        private FolderBrowserDialog dialog;
        const string BeforeDirName = "before";
        const string AfterDirName = "after";
        const string ResultDirName = "result";

        public CTMatcher(FolderBrowserDialog dialog)
        {
            this.dialog = dialog;
        }

        public void SetAlgo(Algo algo)
        {
            algorithm = algo;
        }
        public void LoadFirstSession()
        {
            FillImageSet(ImageSetBefore, BeforeDirName);
        }

        public void LoadSecondSession()
        {
            FillImageSet(ImageSetAfter, AfterDirName);
        }

        public void Register(ImageList.ImageCollection imageList, ListView.ListViewItemCollection listView)
        {
            TrimExtra();
            UtilityHelper.refreshDirectory(ResultDirName);
            listView.Clear();
            imageList.Clear();

            for (int i = 0; i < ImageSetBefore.Count; i++)
            {
                var image1 = GetFirstSessionImage(i);
                var image2 = GetSecondSessionImage(i);

                var matches = new VectorOfVectorOfDMatch();
                MatchBehaviour mb;
                switch (algorithm)
                {
                    case Algo.ORB:
                        mb = new ORBMatchBehaviour(image1, image2, matches);
                        break;
                    case Algo.SURF:
                        mb = new SURFMatchBehaviour(image1, image2, matches);
                        break;
                    default:
                        mb = new SIFTMatchBehaviour(image1, image2, matches);
                        break;
                }

                Image image = mb.Match(ImageSetResult);
                image.Save(ResultDirName + "/" + i + ".jpg");
                imageList.Add(image);
                ListViewItem item = new ListViewItem { ImageIndex = i, Text = i.ToString() };
                listView.Add(item);
            }
        }

        public ImageDataResult GetResultByIndex(int i)
        {
            return ImageSetResult[i];
        }

        public ImageData GetFirstSessionImage(int i)
        {
            return ImageSetBefore[i];
        }

        public ImageData GetSecondSessionImage(int i)
        {
            return ImageSetAfter[i];
        }

        private void TrimExtra()
        {
            if (ImageSetBefore.Count > ImageSetAfter.Count)
            {
                ImageSetBefore.RemoveRange(ImageSetAfter.Count - 1, ImageSetBefore.Count - ImageSetAfter.Count);
            }
            else if (ImageSetBefore.Count < ImageSetAfter.Count)
            {
                ImageSetAfter.RemoveRange(ImageSetBefore.Count - 1, ImageSetAfter.Count - ImageSetBefore.Count);
            }
        }

        private void FillImageSet(List<ImageData> set, string prefix)
        {
            UtilityHelper.refreshDirectory(prefix);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var files = Directory.GetFiles(dialog.SelectedPath, "*.dcm");
                foreach (var file in files)
                {
                    var ds = new DicomImage(file);
                    var dsBones = new DicomImage(file)
                    {
                        WindowWidth = 100,
                        WindowCenter = 500
                    };
                    var image = ds.RenderImage().AsBitmap();
                    var imageBones = dsBones.RenderImage().AsBitmap();
                    string newName = prefix + "/" + Path.GetFileName(file).Replace(".dcm", ".jpg");
                    string newBonesName = prefix + "/" + Path.GetFileName(file).Replace(".dcm", "_bones.jpg");
                    image.Save(newName);
                    imageBones.Save(newBonesName);
                    Feature2D s;
                    switch (algorithm)
                    {
                        case Algo.ORB:
                            s = new ORBDetector();
                            break;
                        case Algo.SURF:
                            s = new SURF(0.8);
                            break;
                        default:
                            s = new SIFT();
                            break;
                    }
                    Mat mat = CvInvoke.Imread(newBonesName, ImreadModes.Grayscale);
                    Mat matOrig = CvInvoke.Imread(newName, ImreadModes.Unchanged);
                    var vec = new VectorOfKeyPoint();
                    Mat modelDescriptors = new Mat();
                    s.DetectAndCompute(mat, null, vec, modelDescriptors, false);
                    ImageData id = new ImageData(matOrig, mat)
                    {
                        KeyPoints = vec,
                        Descriptors = modelDescriptors
                    };
                    set.Add(id);
                }
            }
        }
    }
}
