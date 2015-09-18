using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;
using FANN.Net;

namespace CAPTCHA
{
    public partial class Form1 : Form
    {
        private OpenFileDialog ofd = new OpenFileDialog();
        public static NeuralNet neuralNet = new NeuralNet();
        private int recognizedCount = 0;
        private Stopwatch processTime = new Stopwatch();
        public static List<string[]> moments = new List<string[]>();
        private int otsuThreshold;
        private List<string> openedFiles = new List<string>();

        public Form1()
        {
            InitializeComponent();
            this.Menu = mainMenu1;
            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox2.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox3.SizeMode = PictureBoxSizeMode.AutoSize;
            textBox3.Text = trackBar1.Value.ToString();
            neuralNet.CreateFromFile("NeuralNet.ann");

            if (!Directory.Exists(@"History"))
                Directory.CreateDirectory(@"History");

            if (File.Exists("Moments.bin"))
            {
                using (StreamReader streamReader = new StreamReader("Moments.bin"))
                {
                    while (!streamReader.EndOfStream)
                    {
                        string[] moment = streamReader.ReadLine().Split(' ');
                        moments.Add(moment);
                    }
                }
            }

            if (!File.Exists("OpenedFiles.bin"))
                File.Create("OpenedFiles.bin");
            else
                using (StreamReader streamReader = new StreamReader("OpenedFiles.bin"))
                    while (!streamReader.EndOfStream)
                        openedFiles.Add(streamReader.ReadLine());
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
            if (File.Exists(textBox1.Text))
            {
                pictureBox1.Image = Image.FromFile(textBox1.Text);
              
                processTime.Restart();
                ProcessImage(pictureBox1.Image);
                processTime.Stop();
                TimeSpan processedTime = processTime.Elapsed;
                pictureBox1.Image = Image.FromFile(textBox1.Text);

                menuItem2.Enabled = true;
                menuItem3.Enabled = true;
                menuItem4.Enabled = true;

                toolStripStatusLabel1.Text = "Распознанных изображений: " + ++recognizedCount;
                toolStripStatusLabel2.Text = "Время: " + processedTime.TotalSeconds + " секунд";
            }
            else if (Web.Connection())
            {
                if (Web.IsValidUrl(textBox1.Text))
                {
                    try
                    {
                        pictureBox1.Load(textBox1.Text);

                        processTime.Restart();
                        ProcessImage(pictureBox1.Image);
                        processTime.Stop();
                        TimeSpan processedTime = processTime.Elapsed;

                        menuItem2.Enabled = true;
                        menuItem3.Enabled = true;
                        menuItem4.Enabled = true;

                        toolStripStatusLabel1.Text = "Распознанных изображений: " + ++recognizedCount;
                        toolStripStatusLabel2.Text = "Время: " + processedTime.TotalSeconds + " секунд";
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
            if (File.Exists(textBox1.Text))
            {
                if (!openedFiles.Contains(textBox1.Text))
                {
                    openedFiles.Add(textBox1.Text);

                    if (openedFiles.Count > 10)
                        openedFiles.RemoveAt(0);
                }
            }

            button2.Enabled = false;
            textBox2.Select(0, 0);
            Bitmap bmpStart = new Bitmap(imageStart);

            if ((bmpStart.Height != 70) && (bmpStart.Height != 18))
            {
                pictureBox1.Image = null;
                MessageBox.Show("Неверный тип капчи или изображение имеет недопустимый размер.", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                if (bmpStart.Height == 70)
                {
                    menuItem5.Enabled = false;

                    Bitmap bmpBlur = null;

                    if ((radioButton2.Checked) && ((radioButton5.Checked) || (radioButton6.Checked)))
                    {
                        MessageBox.Show("Выбранный фильтр будет доступен в следующих версиях программы.", "Предупреждение!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        radioButton3.Checked = true;
                    }

                    if ((radioButton1.Checked) || (radioButton3.Checked) || (radioButton7.Checked))
                        bmpBlur = ImageProcessor.GaussianBlur(bmpStart);
                    if ((radioButton4.Checked) && (radioButton2.Checked))
                        bmpBlur = ImageProcessor.MedianFilter(bmpStart);
                 
                    Bitmap bmpGrayScale = ImageProcessor.CropBounds(ImageProcessor.ToGrayScale(bmpBlur), 15, 7, 17, 9);
                    otsuThreshold = ImageProcessor.OtsuThreshold(bmpGrayScale);

                    try
                    {
                        Bitmap bmpBin = ImageProcessor.Binarization(bmpGrayScale, Threshold());
                        ShowBin.bmp = bmpBin;
                        Bitmap bmpCropped = ImageProcessor.DownAndRightCrop(bmpBin, true);
                        ShowBounds.bmp = ImageProcessor.DefineSymbolsBounds(bmpCropped, bmpBin, true);

                        pictureBox2.Image = ShowBin.bmp;
                        pictureBox3.Image = ShowBounds.bmp;

                        if (Form2.renewNeural)
                            neuralNet.CreateFromFile("NeuralNet.ann");

                        textBox2.Text = Neural.NeuralRecognize(ImageProcessor.segments);

                        SaveHistory((Bitmap)imageStart);
                    }
                    catch
                    {
                        pictureBox2.Image = ImageProcessor.WhiteRectangle(ShowBin.bmp);
                        pictureBox3.Image = pictureBox2.Image;
                        textBox2.Text = String.Empty;
                    }
                }
                else
                {
                    menuItem5.Enabled = true;

                    Bitmap bmpGrayScale = ImageProcessor.CropBounds(ImageProcessor.ToGrayScale(bmpStart), 2, 2, 4, 4);
                    otsuThreshold = ImageProcessor.OtsuThreshold(bmpGrayScale);

                    try
                    {
                        Bitmap bmpBin = ImageProcessor.Binarization(bmpGrayScale, Threshold());
                        ShowBin.bmp = bmpBin;
                        Bitmap bmpLeftCrop = ImageProcessor.CropBounds((Bitmap)(Image)bmpBin.Clone(), 4, 0, 4, 0);
                        ShowBounds.bmp = ImageProcessor.DefineSymbolsBounds(bmpLeftCrop, bmpLeftCrop, false);

                        if ((ImageProcessor.segments.Count == 1) && (radioButton1.Checked))
                        {
                            Bitmap bmpTemp = (Bitmap)pictureBox1.Image;
                            bmpGrayScale = ImageProcessor.CropBounds(ImageProcessor.ToGrayScale(bmpTemp), 2, 2, 4, 4);
                            bmpTemp = ImageProcessor.Binarization(bmpGrayScale, 120);
                            ShowBin.bmp = bmpTemp;
                            ShowBounds.bmp = ImageProcessor.DefineSymbolsBounds(bmpTemp, bmpTemp, false);
                        }

                        pictureBox2.Image = ShowBin.bmp;
                        pictureBox3.Image = ShowBounds.bmp;

                        if (Form5.renewMoments)
                        {
                            moments.Clear();

                            if (File.Exists("Moments.bin"))
                            {
                                using (StreamReader streamReader = new StreamReader("Moments.bin"))
                                {
                                    while (!streamReader.EndOfStream)
                                    {
                                        string[] moment = streamReader.ReadLine().Split(' ');
                                        moments.Add(moment);
                                    }
                                }
                            }

                            Form5.renewMoments = false;
                        }

                        textBox2.Text = Contour.ContourRecognize(ImageProcessor.segments);

                        SaveHistory((Bitmap)imageStart);
                    }
                    catch
                    {
                        pictureBox2.Image = ImageProcessor.WhiteRectangle(ShowBin.bmp);
                        pictureBox3.Image = pictureBox2.Image;
                        textBox2.Text = String.Empty;
                    }
                }
            }

            button2.Enabled = true;
            button2.Select();
        }

        private int Threshold()
        {
            int threshold = 0;

            if ((checkBox1.Checked) && (!radioButton1.Checked))
            {
                textBox3.Text = otsuThreshold.ToString();
                trackBar1.Value = otsuThreshold;
            }

            if (radioButton7.Checked)
            {
                int value = (int)(numericUpDown1.Value * otsuThreshold + numericUpDown2.Value);

                if (value > 255)
                    value = 255;

                if (value < 0)
                    value = 0;

                trackBar1.Value = value;
                textBox3.Text = value.ToString();
            }

            if (radioButton1.Checked)
                threshold = otsuThreshold;
            if ((radioButton2.Checked) || (radioButton7.Checked))
                threshold = int.Parse(textBox3.Text);

            return threshold;
        }

        private void SaveHistory(Bitmap bmpStart)
        {
            bool save = true;

            DirectoryInfo di = new DirectoryInfo(@"History");
            FileInfo[] bmpFiles = di.GetFiles("*.png");

            if (bmpFiles.Length >= 10)
            {
                DateTime oldestCreation = bmpFiles[0].CreationTime;

                for (int i = 1; i < bmpFiles.Length; i++)
                    if (DateTime.Compare(bmpFiles[i].CreationTime, oldestCreation) < 0)
                        oldestCreation = bmpFiles[i].CreationTime;

                save = true;

                for (int i = 0; i < bmpFiles.Length; i++)
                {
                    if (DateTime.Compare(oldestCreation, bmpFiles[i].CreationTime) == 0)
                    {
                        try
                        {
                            File.Delete(bmpFiles[i].FullName);
                        }
                        catch (Exception)
                        {
                            save = false;
                            MessageBox.Show("Данное изображение не будет сохранено в историю.", "Предупреждение!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }

                        break;
                    }
                }
            }

            if ((!String.IsNullOrEmpty(textBox2.Text) && (save)) & (!File.Exists(@"History\\" + textBox2.Text + ".png")))
                bmpStart.Save(@"History\\" + textBox2.Text + ".png");
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text.Length == 0)
                button2.Enabled = false;
            else
                button2.Enabled = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            textBox3.Text = trackBar1.Value.ToString();
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            radioButton3.Enabled = false;
            radioButton4.Enabled = false;
            radioButton5.Enabled = false;
            radioButton6.Enabled = false;
            trackBar1.Enabled = false;
            textBox3.Enabled = false;
            checkBox1.Enabled = false;
            numericUpDown1.Enabled = false;
            numericUpDown2.Enabled = false;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            radioButton3.Enabled = true;
            radioButton4.Enabled = true;
            radioButton5.Enabled = true;
            radioButton6.Enabled = true;
            trackBar1.Enabled = true;
            textBox3.Enabled = true;
            checkBox1.Enabled = true;
            numericUpDown1.Enabled = false;
            numericUpDown2.Enabled = false;
        }

        private void radioButton7_CheckedChanged(object sender, EventArgs e)
        {
            radioButton3.Enabled = false;
            radioButton4.Enabled = false;
            radioButton5.Enabled = false;
            radioButton6.Enabled = false;
            trackBar1.Enabled = false;
            textBox3.Enabled = true;
            checkBox1.Enabled = false;
            numericUpDown1.Enabled = true;
            numericUpDown2.Enabled = true;
        }

        private void menuItem2_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();

            sfd.Title = "Cохранить";
            sfd.Filter = "Все файлы изображений|*.jpg;*.jpeg;*.png;*.bmp";
            sfd.Filter += "|JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG (*.png)|*.png|Точечный рисунок (*.bmp)|*.bmp";
            sfd.AddExtension = true;
            sfd.DefaultExt = ".png";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                Bitmap bmpTemp = (Bitmap)pictureBox1.Image;

                if (bmpTemp != null)
                    bmpTemp.Save(sfd.FileName);
                else
                    MessageBox.Show("Изображение отсутствует.", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void menuItem3_Click(object sender, EventArgs e)
        {
            Form3 f3 = new Form3();
            f3.ShowDialog();
        }

        private void menuItem4_Click(object sender, EventArgs e)
        {
            Form4 f4 = new Form4();
            f4.ShowDialog();
        }

        private void menuItem5_Click(object sender, EventArgs e)
        {
            Form8 f8 = new Form8();
            f8.ShowDialog();
        }

        private void menuItem7_Click(object sender, EventArgs e)
        {
            Form2 f2 = new Form2();
            f2.ShowDialog();
        }

        private void menuItem8_Click(object sender, EventArgs e)
        {
            Form5 f5 = new Form5();
            f5.ShowDialog();
        }

        private void menuItem10_Click(object sender, EventArgs e)
        {
            Form6 f6 = new Form6();
            f6.ShowDialog();
        }

        private void menuItem11_Click(object sender, EventArgs e)
        {
            Form7 f7 = new Form7();
            f7.ShowDialog();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            contextMenuStrip1.Show(button4, new Point(0, button4.Height));
        }

        private void dMTToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBox1.Text = "http://dmtsoft.ru/captcha/dmtMultiCaptcha_v1.0/index.php?PHPSESSID=9fd3951fb2e3616b55fe67454faac021";
        }

        private void siteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBox1.Text = "http://www.sourcecodeprojects.com/authImage";
        }

        private void button5_Click(object sender, EventArgs e)
        {
            contextMenuStrip2.Items.Clear(); 
        
            foreach (string file in openedFiles)
                contextMenuStrip2.Items.Add(file);
            
            contextMenuStrip2.Show(button5, new Point(0, button5.Height));
        }

        private void contextMenuStrip2_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            textBox1.Text = e.ClickedItem.Text;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            using (StreamWriter streamWriter = new StreamWriter("OpenedFiles.bin", false))
            {
                foreach (string file in openedFiles)
                    streamWriter.WriteLine(file);
            }
        }
    }
}