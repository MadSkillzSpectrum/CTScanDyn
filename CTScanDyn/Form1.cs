using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dicom;
using Dicom.Imaging;
using Dicom.Imaging.Mathematics;
using Emgu;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Flann;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.XFeatures2D;

namespace CTScanDyn
{
    public partial class Form1 : Form
    {
        public Bitmap Original1;
        public Bitmap Original2;

        private List<ImageData> ImageSetBefore = new List<ImageData>();
        private List<ImageData> ImageSetAfter = new List<ImageData>();
        private List<ImageDataResult> ImageSetResult = new List<ImageDataResult>();

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FillImageSet(ImageSetBefore, "before");
        }


        private void button2_Click(object sender, EventArgs e)
        {
            FillImageSet(ImageSetAfter, "after");
            TrimExtra();
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
            if (Directory.Exists(prefix))
                Directory.Delete(prefix, true);
            Directory.CreateDirectory(prefix);
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                var files = Directory.GetFiles(folderBrowserDialog1.SelectedPath, "*.dcm");
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

        private void button3_Click(object sender, EventArgs e)
        {
            if (Directory.Exists("result"))
                Directory.Delete("result", true);
            Directory.CreateDirectory("result");
            listView1.Items.Clear();
            imageList2.Images.Clear();
            for (int i = 0; i < ImageSetBefore.Count; i++)
            {
                var image1 = ImageSetBefore.ElementAt(i);
                var image2 = ImageSetAfter.ElementAt(i);

                VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch();

                using (Emgu.CV.Flann.LinearIndexParams ip = new Emgu.CV.Flann.LinearIndexParams())
                using (Emgu.CV.Flann.SearchParams sp = new SearchParams())
                using (DescriptorMatcher matcher = new FlannBasedMatcher(ip, sp))
                {
                    matcher.Add(image1.Descriptors);
                    matcher.KnnMatch(image2.Descriptors, matches, 2, null);
                    Mat mask = new Mat(matches.Size, 1, DepthType.Cv8U, 1);
                    mask.SetTo(new MCvScalar(255));
                    Features2DToolbox.VoteForUniqueness(matches, 0.9, mask);
                    Mat homography = null;
                    int сount = CvInvoke.CountNonZero(mask);
                    if (сount >= 4)
                    {
                        сount = Features2DToolbox.VoteForSizeAndOrientation(image1.KeyPoints, image2.KeyPoints, matches, mask, 1.5, 20);
                        if (сount >= 4)
                            homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(image1.KeyPoints, image2.KeyPoints, matches, mask, 2);
                    }

                    Mat result = new Mat();
                    Image image = null;
                    Features2DToolbox.DrawMatches(image1.CvMaterial, image1.KeyPoints, image2.CvMaterial, image2.KeyPoints,
                        matches, result, new MCvScalar(255, 255, 255), new MCvScalar(255, 255, 255), mask, Features2DToolbox.KeypointDrawType.Default);
                    Mat regged = new Mat();
                    if (homography != null)
                    {
                        //draw a rectangle along the projected model
                        System.Drawing.Rectangle rect = new System.Drawing.Rectangle(Point.Empty, image1.CvMaterial.Size);
                        PointF[] pts = new PointF[]
                        {
                            new PointF(rect.Left, rect.Bottom),
                            new PointF(rect.Right, rect.Bottom),
                            new PointF(rect.Right, rect.Top),
                            new PointF(rect.Left, rect.Top)
                        };
                        pts = CvInvoke.PerspectiveTransform(pts, homography);

                        Point[] points = Array.ConvertAll<PointF, Point>(pts, Point.Round);
                        using (VectorOfPoint vp = new VectorOfPoint(points))
                        {
                            CvInvoke.Polylines(result, vp, true, new MCvScalar(255, 0, 0, 255), 1);
                        }
                        CvInvoke.WarpPerspective(image1.CvOriginal, regged, homography, image1.Size);
                        image = ImageData.Subtract(regged.Bitmap, image2.Source);
                    }
                    else
                        image = ImageData.Subtract(image1.Source, image2.Source);
                    image.Save("result/" + i + ".jpg");
                    ImageSetResult.Add(new ImageDataResult((Bitmap)image, result){ Registered = regged});
                    imageList2.Images.Add(image);
                    ListViewItem item = new ListViewItem { ImageIndex = i, Text = i.ToString() };
                    listView1.Items.Add(item);
                }

            }
            listView1.Update();
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;

            var i = listView1.SelectedItems[0].ImageIndex;
            OpenImages(i);
        }

        private void OpenImages(int i)
        {
            pictureBox1.Image = ImageSetResult[i].Source;
            pictureBox3.Image = ImageSetBefore[i].Source;
            pictureBox2.Image = ImageSetAfter[i].Source;
            imageBox1.Image = ImageSetResult[i].Difference;
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}