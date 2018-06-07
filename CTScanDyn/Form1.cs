using System;
using System.Windows.Forms;

namespace CTScanDyn
{
    public partial class Form1 : Form
    {
        private CTMatcher ct = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ct.LoadFirstSession();
        }


        private void button2_Click(object sender, EventArgs e)
        {
            ct.LoadSecondSession();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ct.Register(imageList2.Images, listView1.Items);
            listView1.Update();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ct = new CTMatcher(folderBrowserDialog1);
            toolStripComboBox1.SelectedIndex = 0;
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            var i = listView1.SelectedItems[0].ImageIndex;
            ImageDataResult result = ct.GetResultByIndex(i);
            pictureBox1.Image = result.Source;
            imageBox1.Image = result.Difference;
            pictureBox3.Image = ct.GetFirstSessionImage(i).CvOriginal.Bitmap;
            pictureBox2.Image = ct.GetSecondSessionImage(i).CvOriginal.Bitmap;
        }

        private void toolStripComboBox1_Click(object sender, EventArgs e)
        {

        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (toolStripComboBox1.SelectedItem)
            {
                case "ORB":
                    ct.SetAlgo(CTMatcher.Algo.ORB);
                    break;
                case "SURF":
                    ct.SetAlgo(CTMatcher.Algo.SURF);
                    break;
                default:
                    ct.SetAlgo(CTMatcher.Algo.SIFT);
                    break;
            }
        }
    }
}