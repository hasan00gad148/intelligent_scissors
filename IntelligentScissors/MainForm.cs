using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.IO;
using static IntelligentScissors.scissors;
using System.Timers;

namespace IntelligentScissors
{
    public partial class MainForm : Form
    {
        //list anchors
        //bool is mouse down
        List<Point> anchorPoints;
        List<Point[]> drawPaths;
        int numOfAnchorPoints;
        //
        Point mousePosition;
        Point[] currentPath;
        //
        bool isMouseDown;
        int width;
        int height;
        //testing
        string testingImagePath;
        bool isDone;
        scissors ImageScissors;
        public MainForm()
        {
            InitializeComponent();
            anchorPoints = new List<Point>();
            drawPaths = new List<Point[]>();
            isMouseDown = false;
            isDone = false;
        }

        RGBPixel[,] ImageMatrix;

        private void btnOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Open the browsed image and display it
                string OpenedFilePath = openFileDialog1.FileName;
                ImageMatrix = ImageOperations.OpenImage(OpenedFilePath);
                ImageOperations.DisplayImage(ImageMatrix, pictureBox1);
                //testing
                testingImagePath = Path.GetDirectoryName(OpenedFilePath);
            }
            txtWidth.Text = ImageOperations.GetWidth(ImageMatrix).ToString();
            txtHeight.Text = ImageOperations.GetHeight(ImageMatrix).ToString();

            //do stuff here
            width = ImageOperations.GetWidth(ImageMatrix);
            height = ImageOperations.GetHeight(ImageMatrix);

            ImageScissors = new scissors(width * height);
            Stopwatch sw = Stopwatch.StartNew();
            ImageScissors.constructGraph(ImageMatrix);
            sw.Stop();

            ImageScissors.writeTime(testingImagePath, "time2", sw.ElapsedMilliseconds.ToString(), (sw.ElapsedMilliseconds / 1000f).ToString());

        }


        private void btnGaussSmooth_Click(object sender, EventArgs e)
        {
            double sigma = double.Parse(txtGaussSigma.Text);
            int maskSize = (int)nudMaskSize.Value;
            ImageMatrix = ImageOperations.GaussianFilter1D(ImageMatrix, maskSize, sigma);
            ImageOperations.DisplayImage(ImageMatrix, pictureBox2);
        }


        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            label7.Text = "X: " + e.X + " Y: " + e.Y;
            mousePosition = e.Location;


            pictureBox1.Invalidate();

        }

 

        private void drawPath(MouseEventArgs e)
        {
            if (ImageMatrix != null)
            {
                if (e.Button.Equals(MouseButtons.Left))
                {
                    Point mouseLocation = e.Location;

                    if (anchorPoints.Count == 0)
                    {
                        anchorPoints.Add(mouseLocation);

                        numOfAnchorPoints++;
                        isMouseDown = true;
                    }
                    else
                    {
                        int lastIndex = anchorPoints.Count;
                        ImageMatrix = ImageScissors.multiAnchor(ImageMatrix, getNodeIndex(anchorPoints[lastIndex - 1].X, anchorPoints[lastIndex - 1].Y, width),
                            getNodeIndex(mouseLocation.X, mouseLocation.Y, width));


                        anchorPoints.Add(mouseLocation);
                        numOfAnchorPoints++;
                        ImageOperations.DisplayImage(ImageMatrix, pictureBox1);
                    }

                }
                else if (e.Button.Equals(MouseButtons.Right) && (anchorPoints.Count>1))
                {

                    int lastIndex = anchorPoints.Count;
                    ImageMatrix = ImageScissors.multiAnchor(ImageMatrix, getNodeIndex(anchorPoints[0].X, anchorPoints[0].Y, width),
                            getNodeIndex(anchorPoints[lastIndex - 1].X, anchorPoints[lastIndex - 1].Y, width));
                    anchorPoints.Clear();
                    numOfAnchorPoints = 0;
                    ImageOperations.DisplayImage(ImageMatrix, pictureBox1);
                  
                }

            }
        }
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            //check null
            if (chckBxTestin.Checked)
            {
                drawPath(e);
            }
            else
            {
                if (ImageMatrix != null)
                {
                    if (e.Button.Equals(MouseButtons.Left))
                    {
                        //check size of anchor
                        //empty => just add new anchor
                        Point mouseLocation = e.Location;

                        if (anchorPoints.Count == 0)
                        {
                            anchorPoints.Add(mouseLocation);
                            //currentAnchor = getNodeIndex((int)mouseLocation.X, (int)mouseLocation.Y, height);

                            //drawPaths.Add(new drawPath(mouseLocation, new Vector2D(), null));
                            numOfAnchorPoints++;
                            isMouseDown = true;
                            using (var g = Graphics.FromImage(pictureBox1.Image))
                            {
                                g.DrawRectangle(Pens.Blue, (int)mouseLocation.X, (int)mouseLocation.Y, 2, 2);
                                pictureBox1.Refresh();
                            }
                        }
                        else
                        {
                            //not empty => save current path into pathlist
                            //add new anchor
                            anchorPoints.Add(mouseLocation);
                            numOfAnchorPoints++;
                            drawPaths.Add(currentPath);
                            using (var g = Graphics.FromImage(pictureBox1.Image))
                            {
                                g.DrawRectangle(Pens.Blue, (int)mouseLocation.X, (int)mouseLocation.Y, 2, 2);
                                pictureBox1.Refresh();
                            }
                        }

                    }
                    else if (e.Button.Equals(MouseButtons.Right))
                    {
                        isDone = true;
                    }
                }
            }
        }
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (chckBxTestin.Checked)
            {
                return;
            }
            if (isDone)
            {
                //get free point
                Point freePoint = anchorPoints[0];
                //get anchor point last one in the list
                Point anchorPoint = anchorPoints[numOfAnchorPoints - 1];
                //get the path between them
                List<Point> pathPoints = ImageScissors.ShortestPath(
                    getNodeIndex(anchorPoint.X, anchorPoint.Y, width),
                    getNodeIndex(freePoint.X, freePoint.Y, width)
                    , width);
                //convert list to points array
                Point[] pointPath = pathPoints.ToArray();
                //update current path  and add it
                currentPath = pointPath;
                drawPaths.Add(pointPath);
                //draw list
                GraphicsPath graphicsPath = new GraphicsPath();
                graphicsPath.AddLines(pointPath);
                using (var g = pictureBox1.CreateGraphics())
                {
                    foreach (var item in drawPaths)
                    {
                        GraphicsPath graphicsPath2 = new GraphicsPath();
                        graphicsPath2.AddLines(item);
                        g.DrawPath(Pens.Magenta, graphicsPath2);
                    }

                    pictureBox1.Refresh();
                }
                isMouseDown = false;
            }
            else if (isMouseDown)
            {
                //get free point
                Point freePoint = mousePosition;
                //get anchor point last one in the list
                Point anchorPoint = anchorPoints[numOfAnchorPoints - 1];
                if (freePoint.X == anchorPoint.X && freePoint.Y == anchorPoint.Y) { return; }
                //get the path between them
                List<Point> pathInt = ImageScissors.ShortestPath(
                    getNodeIndex(anchorPoint.X, anchorPoint.Y, width),
                    getNodeIndex(freePoint.X, freePoint.Y, width)
                    , width);
                //convert list to points
                Point[] pointPath = pathInt.ToArray();
                //update current path
                currentPath = pointPath;
                //draw list
                GraphicsPath graphicsPath = new GraphicsPath();
                graphicsPath.AddLines(pointPath);
                using (var g = pictureBox1.CreateGraphics())
                {
                    foreach (var item in drawPaths)
                    {
                        GraphicsPath graphicsPath2 = new GraphicsPath();
                        graphicsPath2.AddLines(item);
                        g.DrawPath(Pens.Magenta, graphicsPath2);
                    }
                    g.DrawPath(Pens.Red, graphicsPath);
                    pictureBox1.Refresh();
                }
            }
        }
        public int getNodeIndex(int x, int y, int width)
        {
            return y * width + x;
        }

        public Point getNodeXY(int index, int width)
        {
            Point p = new Point();
            p.Y = index / width;
            p.X = index % width;
            return p;
        }

        private void btnTesting_Click(object sender, EventArgs e)
        {
            if (ImageMatrix != null)
            {
                bool isComplete = chckBxTestin.Checked;

                ImageScissors = new scissors(width * height);
                Stopwatch sw = Stopwatch.StartNew();
                ImageScissors.constructGraph(ImageMatrix);
                sw.Stop();
                float time = sw.ElapsedMilliseconds;
                if (!isComplete)
                {
                    ImageScissors.saveToFile(testingImagePath, "outPutAlgo", time);

                }
                else
                {
                    ImageScissors.saveToFileComplete(testingImagePath, "outPutAlgo", time);
                }
            }
        }

        private void btnPrintPath_Click(object sender, EventArgs e)
        {
            if (ImageMatrix == null) { return; }
            Point fromPoint = new Point();
            Point toPoint = new Point();
            scissors testing = ImageScissors;
            List<Point> pathList = new List<Point>();
            bool isComplete = cbPrintPathRadio.Checked;
            try
            {
                fromPoint.X = int.Parse(printFromX.Text);
                fromPoint.Y = int.Parse(printFromY.Text);
                toPoint.X = int.Parse(printToX.Text);
                toPoint.Y = int.Parse(printToY.Text);
                Stopwatch sw = Stopwatch.StartNew();
                pathList = testing.ShortestPath(
                     getNodeIndex(fromPoint.X, fromPoint.Y, width),
                     getNodeIndex(toPoint.X, toPoint.Y, width),
                     width);
                sw.Stop();
                float time = sw.ElapsedMilliseconds;
                if (!isComplete)
                {
                    testing.savePathtoFile(testingImagePath, "outPutPath", time, pathList, width, fromPoint, toPoint);
                }
                else
                {
                    testing.savePathtoFileComplete(testingImagePath, "outPutPath", time, pathList, width, fromPoint, toPoint);
                }
            }
            catch (Exception)
            {

                return;
            }
        }
    }
}