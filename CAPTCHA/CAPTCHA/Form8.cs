using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;

namespace CAPTCHA
{
    public partial class Form8 : Form
    {
        private double zoomScale = 1;
        private List<Contour<Point>> completeContour;
        private Bitmap bmpShow;
        private Bitmap bmpClear;

        public Form8()
        {
            InitializeComponent();
        }

        private void Form8_Load(object sender, EventArgs e)
        {
            bmpShow = ImageProcessor.ChangeSize(ShowBin.bmp, ShowBin.bmp.Width, ShowBin.bmp.Height + 4, 0, 2); 
            Image<Bgr, Byte> bmp = new Image<Bgr, byte>(bmpShow);
            Image<Gray, byte> bmpContour = new Image<Gray, byte>(bmpShow);
            Contour<Point> foundContours = bmpContour.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_NONE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST);
            completeContour = FilterContours(foundContours);
            imageBox1.Image = bmp;

            bmpClear = new Bitmap(bmpShow.Width, bmpShow.Height);

            using (Graphics g = Graphics.FromImage(bmpClear))
                g.FillRectangle(Brushes.White, 0, 0, bmpShow.Width, bmpShow.Height);

            toolStripStatusLabel1.Text = String.Empty;
            toolStripStatusLabel2.Text = String.Empty;
        }

        private void imageBox1_MouseMove(object sender, MouseEventArgs e)
        {
            toolStripStatusLabel1.Text = "Позиция: " + e.X + "x" + e.Y;
            toolStripStatusLabel2.Text = "Увеличение: x" + zoomScale;
        }

        private void imageBox1_MouseLeave(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = String.Empty;
            toolStripStatusLabel2.Text = String.Empty;
        }

        private void imageBox1_OnZoomScaleChange(object sender, EventArgs e)
        {
            zoomScale = imageBox1.ZoomScale;
            toolStripStatusLabel2.Text = "Увеличение: x" + zoomScale;
        }

        private void imageBox1_Paint(object sender, PaintEventArgs e)
        {
            Pen borderPen = new Pen(Color.FromArgb(150, 0, 255, 0));

            if (completeContour != null)
            {
                foreach (Contour<Point> contour in completeContour)
                {
                    if (contour.Total > 1)
                    {
                        e.Graphics.DrawLines(Pens.Red, contour.ToArray());

                        Point[] lastPixel = new Point[2];
                        lastPixel[0] = contour.ToArray()[0];
                        lastPixel[1] = contour.ToArray()[contour.ToArray().Length - 1];
                        e.Graphics.DrawLines(Pens.Red, lastPixel);
                    }
                }
            }
        }

        private List<Contour<Point>> FilterContours(Contour<Point> foundContours)
        {
            List<Contour<Point>> charContours = new List<Contour<Point>>();

            while (foundContours != null)
            {
                charContours.Add(foundContours);
                foundContours = foundContours.HNext;
            }

            charContours.RemoveAt(charContours.Count - 1);

            return charContours;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
                imageBox1.Image = new Image<Gray, byte>(bmpShow);
            else
                imageBox1.Image = new Image<Gray, byte>(bmpClear);
        }
    }
}
