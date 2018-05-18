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
        private List<ImageData> ImageSetResult = new List<ImageData>();

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

        public List<Point> Rotate(List<Point> points, Point pivot, double angleDegree)
        {
            double angle = angleDegree * Math.PI / 180;
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            List<Point> rotated = new List<Point>();
            foreach (Point point in points)
            {
                int dx = point.X - pivot.X;
                int dy = point.Y - pivot.Y;
                double x = cos * dx - sin * dy + pivot.X;
                double y = sin * dx + cos * dy + pivot.Y;
                rotated.Add(new Point((int)Math.Round(x), (int)Math.Round(y)));
            }
            return rotated;
        }

        private void FillImageSet(List<ImageData> set, string prefix)
        {
            Directory.Delete(prefix, true);
            Directory.CreateDirectory(prefix);
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                var files = Directory.GetFiles(folderBrowserDialog1.SelectedPath, "*.dcm");
                foreach (var file in files)
                {
                    var ds = new DicomImage(file)
                    {
                        WindowWidth = 5,
                        WindowCenter = 500
                    };
                    var image = ds.RenderImage().AsBitmap();
                    string newName = prefix + "/" + Path.GetFileName(file).Replace(".dcm", ".jpg");
                    image.Save(newName);
                    image = (Bitmap)Image.FromFile(newName);

                    
                    SIFT s = new SIFT();
                    Mat mat = CvInvoke.Imread(newName, ImreadModes.Grayscale);
                    var vec = new VectorOfKeyPoint();
                    Mat modelDescriptors = new Mat();
                    s.DetectAndCompute(mat, null, vec, modelDescriptors, false);
                    ImageData id = new ImageData(image)
                    {
                        KeyPoints = vec,
                        Descriptors = modelDescriptors
                    };
                    set.Add(id);
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Directory.Delete("result", true);
            Directory.CreateDirectory("result");
            listView1.Items.Clear();
            imageList2.Images.Clear();
            int diffX = 0, diffY = 0;
            for (int i = 0; i < ImageSetBefore.Count; i++)
            {
                var image1 = ImageSetBefore.ElementAt(i);
                image1.LockBits();

                var p1 = image1.GetCentroid();
                var image2 = ImageSetAfter.ElementAt(i);
                image2.LockBits();

                var p2 = image2.GetCentroid();

                /*var diff = new Point(p2.X - p1.X,p2.Y-p1.X);
                image1.Translate(diff);*/

                var image = ImageData.Subtract(image1, image2, diffX, diffY);
                image.Save("result/" + i + ".jpg");
                ImageSetResult.Add(new ImageData((Bitmap)image));
                imageList2.Images.Add(image);
                ListViewItem item = new ListViewItem { ImageIndex = i, Text = i.ToString() };
                listView1.Items.Add(item);

                image1.UnlockBits();
                image2.UnlockBits();

                VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch();

                using (Emgu.CV.Flann.LinearIndexParams ip = new Emgu.CV.Flann.LinearIndexParams())
                using (Emgu.CV.Flann.SearchParams sp = new SearchParams())
                using (DescriptorMatcher matcher = new FlannBasedMatcher(ip, sp))
                {
                    matcher.Add(image1.Descriptors);
                    matcher.KnnMatch(image2.Descriptors, matches, 2, null);
                    Mat mask = new Mat(matches.Size, 1, DepthType.Cv8U, 1);
                    mask.SetTo(new MCvScalar(255));
                    Features2DToolbox.VoteForUniqueness(matches, 0.8, mask);
                    Mat homography = null;
                    int Count = CvInvoke.CountNonZero(mask);
                    if (Count >= 4)
                    {
                        Count = Features2DToolbox.VoteForSizeAndOrientation(image1.KeyPoints, image2.KeyPoints, matches, mask, 1.5, 20);
                        if (Count >= 4)
                            homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(image1.KeyPoints, image2.KeyPoints, matches, mask, 2);
                    }
                    var pts = image1.SignificantPoints.Select(a => (PointF) a).ToArray();
                   // CvInvoke.PerspectiveTransform(pts,homography);
                   // RotatedRect IdentifiedImage = CvInvoke.MinAreaRect(pts);
                }

                //CvInvoke.SVDecomp();

            }
            listView1.Update();
            /*diffX = diffX / ImageSetBefore.Count;
            diffY = diffY / ImageSetBefore.Count;
            dataGridView1.Rows.Add(diffX, diffY);*/
        }

        public void UpdateResult()
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
                pictureBox1.Image = ImageSetResult[listView1.SelectedItems[0].ImageIndex].Source;
        }
    }
}