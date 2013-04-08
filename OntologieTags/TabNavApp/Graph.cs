using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using SilverlightCompLib.Graph;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections;

namespace TabNavApp
{
    public class Graph
    {
        private Graphic graphGraphic;                                                   //the graph
        private NodeCollection<string> _nodes = new NodeCollection<string>();
        private event EventHandler<EventArgs> onLoadData;                               //loadData event

        private object readingLock = new object();
        private Dictionary<string, Vertex> grp = new Dictionary<string, Vertex>();
        private Dictionary<string, LinkedList<Vertex>> DictL = new Dictionary<string, LinkedList<Vertex>>();
        private NodeTag[] tags = new NodeTag[4];      //list of node-tags whiche are common to nodes of one direction


        /// <summary>
        /// 
        /// </summary>
        public class Vertex
        {
            public string Word;
            public List<Vertex> Neigh = new List<Vertex>();
        }

        public Graph(double height, double width)
        {
            graphGraphic = new Graphic(new Size(width, height));

            graphGraphic.NodesBindingPath = "ChildNodes";
            graphGraphic.Nodes = new ObservableCollection<Node<string>>();
            _nodes = new NodeCollection<string>();
            readingLock = new object();
            grp = new Dictionary<string, Vertex>();
            DictL = new Dictionary<string, LinkedList<Vertex>>();


            string center = "current concept\nthe surrounding concepts are \nin a direct or indirect relation" +
                "\nto the current one\n(distinguished by direction & color)";
            List<string>[] inputLists = new List<string>[4];
            inputLists[0] = new List<string>() { "broader concepts" };
            inputLists[1] = new List<string>() { "narrower concepts" };
            inputLists[2] = new List<string>() { "sibling concepts" };
            inputLists[3] = new List<string>() { "related concepts" };

            InitTags();
            InitData(inputLists, center);
            
            UpdateCenterEdges(_nodes.Items[0]);
        }

        public Graph(double height, double width, List<string>[] children, string center)
        {
            graphGraphic = new Graphic(new Size(width, height));
            //graphGraphic.onMouseEvent -= new Graphic.MouseEvent(graph_onMouseEvent);
            //graphGraphic.onMovingStoped -= new EventHandler<EventArgs>(graph_onMovingStoped);
            //graphGraphic.onMouseEvent += new Graphic.MouseEvent(graph_onMouseEvent);
            //graphGraphic.onMovingStoped += new EventHandler<EventArgs>(graph_onMovingStoped);

            graphGraphic.NodesBindingPath = "ChildNodes";                       //binding-string
            graphGraphic.Nodes = new ObservableCollection<Node<string>>();      //
            _nodes = new NodeCollection<string>();
            readingLock = new object();
            grp = new Dictionary<string, Vertex>();
            DictL = new Dictionary<string, LinkedList<Vertex>>();

            InitTags();                                                         //define Tags
            InitData(children, center);                                         //refine raw-data

            UpdateCenterEdges(_nodes.Items[0]);                                 //calculate new edges
        }

        /// <summary>
        /// updates graph-graphic when size of container changed
        /// </summary>
        /// <param name="sender">container of the graph</param>
        /// <param name="e">the SizeChangedEventArgs</param>
        void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (graphGraphic == null) return;
            double h = e.NewSize.Height;
            if (h <= 40) return;
            graphGraphic.Height = e.NewSize.Height;
            graphGraphic.Width = e.NewSize.Width;
            graphGraphic.UpdateLayout();
        }

        /// define node-tags (color, initial direction of the node)
        /// </summary>
        void InitTags()
        {
            tags[0] = new NodeTag(Color.FromArgb(255, 255, 200, 200), 0, -1);
            tags[1] = new NodeTag(Color.FromArgb(255, 200, 255, 200), 0, 1);
            tags[2] = new NodeTag(Color.FromArgb(255, 55, 110, 55), -1, 0);
            tags[3] = new NodeTag(Color.FromArgb(255, 200, 200, 255), 1, 0);
        }
        /// <summary>
        /// refines the raw data of concept-lists to node-lists
        /// external code -> no in-depth description
        /// </summary>
        /// <param name="inputLists">the concept-lists destined to become node-lists</param>
        /// <param name="center">center-node-name</param>
        void InitData(List<string>[] inputLists, string center)
        {
            lock (readingLock)
            {
                int i = 0;
                _nodes.AddNode(center);
                grp.Clear();
                Vertex c = new Vertex() { Word = center };
                grp[c.Word] = c;

                foreach (List<string> list in inputLists)
                {
                    foreach (string s in list)
                    {
                        _nodes.AddNode(s, tags[i]);
                        Vertex v;

                        if (grp.ContainsKey(s))
                        {
                            v = grp[s];
                        }
                        else
                        {
                            v = new Vertex() { Word = s };
                            grp[v.Word] = v;
                        }
                        if (!c.Neigh.Contains(v)) c.Neigh.Add(v);
                        v.Neigh.Add(c);

                    }
                    i++;
                }
            }
        }
        /// <summary>
        /// event fires when movement of nodes has stopped
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void graph_onMovingStoped(object sender, EventArgs e)
        {
            //PropertyInfo highlightedItemProperty 
            //object zw = Static.GetPrivateFieldValue<object>(graphGraphic, "_nodePresenters");
            //object highlightedItemValue = highlightedItemProperty.GetValue(graphGraphic, null);
        }


        /// <summary>
        /// calculates new edges between nodes
        /// external code  -> no in-depth description
        /// </summary>
        /// <param name="center">center-node-name</param>
        private void UpdateCenterEdges(string center)
        {
            int ItemsCount = (_nodes == null) ? 0 : _nodes.Items.Count;
            if (ItemsCount == 0) return;

            //if (CheckAccess())
            if (true)
            {
                Node<string> oldnode = graphGraphic.CenterObject as Node<String>;
                List<Node<String>> OldVertices = new List<Node<string>>();
                if (oldnode != null)
                {
                    OldVertices = _nodes.GetChildren(oldnode.Item);
                }

                Node<string> node = _nodes.GetNode(center);
                graphGraphic.CenterObject = node;
                if (node != null)
                {
                    lock (readingLock)
                    {
                        List<Node<String>> Vertices = _nodes.GetChildren(node.Item);
                        Vertices.ForEach(el => _nodes.RemoveEdge(node.Item, el.Item));

                        List<Node<String>> LstNGD = new List<Node<string>>();

                        grp[node.Item].Neigh.ForEach(Vois =>
                        {
                            if (_nodes.Items.Contains(Vois.Word))
                            {
                                Node<string> newNode = _nodes.GetNode(Vois.Word);

                                if (!node.ChildNodes.Contains(newNode))
                                {
                                    LstNGD.Add(newNode);
                                }
                            }
                        });

                        var newlist = LstNGD.Where(el => true).ToList();
                        OldVertices.ForEach(el =>
                        {
                            if (!LstNGD.Remove(el))
                                graphGraphic.Nodes.Remove(el);
                        });

                        newlist.ForEach(el =>
                        {
                            if (!_nodes.ContainsEdge(node.Item, el.Item))
                            {
                                try
                                {
                                    _nodes.AddEdge(node.Item, el.Item);
                                    if (LstNGD.Contains(el) && !graphGraphic.Nodes.Contains(el))
                                    {
                                        graphGraphic.Nodes.Add(el);
                                    }//Recyclage des centres fait apparaitre 2 fois le meme noeud dans les voisins
                                }
                                catch (ArgumentException)
                                {
                                }
                            }
                        });

                    }   //Lock_reading
                }
                if (onLoadData != null) onLoadData(node.Item, null);
            }
        }
    }
}
