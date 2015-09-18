using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Threading;
using Emgu.CV;
using Emgu.CV.Structure;

namespace CAPTCHA
{
    public partial class Form5 : Form
    {
        private OpenFileDialog ofd = new OpenFileDialog();
        private List<Contour<Point>> completeContour;
        private List<Contour<Point>> saveContour;
        private Bitmap bmpShow;
        private Bitmap bmpClear;
        private List<string> momentsList = new List<string>();
        private List<string> calculatedMoments = new List<string>();
        public static bool renewMoments = false;

        public Form5()
        {
            InitializeComponent();
            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;

            if (!File.Exists("Moments.bin"))
                File.Create("Moments.bin");
            else
                using (StreamReader streamReader = new StreamReader("Moments.bin"))
                    while (!streamReader.EndOfStream)
                        momentsList.Add(streamReader.ReadLine());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ofd.Title = "Открыть";
            ofd.Filter = "Все файлы изображений|*.jpg;*.jpeg;*.png;*.bmp";
            ofd.Filter += "|JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG (*.png)|*.png|Точечный рисунок (*.bmp)|*.bmp";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = ofd.FileName;
                button2.Enabled = true;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button3.Enabled = true;
            if (File.Exists(textBox1.Text))
            {
                pictureBox1.Image = Image.FromFile(textBox1.Text);
                ProcessImage(pictureBox1.Image);
                pictureBox1.Image = Image.FromFile(textBox1.Text);
            }
            else if (Web.Connection())
            {
                if (Web.IsValidUrl(textBox1.Text))
                {
                    try
                    {
                        pictureBox1.Load(textBox1.Text);
                        ProcessImage(pictureBox1.Image);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("По указанному адресу отсутствует изображение.", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                    MessageBox.Show("Неверный путь к файлу или адрес URL.", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
                MessageBox.Show("Отсутствует подключение или неверный путь к файлу или адрес URL.", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ProcessImage(Image imageStart)
        {
            Bitmap bmpStart = new Bitmap(imageStart);

            if (bmpStart.Height != 18)
            {
                pictureBox1.Image = null;
                MessageBox.Show("Неверный тип капчи или изображение имеет недопустимый размер.", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                Bitmap bmpGrayScale = ImageProcessor.CropBounds(ImageProcessor.ToGrayScale(bmpStart), 2, 2, 4, 4);
                int otsuThreshold = ImageProcessor.OtsuThreshold(bmpGrayScale);
                Bitmap bmpBin = ImageProcessor.Binarization(bmpGrayScale, otsuThreshold);
                bmpShow = ImageProcessor.ChangeSize(bmpBin, bmpBin.Width, bmpBin.Height + 4, 0, 2);
                ImageProcessor.DefineSymbolsBounds((Bitmap)(Image)bmpBin.Clone(), (Bitmap)(Image)bmpBin.Clone(), false);

                if (ImageProcessor.segments.Count == 1)
                {
                    Bitmap bmpTemp = (Bitmap)pictureBox1.Image;
                    bmpGrayScale = ImageProcessor.CropBounds(ImageProcessor.ToGrayScale(bmpTemp), 2, 2, 4, 4);
                    bmpTemp = ImageProcessor.Binarization(bmpGrayScale, 120);
                    bmpShow = ImageProcessor.ChangeSize(bmpTemp, bmpTemp.Width, bmpTemp.Height + 4, 0, 2);
                    ImageProcessor.DefineSymbolsBounds(bmpTemp, bmpTemp, false);
                }

                Image<Gray, byte> bmpContour = new Image<Gray, byte>(bmpShow);
                Contour<Point> foundContours = bmpContour.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_NONE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST);
                completeContour = FilterContours(foundContours);

                bmpClear = new Bitmap(bmpShow.Width, bmpShow.Height);

                using (Graphics g = Graphics.FromImage(bmpClear))
                    g.FillRectangle(Brushes.White, 0, 0, bmpShow.Width, bmpShow.Height);

                if (checkBox1.Checked)
                    imageBox1.Image = new Image<Gray, byte>(bmpShow);
                else
                    imageBox1.Image = new Image<Gray, byte>(bmpClear);

                int index = dataGridView1.Rows.Count - 1;

                while (index > -1)
                {
                    dataGridView1.Rows.RemoveAt(index);
                    index--;
                }

                dataGridView1.Refresh();

                for (int i = 0; i < ImageProcessor.segments.Count; i++)
                {
                    Bitmap bmp = ImageProcessor.segments[i];

                    Image<Gray, byte> segment = new Image<Gray, byte>(bmp);
                    foundContours = segment.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_NONE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST);
                    List<Contour<Point>> setContoursCount = FilterContours(foundContours);

                    double contourMoment = 0;

                    foreach (Contour<Point> contour in setContoursCount)
                    {
                        MCvMoments moments = contour.GetMoments();
                        contourMoment += moments.m00 + moments.m01;
                    }

                    dataGridView1.Rows.Add();
                    dataGridView1.Rows[i].Cells[0].Value = (i + 1).ToString();
                    dataGridView1.Rows[i].Cells[2].Value = contourMoment;
                }

                Image<Gray, byte> firstChar = new Image<Gray, byte>(ImageProcessor.segments[0]);
                foundContours = firstChar.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_NONE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST);
                saveContour = FilterContours(foundContours);
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

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text.Length == 0)
                button2.Enabled = false;
            else
                button2.Enabled = true;
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

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                if (checkBox1.Checked)
                    imageBox1.Image = new Image<Gray, byte>(bmpShow);
                else
                    imageBox1.Image = new Image<Gray, byte>(bmpClear);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            bool success = true;

            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                if (dataGridView1.Rows[i].Cells[1].Value != null)
                {
                    if (!IsChar((string)dataGridView1.Rows[i].Cells[1].Value))
                    {
                        MessageBox.Show("Значения контуров не должны содержать числа или пробелы.", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        success = false;

                        break;
                    }

                    if (!IsLatin((string)dataGridView1.Rows[i].Cells[1].Value))
                    {
                        MessageBox.Show("Значения контуров не должны содержать русские буквы.", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        success = false;

                        break;
                    }
                }
            }

            if (success)
            {
                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                    if (dataGridView1.Rows[i].Cells[1].Value != null)
                        if (!calculatedMoments.Contains(dataGridView1.Rows[i].Cells[1].Value + " " + dataGridView1.Rows[i].Cells[2].Value))
                           calculatedMoments.Add(dataGridView1.Rows[i].Cells[1].Value + " " + dataGridView1.Rows[i].Cells[2].Value);
            }

            if ((success) && (Web.IsValidUrl(textBox1.Text)))
                button2_Click(button2, null);
        }

        private bool IsChar(string str)
        {
            for (int i = 0; i < str.Length; ++i)
            {
                if (!char.IsLetter(str[i]))
                    return false;
            }

            return true;
        }

        private bool IsLatin(string str)
        {
            char[] letters = str.ToCharArray();

            for (int i = 0; i < letters.Length; i++)
            {
                int charValue = Convert.ToInt32(letters[i]);

                if (charValue > 128)
                    return false;
            }

            return true;
        } 

        private void Form5_FormClosing(object sender, FormClosingEventArgs e)
        {
            using (StreamWriter streamWriter = new StreamWriter("Moments.bin", true))
            {
                foreach (string momentData in calculatedMoments)
                {
                    if (!momentsList.Contains(momentData))
                        streamWriter.WriteLine(momentData);
                }
            }

            while (Locked("Moments.bin") != null)
                Thread.Sleep(1000);

            renewMoments = true;
        }

        private Exception Locked(string file)
        {
            Exception exception = null;

            try
            {
                FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read);

                fs.Close();
            }
            catch (IOException e)
            {
                exception = e;
            }

            return exception;
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                int index = dataGridView1.CurrentRow.Index;

                Image<Gray, byte> segment = new Image<Gray, byte>(ImageProcessor.segments[index]);
                Contour<Point> foundContours = segment.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_NONE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST);
                saveContour = FilterContours(foundContours);

                Bitmap segmentClear = new Bitmap(ImageProcessor.segments[0].Width, ImageProcessor.segments[0].Height);

                using (Graphics g = Graphics.FromImage(segmentClear))
                    g.FillRectangle(Brushes.White, 0, 0, segmentClear.Width, segmentClear.Height);

                imageBox2.Image = new Image<Gray, byte>(segmentClear);
            }
            catch (Exception)
            {
            }
        }

        private void imageBox2_Paint(object sender, PaintEventArgs e)
        {
            Pen borderPen = new Pen(Color.FromArgb(150, 0, 255, 0));

            if (completeContour != null)
            {
                foreach (Contour<Point> contour in saveContour)
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
    }
}
