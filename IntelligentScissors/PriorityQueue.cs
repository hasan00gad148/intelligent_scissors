using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace IntelligentScissors
{
    public partial class scissors
    {
        public class PriorityQueue
        {
            private List<edge> que;
            private Dictionary<int, int> index;

            public PriorityQueue()
            {
                que = new List<edge>();
                index = new Dictionary<int, int>();
            }

            public int getcount()
            {
                return que.Count;
            }

            public void enQ(edge value)
            {
                if (que.Count == 0)
                {
                    que.Add(value);
                    index[value.neighbuorPixel] = 0;
                }
                else
                {
                    que.Add(value);
                    int n = que.Count - 1;
                    index[value.neighbuorPixel] = n;
                    int i = n;
                    while (i > 0)
                    {
                        i = (int)Math.Floor((double)(i - 1) / 2);
                        if (i < 0)
                            break;
                        if (que[i].weight > que[n].weight)
                        {

                            int tmp1 = index[que[i].neighbuorPixel];
                            index[que[i].neighbuorPixel] = index[que[n].neighbuorPixel];
                            index[que[n].neighbuorPixel] = tmp1;

                            edge tmp2 = que[i];
                            que[i] = que[n];
                            que[n] = tmp2;

                            n = i;
                        }

                        else break;
                    }
                }
            }

            private edge deleteNode(int nod)
            {
                edge value = que[nod];
                int n = que.Count - 1;
                int i = nod;


                int tmp3 = index[que[nod].neighbuorPixel];
                index[que[nod].neighbuorPixel] = index[que[n].neighbuorPixel];
                index[que[n].neighbuorPixel] = tmp3;

                edge tmp = que[nod];
                que[nod] = que[n];
                que[n] = tmp;

                index[que[n].neighbuorPixel] = -1;

                var sw = Stopwatch.StartNew();
                que.RemoveAt(n);
                sw.Stop();

                n -= 1;
                int j = nod;
                while (i < n / 2)
                {
                    i = (i * 2) + 1;

                    if (i < n - 1 && que[i].weight > que[i + 1].weight)
                        i += 1;

                    if (que[j].weight > que[i].weight)
                    {

                        int tmp1 = index[que[i].neighbuorPixel];
                        index[que[i].neighbuorPixel] = index[que[j].neighbuorPixel];
                        index[que[j].neighbuorPixel] = tmp1;

                        edge tmp2 = que[i];
                        que[i] = que[j];
                        que[j] = tmp2;
                        j = i;
                    }
                    else break;
                }

                return value;
            }

            public edge deQ()
            {
                edge value = deleteNode(0);

                return value;
            }

            public void update(int nod, double weight)
            {
                int ind = index[nod];
                if (ind == -1)
                {
                    //Node tmp = new Node();
                    //tmp.name = nod;
                    //tmp.weight = weight;
                    //enQ(tmp);
                    return;
                }
                else
                {
                    edge tmp = deleteNode(ind);
                    tmp.weight = weight;
                    enQ(tmp);
                }

            }
        }
    }
}
