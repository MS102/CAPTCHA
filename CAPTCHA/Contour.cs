using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;

namespace CAPTCHA
{
    public class Contour
    {
        public static string ContourRecognize(List<Bitmap> segments)
        {
            string result = String.Empty;

            for (int i = 0; i < segments.Count; i++)
            {
                Bitmap bmp = segments[i];

                Image<Gray, byte> segment = new Image<Gray, byte>(bmp);
                Contour<Point> foundContours = segment.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_NONE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST);

                List<Contour<Point>> completeContour = FilterContours(foundContours);

                if ((completeContour.Count == 1) && (completeContour[0].Perimeter < 10))
                    continue;

                double contourMoment = 0;

                foreach (Contour<Point> contour in completeContour)
                {
                    MCvMoments moments = contour.GetMoments();
                    contourMoment += moments.m00 + moments.m01;
                }

                int index = 0;
                double delta = contourMoment;

                for (int k = 0; k < Form1.moments.Count; k++)
                {
                    double moment = double.Parse(Form1.moments[k][1]);

                    if (Math.Abs(contourMoment - moment) < delta)
                    {
                        delta = Math.Abs(contourMoment - moment);
                        index = k;
                    }
                }

                result += Form1.moments[index][0];
            }

            return result;
        }

        private static List<Contour<Point>> FilterContours(Contour<Point> foundContours)
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
    }
}
