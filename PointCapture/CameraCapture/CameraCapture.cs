using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using System.IO;

namespace CameraCapture
{
/*
 *  The main class that creates Form window
 *  there are two ways of red point detetion
 *  by circles method and smooth method
 */
    public partial class CameraCapture : Form
    {

        // object that get frames from webcam
        private Capture capture;

        // flag that disables button when programm gets web cam frames
        private bool captureInProgress;

        // the main image object that is used to process
        static Image<Bgr, Byte> image;

        // coordinates of detected reed points
        List<RectangleData> listOfDetectedCoor = null;

        private static int scrollVall = 200;


        public CameraCapture()
        {
            InitializeComponent();
        }

        // get each frame from web camera and then process it,
        // i.e. run the algorithm that detects objects
        private void ProcessFrame(object sender, EventArgs arg)
        {
            image = capture.QueryFrame();


            if (image != null)
            {
                Image<Gray, Byte> grayImage = image.Convert<Gray, Byte>();
                
                //detect red object by detecting circles method
                //detectByCircles(image);

                //detect red object by smooth Method
                //detectBySmooth(image);
                //detectBlue(image);
                searchCvLaserDot(image);

                //searchByHSV(image);

            }

        }

        void searchCvLaserDot(Image<Bgr, Byte> origImage)
        {
            Point maxDot = new Point();
            Point minDot = new Point();
            double minVal = 0.0, maxVal = 0.0;
            CvInvoke.cvSetImageCOI(origImage, 1);
            
            IntPtr mask = new IntPtr();

            CvInvoke.cvMinMaxLoc(origImage, ref minVal, ref maxVal, ref minDot, ref maxDot, mask);

            CvInvoke.cvSetImageCOI(image, 0);

            if (maxVal >= scrollVall)
            {
                image.Draw(new Rectangle(maxDot.X, maxDot.Y, 5, 5), new Bgr(Color.Black), 3);
            }
            textBox1.AppendText(maxVal.ToString() + "\n");
            CamImageBox.Image = image;
        }

        void searchByHSV(Image<Bgr, Byte> input)
        {
            using (Image<Hsv, Byte> imgHSV = input.Convert<Hsv, Byte>())
            {
                Image<Gray, Byte>[] channels = imgHSV.Split();

                try
                {
                    //channels[0] is the mask for hue less than 20 or larger than 160
                    CvInvoke.cvInRangeS(channels[0], new MCvScalar(20), new MCvScalar(160), channels[0]);
                    channels[0]._Not();

                    //channels[1] is the mask for satuation of at least 10, this is mainly used to filter out white pixels
                    channels[1]._ThresholdBinary(new Gray(10), new Gray(255.0));

                    CvInvoke.cvAnd(channels[0], channels[1], channels[0], IntPtr.Zero);
                }
                finally
                {
                    channels[1].Dispose();
                    channels[2].Dispose();
                }

                Image<Gray, Byte> smoothedRedMask = channels[0];

                //Use Dilate followed by Erode to eliminate small gaps in some countour.
                smoothedRedMask._Dilate(1);
                smoothedRedMask._Erode(1);

                using (Image<Gray, Byte> canny = smoothedRedMask.Canny(new Gray(100), new Gray(50)))
                using (MemStorage stor = new MemStorage())
                {
                    for (Contour<Point> contours = canny.FindContours(
                       Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE,
                       Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_TREE,stor); 
                       contours != null; 
                       contours = contours.HNext)
                    {
                        Contour<Point> currentContour = contours.ApproxPoly(contours.Perimeter * 0.05, stor);
                        int x = currentContour.BoundingRectangle.X;
                        int y = currentContour.BoundingRectangle.Y;

                        int width = currentContour.BoundingRectangle.Width;
                        int height = currentContour.BoundingRectangle.Height;

                        RectangleData recData = new RectangleData(x, y, width, height);

                        int x_center = recData.getXCenter();
                        int y_center = recData.getYCenter();

                        try
                        {
                            listOfDetectedCoor.Add(recData);
                        }
                        catch (Exception e)
                        {
                            Console.Write(e.StackTrace);
                        }
                    }
                    CamImageBox.Image = canny;
                }
            }

        }

        // EmguCV build-in method that detects circles,
        // i.e. it uses Hough classifier
        private void detectByCircles(Image<Bgr, Byte> imageOriginal)
        {
            Image<Gray, Byte> imProcessed = null;
            imProcessed = imageOriginal.InRange(new Bgr(0, 0, 175),    //0,0,175 - 100,100,256
                                            new Bgr(100, 100, 255)); // somehow range for orange related colors
            imProcessed = imProcessed.SmoothGaussian(9);

            CircleF[] circles = imProcessed.HoughCircles(new Gray(100),
                                                        new Gray(50),
                                                        2,
                                                        imProcessed.Height / 4,
                                                        1, 400)[0];
            foreach (var circle in circles)
            {
                image.Draw(circle, new Bgr(Color.Red), 3); //draw objects, color, thikness
            }

            CamImageBox.Image = image;
        }

        // each frame go through several steps, then
        // output binary image
        private void detectBySmooth(Image<Bgr, Byte> imageOriginal)
        {
            Image<Gray, Byte> diff_im = imageOriginal.InRange(new Bgr(0, 0, 175), new Bgr(100, 100, 255));
            diff_im = bwareaopen(diff_im, 300);

            foreach (RectangleData recData in listOfDetectedCoor)
            {
                image.Draw(new Rectangle(recData.getX(), recData.getY(),
                            recData.getWidth(), recData.getHeight()),
                            new Bgr(Color.Black), 3);
                //Create the font
                MCvFont f = new MCvFont(Emgu.CV.CvEnum.FONT.CV_FONT_HERSHEY_COMPLEX, 1.0, 1.0);
                //Draw text on the image using the specific font
                image.Draw("X: " + recData.getXCenter() + " Y: " + recData.getYCenter(),
                    ref f, new Point(recData.getX(), recData.getY()), new Bgr(0, 255, 0));
            }

            CamImageBox.Image = image;
        }

        private void detectBlue(Image<Bgr, Byte> imageOriginal)
        {
            Image<Gray, Byte> diff_im = imageOriginal.InRange(new Bgr(151, 0, 70),    //0,0,175 - 100,100,256
                                            new Bgr(136, 150, 0)); // somehow range for blue related colors
            diff_im = bwareaopen(diff_im, 500);

            foreach (RectangleData recData in listOfDetectedCoor)
            {
                image.Draw(new Rectangle(recData.getX(), recData.getY(),
                            recData.getWidth(), recData.getHeight()),
                            new Bgr(Color.Blue), 3);
                //Create the font
                MCvFont f = new MCvFont(Emgu.CV.CvEnum.FONT.CV_FONT_HERSHEY_PLAIN, 2.0, 2.0);
                //Draw on the image using the specific font
                image.Draw("X: " + recData.getXCenter() + " Y: " + recData.getYCenter(),
                    ref f, new Point(recData.getX(), recData.getY()), new Bgr(0, 255, 0));

                //controlBot();
            }

            CamImageBox.Image = image;
        }

        // activates web cam
        private void btnStart_Click(object sender, EventArgs e)
        {
            #region if capture is not created, create it now
            if (capture == null)
            {
                try
                {
                    capture = new Capture(1);
                }
                catch (NullReferenceException excpt)
                {
                    MessageBox.Show(excpt.Message);
                }
            }
            #endregion

            if (capture != null)
            {
                if (captureInProgress)
                {  
                    btnStart.Text = "Start!"; //
                    Application.Idle -= ProcessFrame;
                }
                else
                {
                    btnStart.Text = "Stop";
                    Application.Idle += ProcessFrame;
                }

                captureInProgress = !captureInProgress;
            }
        }

        // release web cam
        private void ReleaseData()
        {
            if (capture != null)
                capture.Dispose();
        }
        
        // first action when window is opening
        private void CameraCapture_Load(object sender, EventArgs e)
        {
            listOfDetectedCoor = new List<RectangleData>();
        }


        // detect object from single picture
        private void browsButton_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Image inputImage = Image.FromFile(openFileDialog1.FileName);
                image = new Image<Bgr, byte>(new Bitmap(inputImage));
                searchCvLaserDot(image);
                //detectBySmooth(image);
            }
        }

        // transform image to binary image
        // and then iterate through area to get the coordinates
        // of detected object
        private Image<Gray, Byte> bwareaopen(Image<Gray, Byte> Input_Image, int threshold)
        {
            listOfDetectedCoor.Clear();
            Image<Gray, Byte> bwresults = Input_Image.Copy();

            using (MemStorage storage = new MemStorage())
            {
                for (Contour<Point> contours = Input_Image.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST, storage); contours != null; contours = contours.HNext)
                {
                    Contour<Point> currentContour = contours.ApproxPoly(contours.Perimeter * 0.05, storage);
                    if (currentContour.Area < threshold)
                    {
                        for (int i = currentContour.BoundingRectangle.X; i < currentContour.BoundingRectangle.X + currentContour.BoundingRectangle.Width; i++)
                        {
                            for (int j = currentContour.BoundingRectangle.Y; j < currentContour.BoundingRectangle.Y + currentContour.BoundingRectangle.Height; j++)
                            {
                                bwresults.Data[j, i, 0] = 0;
                            }
                        }
                    }
                    else if (currentContour.Area > threshold)
                    {
                        int x = currentContour.BoundingRectangle.X;
                        int y = currentContour.BoundingRectangle.Y;

                        int width = currentContour.BoundingRectangle.Width;
                        int height = currentContour.BoundingRectangle.Height;

                        RectangleData recData = new RectangleData(x, y, width, height);

                        int x_center = recData.getXCenter();
                        int y_center = recData.getYCenter();

                        try
                        {
                            listOfDetectedCoor.Add(recData);
                        }
                        catch (Exception e)
                        {
                            Console.Write(e.StackTrace);
                        }
                    }
                }
            }
            return bwresults;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            scrollVall = trackBar1.Value;
            textBox1.AppendText(scrollVall.ToString() + "\n");
        }

    }
}
