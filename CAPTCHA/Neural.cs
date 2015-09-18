using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace CAPTCHA
{
    public class Neural
    {
        public static string NeuralRecognize(List<Bitmap> segments)
        {
            string result = String.Empty;

            for (int i = 0; i < segments.Count; i++)
            {
                Bitmap segment = segments[i];
                segment = ImageProcessor.ChangeSize(segment, 50, 60, 0, 0);
                segment = ImageProcessor.Resize(segment, 20, 24);
                segment = ImageProcessor.Binarization(segment, ImageProcessor.OtsuThreshold(segment));
                double[] input = ImageProcessor.GetNumericView(segment, 480);

                double[] output = Form1.neuralNet.Run(input);
                double maxWeight = output[0];
                int maxIndex = 0;

                for (int k = 0; k < output.Length; k++)
                {
                    if (output[k] > maxWeight)
                    {
                        maxIndex = k;
                        maxWeight = output[k];
                    }
                }

                result += maxIndex + 1;
            }

            return result;
        }
    }
}
