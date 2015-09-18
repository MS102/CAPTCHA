using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;
using FANN.Net;

namespace CAPTCHA
{
    public partial class Form2 : Form
    {
        private OpenFileDialog ofd = new OpenFileDialog();
        public static bool renewNeural = false;

        public Form2()
        {
            InitializeComponent();
            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;

            this.Height = 94;

            if (!Directory.Exists(@"TrainNeural"))
            {
                Directory.CreateDirectory(@"TrainNeural");

                for (int i = 1; i < 10; i++)
                    Directory.CreateDirectory(@"TrainNeural\\" + i.ToString());
            }
        }

        private void button2_Click(object sender, EventArgs e)
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

        private void button3_Click(object sender, EventArgs e)
        {
            if (File.Exists(textBox1.Text))
            {
                pictureBox1.Image = Image.FromFile(textBox1.Text);
                ProcessImage(pictureBox1.Image);
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

            if (bmpStart.Height != 70)
            {
                pictureBox1.Image = null;
                MessageBox.Show("Неверный тип капчи или изображение имеет недопустимый размер.", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                Bitmap bmpBlur = ImageProcessor.GaussianBlur(bmpStart);
                Bitmap bmpGrayScale = ImageProcessor.CropBounds(ImageProcessor.ToGrayScale(bmpBlur), 15, 7, 17, 9);
                int otsuThreshold = ImageProcessor.OtsuThreshold(bmpGrayScale);
                Bitmap bmpBin = ImageProcessor.Binarization(bmpGrayScale, otsuThreshold);
                Bitmap bmpCropped = ImageProcessor.DownAndRightCrop(bmpBin, true);
                ImageProcessor.DefineSymbolsBounds(bmpCropped, bmpBin, true);

                for (int i = 0; i < ImageProcessor.segments.Count; i++)
                {
                    Bitmap segment = ImageProcessor.segments[i];
                    segment = ImageProcessor.ChangeSize(segment, 50, 60, 0, 0);
                    ImageProcessor.segments[i] = segment;
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string str = textBox2.Text; 

            if (ImageProcessor.segments.Count == str.Length)
            {
                bool success = true;

                try
                {
                    int.Parse(str);

                    for (int i = 0; i < ImageProcessor.segments.Count; i++)
                    {
                        Bitmap bmp = ImageProcessor.segments[i];
                        bmp = ImageProcessor.Resize(bmp, 20, 24);

                        bmp.Save(@"TrainNeural\\" + str[i] + @"\\" + DateTime.Now.Ticks.ToString() + ".png");
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Строка должна содержать только числа.", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    success = false;
                }

                if (success) 
                  textBox2.Clear();

                if ((success) && (Web.IsValidUrl(textBox1.Text)))
                    button3_Click(button3, null);
            }
            else
                MessageBox.Show("Количество введенных символов не совпадает с количеством выделенных сегментов.", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text.Length == 0)
                button3.Enabled = false;
            else
                button3.Enabled = true;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (textBox2.Text.Length == 0)
                button1.Enabled = false;
            else if (pictureBox1.Image != null)
                button1.Enabled = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            int fileCount = 0;

            for (int i = 1; i < 10; i++)
            {
                DirectoryInfo di = new DirectoryInfo(@"TrainNeural\\" + i.ToString());
                FileInfo[] bmpFiles = di.GetFiles("*.png");
                fileCount += bmpFiles.Length;
            }

            double[,] input = new double[fileCount, 480];
            double[,] output = new double[fileCount, 10];

            List<string> hashes = new List<string>();
            int count = 0;

            for (int i = 1; i < 10; i++)
            {
                DirectoryInfo di = new DirectoryInfo(@"TrainNeural\\" + i.ToString());
                FileInfo[] bmpFiles = di.GetFiles("*.png");

                foreach (FileInfo fi in bmpFiles)
                {
                    string hash = FileHashSum(fi.FullName);

                    if (hashes.Contains(hash))
                        continue;

                    hashes.Add(hash);

                    Bitmap bmp = new Bitmap(fi.FullName);

                    bmp = ImageProcessor.Binarization(bmp, ImageProcessor.OtsuThreshold(bmp));
                    ImageProcessor.GetNumericView(bmp, ref input, count);

                    for (int j = 1; j < 10; j++)
                    {
                        if (j == i)
                            output[count, j - 1] = 1;
                        else
                            output[count, j - 1] = 0;
                    }

                    count++;
                }
            }

            if (File.Exists("TrainingData.tr"))
                File.Delete("TrainingData.tr");

            string fillTrainFile = count.ToString() + " 480 9" + Environment.NewLine;

            for (int i = 0; i < count; i++)
            {
                for (int x = 0; x < 480; x++)
                {
                    fillTrainFile += input[i, x].ToString();
                    if (x < 479)
                        fillTrainFile += " ";
                    else
                        fillTrainFile += Environment.NewLine;
                }

                for (int x = 0; x < 9; x++)
                {
                    fillTrainFile += output[i, x].ToString();
                    if (x < 8) 
                       fillTrainFile += " ";
                    else
                        fillTrainFile += Environment.NewLine;
                }

                if (i % 40 == 0)
                {
                    File.AppendAllText("TrainingData.tr", fillTrainFile);
                    fillTrainFile = String.Empty;
                }
            }

            File.AppendAllText("TrainingData.tr", fillTrainFile);

            NeuralNet neuralNet = new NeuralNet();

            uint[] layers = { 480, 190, 9 };
            neuralNet.CreateStandardArray(layers);

            neuralNet.RandomizeWeights(-0.1, 0.1);
            neuralNet.SetLearningRate(0.7f);

            TrainingData trainingData = new TrainingData();
            trainingData.ReadTrainFromFile("TrainingData.tr");

            switch (comboBox1.SelectedIndex)
            {
                case 0:
                    neuralNet.TrainOnData(trainingData, 1000, 0, 0.1f);
                    break;
                case 1:
                    neuralNet.TrainOnData(trainingData, 1000, 0, 0.05f);
                    break;
                case 2:
                    neuralNet.TrainOnData(trainingData, 1000, 0, 0.01f);
                    break;
                case 3:
                    neuralNet.TrainOnData(trainingData, 1000, 0, 0.005f);
                    break;
                case 4:
                    neuralNet.TrainOnData(trainingData, 1000, 0, 0.001f);
                    break;
            }

            neuralNet.Save("NeuralNet.ann");
            
            renewNeural = true;
        }

        private string FileHashSum(string path)
        {
            using (FileStream fs = File.OpenRead(path))
            {
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] buffer = new byte[fs.Length];
                fs.Read(buffer, 0, (int)fs.Length);
                byte[] hashSumByte = md5.ComputeHash(buffer);
                string hashSum = BitConverter.ToString(hashSumByte).Replace("-", String.Empty);

                return hashSum;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                button1.Visible = true;
                button2.Visible = true;
                button3.Visible = true;
                textBox1.Visible = true;
                textBox2.Visible = true;
                pictureBox1.Visible = true;
                this.Height = 265;
            }
            else
            {
                button1.Visible = false;
                button2.Visible = false;
                button3.Visible = false;
                textBox1.Visible = false;
                textBox2.Visible = false;
                pictureBox1.Visible = false;
                this.Height = 94;
            }
        }
    }
}
