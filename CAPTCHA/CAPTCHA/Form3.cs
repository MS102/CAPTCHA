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
    public partial class Form3 : Form
    {
        private double zoomScale = 1;

        public Form3()
        {
            InitializeComponent();
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            Image<Bgr, byte> bmp = new Image<Bgr, byte>(ShowBin.bmp);
            imageBox1.Image = bmp;
            toolStripStatusLabel1.Text = String.Empty;
            toolStripStatusLabel2.Text = String.Empty;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();

            sfd.Title = "Cохранить";
            sfd.Filter = "Все файлы изображений|*.jpg;*.jpeg;*.png;*.bmp";
            sfd.Filter += "|JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG (*.png)|*.png|Точечный рисунок (*.bmp)|*.bmp";
            sfd.AddExtension = true;
            sfd.DefaultExt = ".png";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                if (ShowBin.bmp != null)
                    ShowBin.bmp.Save(sfd.FileName);
                else
                    MessageBox.Show("Изображение отсутствует.", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
    }

    public static class ShowBin
    {
        public static Bitmap bmp { get; set; }
    }
}
