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
using Emgu.CV.Util;
using Emgu.CV.XFeatures2D;

namespace CTScanDyn
{
    class CTMatcher
    {
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

        public 

        public void LoadFirstSession()
        {
            FillImageSet(ImageSetBefore, BeforeDirName);
        }

        public void LoadSecondSession()
        {
            FillImageSet(ImageSetAfter, AfterDirName);
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
                    var ds_bones = new DicomImage(file)
                    {
                        WindowWidth = 100,
                        WindowCenter = 500
                    };
                    var image = ds.RenderImage().AsBitmap();
                    var image_bones = ds_bones.RenderImage().AsBitmap();
                    string newName = prefix + "/" + Path.GetFileName(file).Replace(".dcm", ".jpg");
                    string newBonesName = prefix + "/" + Path.GetFileName(file).Replace(".dcm", "_bones.jpg");
                    image.Save(newName);
                    image_bones.Save(newBonesName);
                    image = (Bitmap)Image.FromFile(newName);
                    image_bones = (Bitmap)Image.FromFile(newBonesName);

                    SIFT s = new SIFT();
                    Mat mat = CvInvoke.Imread(newBonesName, ImreadModes.Grayscale);
                    Mat matOrig = CvInvoke.Imread(newName, ImreadModes.Unchanged);
                    var vec = new VectorOfKeyPoint();
                    Mat modelDescriptors = new Mat();
                    s.DetectAndCompute(mat, null, vec, modelDescriptors, false);
                    ImageData id = new ImageData(image, image_bones)
                    {
                        KeyPoints = vec,
                        Descriptors = modelDescriptors,
                        CvMaterial = mat,
                        CvOriginal = matOrig
                    };
                    set.Add(id);
                }
            }
        }
    }
}
