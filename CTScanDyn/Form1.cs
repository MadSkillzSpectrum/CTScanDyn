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
            button2.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FillImageSet(ImageSetBefore, "before");
            button2.Enabled = true;
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
                ImageSetBefore.RemoveRange(ImageSetAfter.Count-1, ImageSetBefore.Count - ImageSetAfter.Count);
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
                    var image = new DicomImage(file).RenderImage().AsBitmap();
                    string newName = prefix +"/"+ Path.GetFileName(file).Replace(".dcm", ".jpg");
                    image.Save(newName);
                    image = (Bitmap)Image.FromFile(newName);
                    ImageData id = new ImageData(image);
                    set.Add(id);
                }
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            label1.Text = (trackBar1.Value * 100 / trackBar1.Maximum) + @"%";
        }


        private void button3_Click(object sender, EventArgs e)
        {
            Directory.Delete("result", true);
            Directory.CreateDirectory("result");
            listView1.Items.Clear();
            imageList2.Images.Clear();
            int sum = trackBar1.Value;
            int diffX=0, diffY=0;
            for (int i=0; i<ImageSetBefore.Count;i++)
            {
                var image1 = ImageSetBefore.ElementAt(i);
                image1.LockBits();
                var p1 = image1.GetBones(sum);

                var image2 = ImageSetAfter.ElementAt(i);
                image2.LockBits();
                var p2 = image2.GetBones(sum);

                //diffX = p2.X - p1.X;
                //diffY = p2.Y - p1.Y;

                var image = ImageData.Subtract(image1, image2, sum, diffX, diffY);
                image.Save("result/"+i+".jpg");
                ImageSetResult.Add(new ImageData((Bitmap) image));
                imageList2.Images.Add(image);
                ListViewItem item = new ListViewItem { ImageIndex =i, Text = i.ToString() };
                listView1.Items.Add(item);

                image1.UnlockBits();
                image2.UnlockBits();

            }
            listView1.Update();
            /*diffX = diffX / ImageSetBefore.Count;
            diffY = diffY / ImageSetBefore.Count;
            dataGridView1.Rows.Add(diffX, diffY);*/
        }

        public void UpdateResult()
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            UpdateResult();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(listView1.SelectedItems.Count>0)
                pictureBox1.Image = ImageSetResult[listView1.SelectedItems[0].ImageIndex].Source;
        }
    }
}