using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;

namespace CAPTCHA
{
    public class ImageProcessor
    {
        public static int symbolsCount = 0;
        public static List<Bitmap> segments = new List<Bitmap>();

        public static Bitmap GaussianBlur(Bitmap bmp)
        {
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int width = bmpData.Stride;
            byte[] pixelsByte = new byte[width * bmpData.Height];
            byte[] filtered = new byte[width * bmpData.Height];

            Marshal.Copy(bmpData.Scan0, pixelsByte, 0, pixelsByte.Length);
            bmp.UnlockBits(bmpData);

            List<string> colorChars = new List<string>();
            colorChars.Add("R");
            colorChars.Add("G");
            colorChars.Add("B");

            Dictionary<string, double> colorDoubles = new Dictionary<string, double>();

            foreach (string color in colorChars)
                colorDoubles.Add(color, 0);

            double[,] matrix = new double[3, 3] { { 1, 2, 1, }, 
                                                  { 2, 4, 2, }, 
                                                  { 1, 2, 1, }, };

            int kernel = 3;
            int k = (kernel - 1) / 2;

            for (int x = k; x < bmp.Width - k; x++)
            {
                for (int y = k; y < bmp.Height - k; y++)
                {
                    foreach (string color in colorChars)
                        colorDoubles[color] = 0;

                    for (int fX = -k; fX <= k; fX++)
                    {
                        for (int fY = -k; fY <= k; fY++)
                        {
                            foreach (string color in colorChars)
                                colorDoubles[color] += (double)(pixelsByte[x * 3 + y * width + fX * 3 + fY * width]) * matrix[fX + k, fY + k];
                        }
                    }

                    foreach (string color in colorChars)
                    {
                        colorDoubles[color] = colorDoubles[color] * 1.0 / 16.0;

                        if (colorDoubles[color] > 255)
                            colorDoubles[color] = 255;

                        if (colorDoubles[color] < 0)
                            colorDoubles[color] = 0;
                    }

                    filtered[x * 3 + y * width] = (byte)(colorDoubles["B"]);
                    filtered[x * 3 + y * width + 1] = (byte)(colorDoubles["G"]);
                    filtered[x * 3 + y * width + 2] = (byte)(colorDoubles["R"]);
                }
            }

            Bitmap bmpFiltered = new Bitmap(bmp.Width, bmp.Height);
            BitmapData bmpFilteredData = bmpFiltered.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            Marshal.Copy(filtered, 0, bmpFilteredData.Scan0, filtered.Length);
            bmpFiltered.UnlockBits(bmpFilteredData);

            return bmpFiltered;
        }

        public static Bitmap MedianFilter(Bitmap bmp)
        {
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            int width = bmpData.Stride;
            byte[] pixelsByte = new byte[width * bmpData.Height];
            byte[] filtered = new byte[width * bmpData.Height];

            Marshal.Copy(bmpData.Scan0, pixelsByte, 0, pixelsByte.Length);
            bmp.UnlockBits(bmpData);

            int kernel = 3;
            int k = (kernel - 1) / 2;

            for (int x = k; x < bmp.Height - k; x++)
            {
                for (int y = k; y < bmp.Width - k; y++)
                {
                    List<int> nghbrsPixelList = new List<int>();

                    for (int nX = -k; nX < k + 1; nX++)
                        for (int nY = -k; nY < k + 1; nY++)
                            nghbrsPixelList.Add(BitConverter.ToInt32(pixelsByte, x * width + y * 4 + nX * width + nY * 4));

                    nghbrsPixelList.Sort();

                    byte[] midPixel = BitConverter.GetBytes(nghbrsPixelList[k]);

                    for (int i = 0; i < 4; i++)
                        filtered[x * width + y * 4 + i] = midPixel[i];
                }
            }

            Bitmap bmpFiltered = new Bitmap(bmp.Width, bmp.Height);
            BitmapData bmpFilteredData = bmpFiltered.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            Marshal.Copy(filtered, 0, bmpFilteredData.Scan0, filtered.Length);
            bmpFiltered.UnlockBits(bmpFilteredData);

            return bmpFiltered;
        }

        public static Bitmap CropBounds(Bitmap bmp, int x, int y, int offsetWidth, int offsetHeight)
        {
            Rectangle selection = new Rectangle(x, y, bmp.Width - offsetWidth, bmp.Height - offsetHeight);

            Bitmap bmpCropped = bmp.Clone(selection, bmp.PixelFormat);

            return bmpCropped;
        }

        public static Bitmap ToGrayScale(Bitmap bmp)
        {
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int width = bmpData.Stride;
            byte[] pixelsByte = new byte[width * bmpData.Height];

            Marshal.Copy(bmpData.Scan0, pixelsByte, 0, pixelsByte.Length);

            for (int i = 0; i < pixelsByte.Length; i += 3)
            {
                byte grayScale = (byte)(0.299 * pixelsByte[i + 2] + 0.587 * pixelsByte[i + 1] + 0.114 * pixelsByte[i]);
                ChangeColor(ref pixelsByte, i, grayScale, grayScale, grayScale);
            }

            Marshal.Copy(pixelsByte, 0, bmpData.Scan0, pixelsByte.Length);
            bmp.UnlockBits(bmpData);

            return bmp;
        }

        public static void ChangeColor(ref byte[] pixelsByte, int index, byte r, byte g, byte b)
        {
            pixelsByte[index] = b;
            pixelsByte[index + 1] = g;
            pixelsByte[index + 2] = r;
        }

        public static int OtsuThreshold(Bitmap bmp)
        {
            List<int> histogram = new List<int>();

            for (int i = 0; i < 256; i++)
                histogram.Add(0);

            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int width = bmpData.Stride;
            byte[] pixelsByte = new byte[width * bmpData.Height];

            Marshal.Copy(bmpData.Scan0, pixelsByte, 0, pixelsByte.Length);
            bmp.UnlockBits(bmpData);

            for (int j = 0; j < bmp.Height; j++)
            {
                for (int i = 0; i < bmp.Width * 3; i += 3)
                {
                    int index = i + j * width;
                    histogram[pixelsByte[index]]++;
                }
            }

            int m = 0;
            int n = 0;

            for (int i = 0; i < 256; i++)
            {
                m += i * histogram[i];
                n += histogram[i];
            }

            float maxSigma = -1;
            int threshold = 0;

            float alpha = 0;
            float beta = 0;

            for (int i = 0; i < 256; i++)
            {
                alpha += i * histogram[i];
                beta += histogram[i];

                float w = beta / n;

                float a = alpha / beta - (float)(m - alpha) / (n - beta);

                float sigma = w * (1 - w) * a * a;

                if (sigma > maxSigma)
                {
                    maxSigma = sigma;
                    threshold = i;
                }
            }

            return threshold;
        }

        public static Bitmap Binarization(Bitmap bmp, int threshold)
        {
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int width = bmpData.Stride;
            byte[] pixelsByte = new byte[width * bmpData.Height];

            Marshal.Copy(bmpData.Scan0, pixelsByte, 0, pixelsByte.Length);

            for (int i = 0; i < pixelsByte.Length; i += 3)
            {
                if (pixelsByte[i] > threshold)
                    ChangeColor(ref pixelsByte, i, 255, 255, 255);
                else
                    ChangeColor(ref pixelsByte, i, 0, 0, 0);
            }

            Marshal.Copy(pixelsByte, 0, bmpData.Scan0, pixelsByte.Length);
            bmp.UnlockBits(bmpData);

            return bmp;
        }

        public static Bitmap DownAndRightCrop(Bitmap bmp, bool downCrop)
        {
            Bitmap bmpCropped = bmp;

            if (downCrop)
            {
                int horisontBound = (int)(bmp.Height / 5.0);
                bmpCropped = CropBounds(bmp, 0, 0, 0, horisontBound);
            }

            int verticalBound = (int)Math.Ceiling((bmp.Width / 4.0));
            Bitmap bmpRightQuarter = CropBounds(bmp, 3 * verticalBound, 0, 3 * verticalBound, 0);

            Rectangle rect = new Rectangle(0, 0, bmpRightQuarter.Width, bmpRightQuarter.Height);
            BitmapData bmpData = bmpRightQuarter.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int width = bmpData.Stride;
            byte[] pixelsByte = new byte[width * bmpData.Height];

            Marshal.Copy(bmpData.Scan0, pixelsByte, 0, pixelsByte.Length);

            int blackPixelsCount = 0;

            for (int i = 0; i < pixelsByte.Length; i += 3)
            {
                if (pixelsByte[i] == 0)
                    blackPixelsCount++;
            }

            Marshal.Copy(pixelsByte, 0, bmpData.Scan0, pixelsByte.Length);
            bmpRightQuarter.UnlockBits(bmpData);

            if (blackPixelsCount < 250)
            {
                symbolsCount = 3;

                switch (blackPixelsCount)
                {
                    case 0:
                        bmpCropped = CropBounds(bmpCropped, 0, 0, verticalBound, 0);
                        break;
                    default:
                        bmpCropped = CropBounds(bmpCropped, 0, 0, verticalBound - 3, 0);
                        break;
                }
            }
            else
                symbolsCount = 4;

            return bmpCropped;
        }

        public static Bitmap DefineSymbolsBounds(Bitmap bmp, Bitmap bmpBin, bool typeOne)
        {
            List<int> blackInLines = new List<int>();

            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int width = bmpData.Stride;
            byte[] pixelsByte = new byte[width * bmpData.Height];

            Marshal.Copy(bmpData.Scan0, pixelsByte, 0, pixelsByte.Length);

            for (int i = 0; i < bmp.Width * 3; i += 3)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    int index = i + j * width;

                    if (pixelsByte[index] == 0)
                    {
                        if (!blackInLines.Contains((int)(i / 3)))
                            blackInLines.Add((int)(i / 3));
                    }
                }
            }

            bmp.UnlockBits(bmpData);

            if (typeOne)
                bmpBin = CropBounds(bmpBin, 0, 0, bmpBin.Width - bmp.Width, 0);
            else
                bmpBin = ChangeSize(bmp, bmp.Width, bmp.Height + 1, 0, 0);

            rect = new Rectangle(0, 0, bmpBin.Width, bmpBin.Height);
            bmpData = bmpBin.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            width = bmpData.Stride;
            pixelsByte = new byte[width * bmpData.Height];

            Marshal.Copy(bmpData.Scan0, pixelsByte, 0, pixelsByte.Length);

            List<int> areaWidth = new List<int>();
            List<int> startX = new List<int>();
            List<int> endX = new List<int>();
            int length = 0;

            for (int i = 0; i < blackInLines.Count - 1; i++)
            {
                if (blackInLines[i] == blackInLines[i + 1] - 1)
                {
                    length++;
                }
                else
                {
                    areaWidth.Add(++length);
                    length = 0;
                }
            }

            areaWidth.Add(++length);

            startX.Add(blackInLines[0]);
            int k = 0;

            foreach (int item in areaWidth)
            {
                int endOf = startX[startX.Count - 1] + item - 1;
                endX.Add(endOf);
                k++;

                if (k < areaWidth.Count)
                    startX.Add(blackInLines[blackInLines.IndexOf(endOf) + 1]);
            }

            if (startX[0] == 0)
                startX[0]++;

            for (int i = 0; i < areaWidth.Count; i++)
            {
                startX[i]--;
                endX[i]++;
            }

            if (endX[endX.Count - 1] == bmp.Width)
                endX[endX.Count - 1]--;

            if (typeOne)
            {
                int delta = bmpBin.Height - bmp.Height;
                List<int> boundY = DrawBounds(bmpBin.Width, bmpBin.Height, delta, width, ref pixelsByte, ref areaWidth, ref startX, ref endX);

                Marshal.Copy(pixelsByte, 0, bmpData.Scan0, pixelsByte.Length);
                bmpBin.UnlockBits(bmpData);

                if (symbolsCount == 4)
                    bmpBin = DownAndRightCrop(bmpBin, false);

                if (boundY[boundY.Count - 1] > 40)
                {
                    bmpBin = GetSegemnt(bmpBin, 0, 0, startX[startX.Count - 1], bmpBin.Height - 1);
                    bmpBin = GetSegemnt(bmpBin, 0, 0, bmpBin.Width - 1, bmpBin.Height - 1);
                    boundY.RemoveAt(boundY.Count - 1);
                    startX.RemoveAt(startX.Count - 1);
                    endX.RemoveAt(endX.Count - 1);
                    areaWidth.RemoveAt(areaWidth.Count - 1);
                    symbolsCount = 3;
                }

                segments.Clear();

                for (int i = 0; i < areaWidth.Count; i++)
                    segments.Add(GetSegemnt(bmpBin, startX[i] + 1, boundY[i] + 1, endX[i] - 1, (bmpBin.Height - 2)));

                List<Bitmap> container = new List<Bitmap>();

                if (symbolsCount != areaWidth.Count)
                {
                    for (int i = 0; i < areaWidth.Count; i++)
                    {
                        if ((areaWidth[i] > 60) && (areaWidth[i] < 95))
                        {
                            int boundX = startX[i] + AnalyseSegment(segments[i], delta);
                            DrawBoundInGlued(ref bmpBin, boundX, startX[i], endX[i], boundY[i]);

                            if (i != areaWidth.Count - 1)
                                if (endX[i] == startX[i + 1])
                                    DrawVerticalBound(ref bmpBin, endX[i], boundY[i]);

                            if (i != 0)
                                if (startX[i] == endX[i - 1])
                                    DrawVerticalBound(ref bmpBin, startX[i], boundY[i]);

                            if (symbolsCount == 3)
                            {
                                if (i == 0)
                                {
                                    CropGluedSegments(bmpBin, startX[i] + 1, endX[i] - 1, ref container, true, false);
                                    container.Add(segments[1]);
                                }
                                else
                                {
                                    container.Add(segments[0]);
                                    CropGluedSegments(bmpBin, startX[i] + 1, endX[i] - 1, ref container, true, false);
                                }
                            }
                            else
                            {
                                if (segments.Count == 2)
                                    CropGluedSegments(bmpBin, startX[i] + 1, endX[i] - 1, ref container, true, false);
                                else
                                {
                                    if (i == 0)
                                    {
                                        CropGluedSegments(bmpBin, startX[i] + 1, endX[i] - 1, ref container, true, false);
                                        container.Add(segments[1]);
                                        container.Add(segments[2]);
                                    }
                                    else if (i == 1)
                                    {
                                        container.Add(segments[0]);
                                        CropGluedSegments(bmpBin, startX[i] + 1, endX[i] - 1, ref container, true, false);
                                        container.Add(segments[2]);
                                    }
                                    else
                                    {
                                        container.Add(segments[0]);
                                        container.Add(segments[1]);
                                        CropGluedSegments(bmpBin, startX[i] + 1, endX[i] - 1, ref container, true, false);
                                    }
                                }
                            }
                        }

                        if ((areaWidth[i] >= 95) && (areaWidth[i] < 140))
                        {
                            Bitmap halfSegment = GetSegemnt(segments[i], 0, 0, segments[i].Width / 2, segments[i].Height - 1);
                            int boundX = startX[i] + AnalyseSegment(halfSegment, delta);

                            DrawBoundInGlued(ref bmpBin, boundX, startX[i], endX[i], boundY[i]);

                            halfSegment = GetSegemnt(segments[i], segments[i].Width / 2, 0, segments[i].Width - 1, segments[i].Height - 1);
                            boundX = startX[i] + AnalyseSegment(halfSegment, delta);

                            DrawBoundInGlued(ref bmpBin, boundX + halfSegment.Width, startX[i], endX[i], boundY[i]);

                            if (symbolsCount == 3)
                                CropGluedSegments(bmpBin, startX[i] + 1, endX[i] - 1, ref container, false, true);
                            else
                            {
                                if (i == 0)
                                {
                                    CropGluedSegments(bmpBin, startX[i] + 1, endX[i] - 1, ref container, false, true);
                                    container.Add(segments[1]);
                                }
                                else
                                {
                                    container.Add(segments[0]);
                                    CropGluedSegments(bmpBin, startX[i] + 1, endX[i] - 1, ref container, false, true);
                                }
                            }
                        }

                        if (areaWidth[i] >= 140)
                        {
                            List<int> boundList = new List<int>();
                            Bitmap thirdOfSegment = GetSegemnt(segments[i], 0, 0, segments[i].Width / 3, segments[i].Height - 1);
                            int boundX = startX[i] + AnalyseSegment(thirdOfSegment, delta);
                            boundList.Add(boundX - startX[i]);

                            DrawBoundInGlued(ref bmpBin, boundX, startX[i], endX[i], boundY[i]);

                            thirdOfSegment = GetSegemnt(segments[i], segments[i].Width / 3, 0, 2 * segments[i].Width / 3, segments[i].Height - 1);
                            boundX = startX[i] + AnalyseSegment(thirdOfSegment, delta);
                            boundList.Add(boundX - startX[i]);

                            DrawBoundInGlued(ref bmpBin, boundX + thirdOfSegment.Width, startX[i], endX[i], boundY[i]);

                            thirdOfSegment = GetSegemnt(segments[i], 2 * segments[i].Width / 3, 0, segments[i].Width - 1, segments[i].Height - 1);
                            boundX = startX[i] + AnalyseSegment(thirdOfSegment, delta);
                            boundList.Add(boundX - startX[i]);

                            DrawBoundInGlued(ref bmpBin, boundX + 2 * thirdOfSegment.Width - 1, startX[i], endX[i], boundY[i]);

                            CropGluedSegments(bmpBin, startX[i] + 1, endX[i] - 1, ref container, false, false);
                        }
                    }

                    segments.Clear();

                    foreach (Bitmap segment in container)
                        segments.Add(segment);

                    container.Clear();

                    for (int i = 0; i < segments.Count; i++)
                    {
                        Bitmap segment = segments[i];
                        CheckColors(ref segment);
                        segments[i] = segment;
                    }
                }

                return bmpBin;
            }
            else
            {
                DrawAllBounds(bmpBin.Height, width, ref pixelsByte, areaWidth, startX, endX);

                Marshal.Copy(pixelsByte, 0, bmpData.Scan0, pixelsByte.Length);
                bmpBin.UnlockBits(bmpData);

                segments.Clear();

                for (int i = 0; i < areaWidth.Count; i++)
                    segments.Add(GetSegemnt(bmpBin, startX[i] + 1, 1, endX[i] - 1, (bmpBin.Height - 2)));

                for (int i = 0; i < segments.Count; i++)
                    segments[i] = ChangeSize(segments[i], segments[i].Width + 8, segments[i].Height + 8, 4, 4);

                return bmpBin;
            }
        }

        public static Bitmap ChangeSize(Bitmap bmpSource, int newWidth, int newHeight, int offsetX, int offsetY)
        {
            Image bmpDest = new Bitmap(newWidth, newHeight);

            using (Graphics g = Graphics.FromImage(bmpDest))
            {
                g.FillRectangle(Brushes.White, 0, 0, newWidth, newHeight);
                g.DrawImage((Image)bmpSource, offsetX, offsetY);
            }

            return (Bitmap)bmpDest;
        }

        public static Bitmap Resize(Bitmap bmpSource, int newWidth, int newHeight)
        {
            Image bmpDest = new Bitmap(newWidth, newHeight);

            using (Graphics g = Graphics.FromImage(bmpDest))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(bmpSource, new Rectangle(0, 0, newWidth, newHeight));
            }

            return (Bitmap)bmpDest;
        }

        public static void CheckColors(ref Bitmap segment)
        {
            Rectangle rect = new Rectangle(0, 0, segment.Width, segment.Height);
            BitmapData bmpData = segment.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int width = bmpData.Stride;
            byte[] pixelsByte = new byte[width * bmpData.Height];

            Marshal.Copy(bmpData.Scan0, pixelsByte, 0, pixelsByte.Length);

            for (int i = 0; i < segment.Width; i++)
            {
                for (int j = 0; j < segment.Height; j++)
                {
                    int index = i * 3 + j * width;

                    if (pixelsByte[index] != pixelsByte[index + 2])
                        ChangeColor(ref pixelsByte, index, 255, 255, 255);
                }
            }

            Marshal.Copy(pixelsByte, 0, bmpData.Scan0, pixelsByte.Length);
            segment.UnlockBits(bmpData);
        }

        public static void CropGluedSegments(Bitmap bmpBin, int startX, int endX, ref List<Bitmap> container, bool twoSymbols, bool threeSymbols)
        {
            List<int> boundsList = new List<int>();

            Rectangle rect = new Rectangle(0, 0, bmpBin.Width, bmpBin.Height);
            BitmapData bmpData = bmpBin.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int width = bmpData.Stride;
            byte[] pixelsByte = new byte[width * bmpData.Height];

            Marshal.Copy(bmpData.Scan0, pixelsByte, 0, pixelsByte.Length);

            for (int i = startX * 3; i < endX * 3; i += 3)
            {
                int index = i + bmpBin.Height / 2 * width;

                if ((pixelsByte[index] == 0) && (pixelsByte[index + 2]) == 255)
                    boundsList.Add(i / 3);
            }

            Marshal.Copy(pixelsByte, 0, bmpData.Scan0, pixelsByte.Length);
            bmpBin.UnlockBits(bmpData);

            container.Add(GetSegemnt(bmpBin, startX, 0, boundsList[0] - 1, bmpBin.Height - 2));

            if (twoSymbols)
                container.Add(GetSegemnt(bmpBin, boundsList[0] + 1, 0, endX, bmpBin.Height - 2));
            else
            {
                container.Add(GetSegemnt(bmpBin, boundsList[0] + 1, 0, boundsList[1] - 1, bmpBin.Height - 2));

                if (threeSymbols)
                    container.Add(GetSegemnt(bmpBin, boundsList[1] + 1, 0, endX, bmpBin.Height - 2));
                else
                {
                    container.Add(GetSegemnt(bmpBin, boundsList[1] + 1, 0, boundsList[2] - 1, bmpBin.Height - 2));
                    container.Add(GetSegemnt(bmpBin, boundsList[2] + 1, 0, endX, bmpBin.Height - 2));
                }
            }

            for (int item = 0; item < container.Count; item++)
            {
                rect = new Rectangle(0, 0, container[item].Width, container[item].Height);
                bmpData = container[item].LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

                width = bmpData.Stride;
                pixelsByte = new byte[width * bmpData.Height];

                Marshal.Copy(bmpData.Scan0, pixelsByte, 0, pixelsByte.Length);

                bool whiteLine = false;
                int bound = 0;

                for (int j = container[item].Height / 2; j > 0; j--)
                {
                    for (int i = 0; i < container[item].Width; i++)
                    {
                        int index = i * 3 + j * width;

                        if (pixelsByte[index] == 0)
                        {
                            whiteLine = false;
                            break;
                        }
                    }

                    if (!whiteLine)
                        whiteLine = true;
                    else
                    {
                        bound = j + 1;

                        break;
                    }
                }

                Marshal.Copy(pixelsByte, 0, bmpData.Scan0, pixelsByte.Length);
                container[item].UnlockBits(bmpData);

                container[item] = GetSegemnt(container[item], 0, bound, container[item].Width - 1, container[item].Height - 1);
            }
        }

        public static Bitmap GetSegemnt(Bitmap bmpBin, int startX, int startY, int endX, int endY)
        {
            Bitmap segment = new Bitmap(endX - startX + 1, endY - startY + 1);

            Rectangle rectS = new Rectangle(0, 0, segment.Width, segment.Height);
            BitmapData segmentData = segment.LockBits(rectS, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int segmentWidth = segmentData.Stride;
            byte[] segmentPixels = new byte[segmentWidth * segmentData.Height];

            Marshal.Copy(segmentData.Scan0, segmentPixels, 0, segmentPixels.Length);

            Rectangle rect = new Rectangle(0, 0, bmpBin.Width, bmpBin.Height);
            BitmapData bmpData = bmpBin.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int width = bmpData.Stride;
            byte[] pixelsByte = new byte[width * bmpData.Height];

            Marshal.Copy(bmpData.Scan0, pixelsByte, 0, pixelsByte.Length);

            for (int i = startX * 3; i <= endX * 3; i += 3)
            {
                for (int j = startY; j <= endY; j++)
                {
                    int index = i - startX * 3 + (j - startY) * segmentWidth;
                    int startIndex = i + j * width;

                    ChangeColor(ref segmentPixels, index, pixelsByte[startIndex], pixelsByte[startIndex + 1], pixelsByte[startIndex + 2]);
                }
            }

            Marshal.Copy(pixelsByte, 0, bmpData.Scan0, pixelsByte.Length);
            bmpBin.UnlockBits(bmpData);

            Marshal.Copy(segmentPixels, 0, segmentData.Scan0, segmentPixels.Length);
            segment.UnlockBits(segmentData);

            return segment;
        }

        public static List<int> DrawBounds(int bmpWidth, int bmpHeight, int delta, int width, ref byte[] pixelsByte, ref List<int> areaWidth, ref List<int> startX, ref List<int> endX)
        {
            for (int item = 0; item < areaWidth.Count; item++)
            {
                while ((item < areaWidth.Count) && (areaWidth[item] < 20))
                {
                    for (int i = (startX[item] + 1) * 3; i < endX[item] * 3; i += 3)
                    {
                        for (int j = 0; j < bmpHeight; j++)
                        {
                            int index = i + j * width;
                            ChangeColor(ref pixelsByte, index, 255, 255, 255);
                        }
                    }

                    areaWidth.RemoveAt(item);
                    startX.RemoveAt(item);
                    endX.RemoveAt(item);
                }
            }

            for (int i = 0; i < startX[0] * 3; i += 3)
            {
                for (int j = 0; j < bmpHeight; j++)
                {
                    int index = i + j * width;
                    ChangeColor(ref pixelsByte, index, 255, 255, 255);
                }
            }

            for (int i = endX[endX.Count - 1] * 3; i < bmpWidth * 3; i += 3)
            {
                for (int j = 0; j < bmpHeight; j++)
                {
                    int index = i + j * width;
                    ChangeColor(ref pixelsByte, index, 255, 255, 255);
                }
            }

            for (int item = 0; item < areaWidth.Count - 1; item++)
            {
                for (int i = endX[item] * 3; i < startX[item + 1] * 3; i += 3)
                {
                    for (int j = 0; j < bmpHeight; j++)
                    {
                        int index = i + j * width;
                        ChangeColor(ref pixelsByte, index, 255, 255, 255);
                    }
                }
            }

            List<int> blackVertical = new List<int>();
            List<int> boundY = new List<int>();

            for (int item = 0; item < areaWidth.Count; item++)
            {
                blackVertical.Clear();

                for (int j = 0; j < bmpHeight - delta; j++)
                {
                    for (int i = (startX[item] + 1) * 3; i < endX[item] * 3; i += 3)
                    {
                        int index = i + j * width;

                        if (pixelsByte[index] == 0)
                        {
                            if (!blackVertical.Contains(j))
                                blackVertical.Add(j);
                        }
                    }
                }

                List<int> areaHeight = new List<int>();
                int length = 0;

                for (int i = 0; i < blackVertical.Count - 1; i++)
                {
                    if (blackVertical[i] == blackVertical[i + 1] - 1)
                    {
                        length++;
                    }
                    else
                    {
                        areaHeight.Add(++length);
                        length = 0;
                    }
                }

                areaHeight.Add(++length);

                int startY = blackVertical[blackVertical.Count - 1] - areaHeight[areaHeight.Count - 1];

                if (startY < 0)
                    startY = 0;

                boundY.Add(startY);
            }

            for (int itemH = 0; itemH < areaWidth.Count; itemH++)
            {
                for (int j = boundY[itemH]; j < bmpHeight; j++)
                {
                    int index = startX[itemH] * 3 + j * width;
                    ChangeColor(ref pixelsByte, index, 255, 0, 0);

                    index = endX[itemH] * 3 + j * width;
                    ChangeColor(ref pixelsByte, index, 255, 0, 0);
                }

                for (int i = startX[itemH]; i < endX[itemH]; i++)
                {
                    int index = i * 3 + boundY[itemH] * width;
                    ChangeColor(ref pixelsByte, index, 255, 0, 0);

                    index = i * 3 + (bmpHeight - 1) * width;
                    ChangeColor(ref pixelsByte, index, 255, 0, 0);
                }
            }

            return boundY;
        }

        public static void DrawAllBounds(int bmpHeight, int width, ref byte[] pixelsByte, List<int> areaWidth, List<int> startX, List<int> endX)
        {
            for (int item = 0; item < areaWidth.Count; item++)
            {
                for (int j = 0; j < bmpHeight; j++)
                {
                    int index = startX[item] * 3 + j * width;
                    ChangeColor(ref pixelsByte, index, 255, 0, 0);

                    index = endX[item] * 3 + j * width;
                    ChangeColor(ref pixelsByte, index, 255, 0, 0);
                }

                for (int i = startX[item]; i < endX[item]; i++)
                {
                    ChangeColor(ref pixelsByte, i * 3, 255, 0, 0);

                    int index = i * 3 + (bmpHeight - 1) * width;
                    ChangeColor(ref pixelsByte, index, 255, 0, 0);
                }
            }
        }

        public static int AnalyseSegment(Bitmap segment, int delta)
        {
            Rectangle rect = new Rectangle(0, 0, segment.Width, segment.Height);
            BitmapData segmentData = segment.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int width = segmentData.Stride;
            byte[] pixelsByte = new byte[width * segmentData.Height];

            Marshal.Copy(segmentData.Scan0, pixelsByte, 0, pixelsByte.Length);

            bool whiteLine = false;
            bool stop = false;
            int searchLine = segment.Height - delta;
            int bound = 0;

            while ((!stop) && (searchLine > 0))
            {
                for (int i = (segment.Width - 10) * 3; i > 10 * 3; i -= 3)
                {
                    for (int j = 0; j < searchLine; j++)
                    {
                        int index = i + j * width;

                        if (pixelsByte[index] == 0)
                        {
                            whiteLine = false;
                            break;
                        }
                    }

                    if (!whiteLine)
                        whiteLine = true;
                    else
                    {
                        for (int j = 0; j < segment.Height; j++)
                        {
                            int index = i + j * width;
                            ChangeColor(ref pixelsByte, index, 255, 0, 0);
                        }

                        bound = i / 3;

                        if (bound > 11)
                        {
                            stop = true;
                            break;
                        }
                        else
                            continue;
                    }
                }

                searchLine--;
            }

            Marshal.Copy(pixelsByte, 0, segmentData.Scan0, pixelsByte.Length);
            segment.UnlockBits(segmentData);

            return bound;
        }

        public static void DrawBoundInGlued(ref Bitmap bmpBin, int boundX, int startX, int endX, int boundY)
        {
            Rectangle rect = new Rectangle(0, 0, bmpBin.Width, bmpBin.Height);
            BitmapData bmpData = bmpBin.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int width = bmpData.Stride;
            byte[] pixelsByte = new byte[width * bmpData.Height];

            Marshal.Copy(bmpData.Scan0, pixelsByte, 0, pixelsByte.Length);

            for (int j = 0; j < bmpBin.Height; j++)
            {
                int index = boundX * 3 + j * width;
                ChangeColor(ref pixelsByte, index, 255, 0, 0);
            }

            for (int i = (startX + 1) * 3; i < (endX - 1) * 3; i += 3)
            {
                for (int j = 0; j < boundY; j++)
                {
                    int index = i + j * width;
                    ChangeColor(ref pixelsByte, index, 255, 255, 255);
                }
            }

            for (int j = bmpBin.Height / 2; j > boundY; j--)
            {
                bool whiteLine = true;

                for (int i = (startX + 1) * 3; i < (boundX - 1) * 3; i += 3)
                {
                    int index = i + j * width;

                    if (pixelsByte[index] == 0)
                    {
                        whiteLine = false;
                        break;
                    }
                }

                if (whiteLine)
                {
                    for (int i = startX * 3; i < boundX * 3; i += 3)
                    {
                        int index = i + boundY * width;
                        ChangeColor(ref pixelsByte, index, 255, 255, 255);
                    }

                    for (int k = boundY; k < j; k++)
                    {
                        int index = startX * 3 + k * width;
                        ChangeColor(ref pixelsByte, index, 255, 255, 255);
                    }

                    for (int i = startX * 3; i < boundX * 3; i += 3)
                    {
                        int index = i + j * width;
                        ChangeColor(ref pixelsByte, index, 255, 0, 0);
                    }

                    break;
                }
            }

            for (int j = bmpBin.Height / 2; j > boundY; j--)
            {
                bool whiteLine = true;

                for (int i = (boundX + 1) * 3; i < (endX - 1) * 3; i += 3)
                {
                    int index = i + j * width;

                    if (pixelsByte[index] == 0)
                    {
                        whiteLine = false;
                        break;
                    }
                }

                if (whiteLine)
                {
                    for (int i = boundX * 3; i < endX * 3; i += 3)
                    {
                        int index = i + boundY * width;
                        ChangeColor(ref pixelsByte, index, 255, 255, 255);
                    }

                    for (int k = boundY; k < j; k++)
                    {
                        int index = endX * 3 + k * width;
                        ChangeColor(ref pixelsByte, index, 255, 255, 255);
                    }

                    for (int i = boundX * 3; i < endX * 3; i += 3)
                    {
                        int index = i + j * width;
                        ChangeColor(ref pixelsByte, index, 255, 0, 0);
                    }

                    break;
                }
            }

            Marshal.Copy(pixelsByte, 0, bmpData.Scan0, pixelsByte.Length);
            bmpBin.UnlockBits(bmpData);
        }

        public static void DrawVerticalBound(ref Bitmap bmpBin, int x, int boundY)
        {
            Rectangle rect = new Rectangle(0, 0, bmpBin.Width, bmpBin.Height);
            BitmapData bmpData = bmpBin.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int width = bmpData.Stride;
            byte[] pixelsByte = new byte[width * bmpData.Height];

            Marshal.Copy(bmpData.Scan0, pixelsByte, 0, pixelsByte.Length);

            for (int j = boundY; j < bmpBin.Height; j++)
            {
                int index = x * 3 + j * width;
                ChangeColor(ref pixelsByte, index, 255, 0, 0);
            }

            Marshal.Copy(pixelsByte, 0, bmpData.Scan0, pixelsByte.Length);
            bmpBin.UnlockBits(bmpData);
        }

        public static double[] GetNumericView(Bitmap bmp, int n)
        {
            double[] numericView = new double[n];
            int index = 0;

            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int width = bmpData.Stride;
            byte[] pixelsByte = new byte[width * bmpData.Height];

            Marshal.Copy(bmpData.Scan0, pixelsByte, 0, pixelsByte.Length);

            for (int i = 0; i < pixelsByte.Length; i += 3)
            {
                if (pixelsByte[i] == 0)
                    numericView[index++] = 1;
                else
                    numericView[index++] = 0;
            }

            Marshal.Copy(pixelsByte, 0, bmpData.Scan0, pixelsByte.Length);
            bmp.UnlockBits(bmpData);

            return numericView;
        }

        public static void GetNumericView(Bitmap bmp, ref double[,] numericView, int m)
        {
            int index = 0;

            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int width = bmpData.Stride;
            byte[] pixelsByte = new byte[width * bmpData.Height];

            Marshal.Copy(bmpData.Scan0, pixelsByte, 0, pixelsByte.Length);

            for (int i = 0; i < pixelsByte.Length; i += 3)
            {
                if (pixelsByte[i] == 0)
                    numericView[m, index++] = 1;
                else
                    numericView[m, index++] = 0;
            }

            Marshal.Copy(pixelsByte, 0, bmpData.Scan0, pixelsByte.Length);
            bmp.UnlockBits(bmpData);
        }

        public static Bitmap WhiteRectangle(Bitmap bmp)
        {
            Graphics g = Graphics.FromImage(bmp);
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            g.DrawRectangle(new Pen(Color.White), rect);

            return bmp;
        }
    }
}
