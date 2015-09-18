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
    public partial class Form4 : Form
    {
        private double zoomScale = 1;

        public Form4()
        {
            InitializeComponent();
        }

        private void Form4_Load(object sender, EventArgs e)
        {
            Image<Bgr, Byte> bmp = new Image<Bgr, byte>(ShowBounds.bmp);
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
                if (ShowBounds.bmp != null)
                    ShowBounds.bmp.Save(sfd.FileName);
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

    public static class ShowBounds
    {
        public static Bitmap bmp { get; set; }
    }
}
