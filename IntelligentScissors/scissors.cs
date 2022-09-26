using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IntelligentScissors
{
    public partial class scissors
    {
        //data
        Dictionary<int, List<edge>> imageGraph;
        public struct edge
        {
            public int neighbuorPixel;
            public double weight;
            public edge(int neighbuorPixel, double weight)
            {
                this.neighbuorPixel = neighbuorPixel;
                this.weight = weight;
            }
        }
        public struct DijkstraReturn
        {
            public int[] parents;
            public double[] weights;
        }
        public scissors(int size)
        {
            imageGraph = new Dictionary<int, List<edge>>(size);
            for (int i = 0; i < size; i++)
            {
                imageGraph.Add(i, new List<edge>());
            }
        }

        public void constructGraph(RGBPixel[,] imageMatrix)
        {
            //Get size details
            int width = imageMatrix.GetLength(1);
            int height = imageMatrix.GetLength(0);
            //edges left top right bottom
            //Start mapping the boundary pixels (row, col equals 0 or Max-1)
            int maxWidthIndex = width - 1;
            int maxHeightIndex = height - 1;
            //Start mapping inside pixels
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    //col=>x row=>y
                    //row col
                    //Calculate Weight(p1,p2) = 1/G
                    Vector2D pixelEnergy = ImageOperations.CalculatePixelEnergies(col, row, imageMatrix);
                    pixelEnergy.X = 1 / pixelEnergy.X;
                    pixelEnergy.Y = 1 / pixelEnergy.Y;
                    //check for infinite numbers
                    if (Double.IsInfinity(pixelEnergy.X)) { pixelEnergy.X = 1e+16; }
                    if (Double.IsInfinity(pixelEnergy.Y)) { pixelEnergy.Y = 1e+16; }
                    //start adding
                    int pixelPosition = (row * width) + col;
                    //add x weight in current node and nodes right to it
                    if (col < maxWidthIndex)
                    {
                        imageGraph[pixelPosition].Add(new edge(pixelPosition + 1, pixelEnergy.X));
                        imageGraph[pixelPosition + 1].Add(new edge(pixelPosition, pixelEnergy.X));
                    }
                    //add y weight in current node and nodes below  it
                    if (row < maxHeightIndex)
                    {
                        imageGraph[pixelPosition].Add(new edge(pixelPosition + width, pixelEnergy.Y));
                        imageGraph[pixelPosition + width].Add(new edge(pixelPosition, pixelEnergy.Y));
                    }
                }
            }
        }
        private DijkstraReturn Dijkstra(int s, int e)
        {
            Dictionary<int, List<edge>> Graph = imageGraph;
            int nodeSize = Graph.Keys.Count;
            int[] parents = new int[nodeSize];
            double[] weights = new double[nodeSize];
            PriorityQueue nodeQ = new PriorityQueue();

            edge start = new edge();
            start.neighbuorPixel = s;
            start.weight = 0;
            nodeQ.enQ(start);

            parents[s] = s;
            weights[s] = 0;

            foreach (int n in Graph.Keys)
            {
                if (n == s)
                    continue;

                parents[n] = -1;
                weights[n] = Double.PositiveInfinity;
            }

            while (nodeQ.getcount() != 0)
            {
                edge v = nodeQ.deQ();
                double weightFromSToV = weights[v.neighbuorPixel];

                if (v.neighbuorPixel == e)
                    break;

                foreach (edge nod in Graph[v.neighbuorPixel])
                {
                    double weightFromSToNod_v = weightFromSToV + nod.weight;
                    if (weightFromSToNod_v < weights[nod.neighbuorPixel])
                    {
                        edge node2 = new edge();
                        node2.neighbuorPixel = nod.neighbuorPixel;
                        node2.weight = weightFromSToNod_v;
                        nodeQ.enQ(node2);
                        weights[nod.neighbuorPixel] = weightFromSToNod_v;
                        parents[nod.neighbuorPixel] = v.neighbuorPixel;
                    }
                }

            }
            DijkstraReturn dr = new DijkstraReturn();
            dr.parents = parents;
            dr.weights = weights;
            return dr;
        }

        public List<Point> BackTrack(int s, int e, int width, DijkstraReturn dr)
        {
            List<Point> path = new List<Point>();

            int nod = e;

            while (nod != s)
            {
                Point p1 = getNodeXY(nod, width);
                path.Add(p1);
                nod = dr.parents[nod];
            }
            Point p2 = getNodeXY(s, width);
            path.Add(p2);
            return path;
        }

        public List<Point> ShortestPath(int s, int e, int width)
        {
            DijkstraReturn dr = Dijkstra(s, e);
            Dictionary<int, List<edge>> Graph = imageGraph;

            return BackTrack(s, e, width, dr);
        }

        public RGBPixel[,] multiAnchor(RGBPixel[,] imageMatrix, int s, int e)
        {
            int w = ImageOperations.GetWidth(imageMatrix);
            List<Point> points = ShortestPath(s, e, w);



            return draw(imageMatrix, points);
        }
        public RGBPixel[,] draw(RGBPixel[,] imageMatrix, List<Point> points)
        {
            RGBPixel[,] imageMatrixCopy = imageMatrix;
            int size = points.Count;
            for (int i = 1; i < size - 1; ++i)
            {
                RGBPixel anchorREC = new RGBPixel();
                anchorREC.red = 255;
                anchorREC.green = 0;
                anchorREC.blue = 0;

                imageMatrix[points[i].Y, points[i].X] = anchorREC;
            }
            RGBPixel anchorREC2 = new RGBPixel();
            anchorREC2.red = 0;
            anchorREC2.green = 0;
            anchorREC2.blue = 255;

            imageMatrix[points[0].Y, points[0].X] = anchorREC2;

            anchorREC2 = new RGBPixel();
            anchorREC2.red = 0;
            anchorREC2.green = 0;
            anchorREC2.blue = 255;

            imageMatrix[points[size - 1].Y, points[size - 1].X] = anchorREC2;

            return imageMatrix;
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
        //save
        public void saveToFile(string filePath, string fileName, float time)
        {
            string outputPath = filePath + "\\" + fileName + ".txt";
            string text = "The constructed graph\r\n\r\n";
            using (var outputFileStream = File.CreateText(outputPath))
            {
                // add the current line
                outputFileStream.Write(text);
                foreach (var vertex in imageGraph.Keys)
                {
                    outputFileStream.Write(" The  index node");
                    outputFileStream.Write(vertex.ToString());
                    outputFileStream.Write("\r\nEdges\r\n");
                    foreach (var edge in imageGraph[vertex])
                    {
                        text = "edge from   " + vertex.ToString() + "  To  " + edge.neighbuorPixel.ToString() + "  With Weights  " + edge.weight.ToString() + "\r\n";
                        outputFileStream.Write(text);
                    }
                    text = "\r\n\r\n\r\n";
                    outputFileStream.Write(text);
                }
                outputFileStream.WriteLine("\r\n" + "Time in milliseconds: " + time
                    + "\r\n" + "Time in Seconds " + time / 1000f);
            }
        }
        public void saveToFileComplete(string filePath, string fileName, float time)
        {
            string outputPath = filePath + "\\" + fileName + ".txt";
            string text = "Constructed Graph: (Format: node_index|edges:(from, to, weight)... )\r\n";
            using (var outputFileStream = File.CreateText(outputPath))
            {
                // add the current line
                outputFileStream.Write(text);
                foreach (var vertex in imageGraph.Keys)
                {
                    outputFileStream.Write(vertex.ToString());
                    outputFileStream.Write("|edges:");
                    foreach (var edge in imageGraph[vertex])
                    {
                        text = "(" + vertex.ToString() + "," + edge.neighbuorPixel.ToString() + "," + edge.weight + ")";
                        outputFileStream.Write(text);
                    }
                    text = "\r\n";
                    outputFileStream.Write(text);
                }
                outputFileStream.WriteLine("\r\n" + "Time in milliseconds: " + time + "\r\n" + "Time in Seconds " + time / 1000f);
            }
        }
        public void savePathtoFile(string filePath, string fileName, float time, List<Point> pathArray, int width, Point start, Point end)
        {
            string outputPath = filePath + "\\" + fileName + ".txt";
            string firstLine = " The Shortest path from Node " + getNodeIndex(start.X, start.Y, width).ToString()
                + " position   " + start.X.ToString() + "  " + start.Y.ToString() + "\r\n";
            string secondLine = " The Shortest path to Node " + getNodeIndex(end.X, end.Y, width).ToString()
                + " position   " + end.X.ToString() + "  " + end.Y.ToString() + "\r\n";
            using (var outputFileStream = File.CreateText(outputPath))
            {
                outputFileStream.Write(firstLine);
                outputFileStream.Write(secondLine);
                // add the current line
                foreach (var pathLine in pathArray)
                {
                    int index = getNodeIndex(pathLine.X, pathLine.Y, width);
                    outputFileStream.Write("Node ");
                    outputFileStream.Write(index.ToString());
                    outputFileStream.Write(" at position x ");
                    outputFileStream.Write(pathLine.X);
                    outputFileStream.Write(" at position y ");
                    outputFileStream.Write(pathLine.Y);
                    outputFileStream.Write("\r\n");
                }
                outputFileStream.WriteLine("\r\n" + "Time in milliseconds: " + time
                    + "\r\n" + "Time in Seconds " + time / 1000f);
            }
        }
        public void savePathtoFileComplete(string filePath, string fileName, float time, List<Point> pathArray, int width, Point start, Point end)
        {
            string outputPath = filePath + "\\" + fileName + ".txt";
            string firstLine = "The Shortest path from Node "
                + getNodeIndex(start.X, start.Y, width).ToString()
                + " at (" + start.X + ", " + start.Y + ") to Node "
                + getNodeIndex(end.X, end.Y, width).ToString()
                + " at (" + end.X + ", " + end.Y + ")" + "\r\nFormat: (node_index, x, y)\r\n";

            using (var outputFileStream = File.CreateText(outputPath))
            {
                outputFileStream.Write(firstLine);
                // add the current line
                foreach (var pathLine in pathArray)
                {
                    int index = getNodeIndex(pathLine.X, pathLine.Y, width);
                    outputFileStream.Write("{" + index + "}," + pathLine.X + "," + pathLine.Y + ")\r\n");
                }
                outputFileStream.WriteLine("\r\n" + "Time in milliseconds: " + time
                    + "\r\n" + "Time in Seconds " + time / 1000f);
            }
        }
        //test function
        public void readFile(string myFile, string caseNum, string testFile, string resultName, string time, string sample)
        {
            //read 2 files
            string[] myText = File.ReadAllLines(@"D:\Study\Analysis & Design of Algorithms\Project\[1] Intelligent Scissors\Testcases\" + sample + "\\" + caseNum + "\\" + myFile + ".txt");
            string[] testText = File.ReadAllLines(@"D:\Study\Analysis & Design of Algorithms\Project\[1] Intelligent Scissors\Testcases\" + sample + "\\" + caseNum + "\\" + testFile + ".txt");

            int length = testText.Length;
            int i = 0;
            bool result = true;

            string resultStr = "";
            for (int line = 1; line < length; line++)
            {
                int nodeNumber;
                if (myText[line].StartsWith(" The"))
                {
                    nodeNumber = int.Parse(myText[line].Substring(" The  index node".Length));
                    //get to edges line
                    line += 2;
                    //get to line
                    //edge from   0  To  1  With Weights  1E+16
                    int currentLine = line;
                    Dictionary<int, double> keyValuePairs = new Dictionary<int, double>();

                    while (myText[currentLine].StartsWith("edge from "))
                    {
                        string[] data = myText[currentLine].Split();
                        keyValuePairs.Add(int.Parse(data[8]), double.Parse(data[13]));
                        currentLine++;
                    }
                    int T = currentLine;
                    currentLine = line;
                    while (testText[currentLine].StartsWith("edge from "))
                    {
                        string[] data = testText[currentLine].Split();
                        if (keyValuePairs.ContainsKey(int.Parse(data[8])))
                        {
                            if (keyValuePairs[int.Parse(data[8])] != double.Parse(data[13]))
                            {
                                //weight wrong
                                resultStr += "\ntest \n";
                                resultStr += "node " + data[8] + " weight" + data[13] + "\n";
                                resultStr += "\nmine\n";
                                resultStr += "node " + data[8] + " actual weight " + keyValuePairs[int.Parse(data[8])] + "\n";
                                result = false;
                            }
                        }
                        else
                        {
                            //name wrong
                            resultStr += "node " + nodeNumber + " doesnt contain " + data[8] + " \n";
                            result = false;
                        }
                        currentLine++;
                    }
                    line = currentLine;
                }
                if (!result) { break; }
            }

            if (result)
            {
                resultStr += "True \n" + time + "\n";
            }
            else
            {
                resultStr += "False \n" + time + "\n";
            }
            File.WriteAllText(@"D:\Study\Analysis & Design of Algorithms\Project\[1] Intelligent Scissors\Testcases\" + sample + "\\" + caseNum + "\\" + resultName + ".txt", resultStr);
        }
        public void readFileComplete(string myFile, string caseNum, string testFile, string resultName, string time, string sample)
        {
            //read 2 files
            //string[] myText = File.ReadAllLines("C:\\Users\\Administrator\\Desktop\\[1] Intelligent Scissors\\Testcases\\" + sample + "\\" + caseNum + "\\" + myFile + ".txt");
            //string[] testText = File.ReadAllLines("C:\\Users\\Administrator\\Desktop\\[1] Intelligent Scissors\\Testcases\\" + sample + "\\" + caseNum + "\\" + testFile + ".txt");


            int i = 0;
            bool result = true;
            string resultStr = "";
            using (StreamReader testStream = File.OpenText(@"D:\Study\Analysis & Design of Algorithms\Project\[1] Intelligent Scissors\Testcases\" + sample + "\\" + caseNum + "\\" + testFile + ".txt"))
            {
                using (StreamReader myStream = File.OpenText(@"D:\Study\Analysis & Design of Algorithms\Project\[1] Intelligent Scissors\Testcases\" + sample + "\\" + caseNum + "\\" + myFile + ".txt"))
                {
                    //1|edges:(1,0,1)(1,2,1)(1,4273,8.16588936419192E+15)
                    string myLine = string.Empty;
                    string testLine = string.Empty;
                    testLine = testStream.ReadLine();
                    myLine = myStream.ReadLine();
                    while ((myLine = myStream.ReadLine()) != null)
                    {
                        Dictionary<int, double> keyValuePairs = new Dictionary<int, double>();
                        testLine = testStream.ReadLine();
                        int nodeNumber = int.Parse(myLine.Substring(0, 1));
                        if (nodeNumber == 100) { break; }
                        //parse line from test file
                        string[] testArr = testLine.Split('|');
                        //[0] 1|edges
                        //[1] (1,0,1)(1,2,1)(1,4273,8.16588936419192E+15)
                        string[] testData = testArr[1].Split('(');
                        for (int x = 1; x < testData.Length; x++)
                        {
                            string[] nodeInfo = testData[x].Split(',');
                            keyValuePairs.Add(int.Parse(nodeInfo[1]), double.Parse(nodeInfo[2].Substring(0, nodeInfo[2].Length - 1)));
                        }
                        //parse line form my file
                        string[] myArr = myLine.Split('|');
                        string[] myData = myArr[1].Split('(');
                        for (int x = 1; x < myData.Length; x++)
                        {
                            string[] nodeInfo = myData[x].Split(',');
                            int nodeNum = int.Parse(nodeInfo[1]);
                            //check if node exists
                            if (!keyValuePairs.ContainsKey(nodeNum))
                            {
                                //node doesnt exist
                                resultStr += "node " + nodeNumber + " doesnt contain " + nodeNum + " \n";
                                result = false;
                            }
                            else
                            {
                                if (keyValuePairs[nodeNum] != double.Parse(nodeInfo[2].Substring(0, nodeInfo[2].Length - 1)))
                                {
                                    //wieght wrong
                                    resultStr += "\ntest \n";
                                    resultStr += "node " + nodeNumber + " to " + nodeNum + " weight " + keyValuePairs[nodeNum];
                                    resultStr += "\nmine\n";
                                    resultStr += "node " + nodeNumber + " to " + nodeNum + " Realweight " + double.Parse(nodeInfo[2].Substring(0, nodeInfo[2].Length - 1)) + "\n";

                                    result = false;
                                }
                            }
                        }
                        //if (!result) { break; }

                    }

                }
            }


            if (result)
            {
                resultStr += "True \n" + time + "\n";
            }
            else
            {
                resultStr += "False \n" + time + "\n";
            }
            File.WriteAllText(@"D:\Study\Analysis & Design of Algorithms\Project\[1] Intelligent Scissors\Testcases\" + sample + "\\" + caseNum + "\\" + resultName + ".txt", resultStr);
        }
        public void testShortestPath()
        {
            RGBPixel[,] ImageMatrix = ImageOperations.OpenImage("F:\\aaaaaaaaaaa\\Studying\\ALGO\\[1] Intelligent Scissors\\Testcases\\Complete\\Case2\\test.png");
            Dictionary<int, List<edge>> Graph = this.imageGraph;
            int h = ImageOperations.GetHeight(ImageMatrix);
            int width = ImageOperations.GetWidth(ImageMatrix);
            List<Point> path = ShortestPath(1695577, 2055619, h);

            string createText = "";
            int i = 1;
            foreach (Point p in path)
            {
                //createText += "===========================================\n";
                createText += Convert.ToString(i) + "#Node :" + Convert.ToString(getNodeIndex(p.X, p.Y, h)) + "====> " + "(X):" + Convert.ToString(p.X) + ", (y):" + Convert.ToString(p.Y) + "===========>" + "\n";

                ++i;
            }
            File.WriteAllText("F:\\aaaaaaaaaaa\\Studying\\ALGO\\[1] Intelligent Scissors\\Testcases\\Complete\\Case2\\out.txt", createText);

        }

        public void testShortestPath2()
        {
            RGBPixel[,] ImageMatrix = ImageOperations.OpenImage("testFiles//");
            Dictionary<int, List<edge>> Graph = this.imageGraph;
            int w = ImageOperations.GetWidth(ImageMatrix);
            int h = ImageOperations.GetHeight(ImageMatrix);
            int n1 = getNodeIndex(8, 31, h);
            int n2 = getNodeIndex(13, 39, h);
            List<Point> path = ShortestPath(n1, n2, w);


            foreach (Point i in path)
            {

                RGBPixel rpg = new RGBPixel();
                rpg.blue = 0;
                rpg.green = 0;
                rpg.red = 250;
                ImageMatrix[(int)i.X, (int)i.Y] = rpg;

            }

            Bitmap bmp = new Bitmap(78, 59);

            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                {

                    Color c = Color.FromArgb(ImageMatrix[i, j].red, ImageMatrix[i, j].blue, ImageMatrix[i, j].green);
                    bmp.SetPixel(j, i, c);
                }
            }
            bmp.Save("F:\\aaaaaaaaaaa\\Studying\\ALGO\\[1] Intelligent Scissors\\Testcases\\Sample\\Case3\\m.bmp");
        }

        public void writeTime(string myFile, string resultName, string t1, string t2)
        {
            string result = "MilliSeconds: " + t1 + "\r\n " + "Seconds: " + t2;
            File.WriteAllText(myFile + "\\" + resultName + ".txt", result);
        }
    }
}
