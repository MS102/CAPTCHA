using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace CAPTCHA
{
    public partial class Form6 : Form
    {
        public Form6()
        {
            InitializeComponent();
        }

        private void Form6_Load(object sender, EventArgs e)
        {
            DirectoryInfo di = new DirectoryInfo(@"History");
            FileInfo[] bmpFiles = di.GetFiles("*.png");

            foreach (FileInfo fi in bmpFiles)
                listBox1.Items.Add(fi.CreationTime);

            listBox1.SetSelected(0, true);

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            DirectoryInfo di = new DirectoryInfo(@"History");
            FileInfo[] bmpFiles = di.GetFiles("*.png");

            for (int i = 0; i < bmpFiles.Length; i++)
            {
                if (DateTime.Compare((DateTime)listBox1.SelectedItem, bmpFiles[i].CreationTime) == 0)
                {
                    pictureBox1.Image = Image.FromFile(bmpFiles[i].FullName);
                    textBox1.Text = bmpFiles[i].Name.Replace(bmpFiles[i].Extension, String.Empty);

                    break;
                }
            }
        }
    }
}
