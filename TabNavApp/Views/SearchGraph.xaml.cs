﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SilverlightCompLib.Classes;
using SilverlightCompLib.Graph;
using SilverlightCompLib.Controls;
using System.Collections.ObjectModel;
using System.Reflection;
using VirtuosoQuery;
using VirtuosoQuery.Silverlight.Skos;
using VDS.RDF;
using VDS.RDF.Query;
using System.Threading;
using System.Collections;

namespace TabNavApp
{

    /// <summary>
    /// the code-behind-class of the search-tags-tab
    /// as always: all members generated by the editor are situated in the 
    /// designer-part of this partial class and will not be commented upon (auto-generated!)
    /// </summary>
    public partial class SearchGraph : Page
    {
        private GraphControl graphControl;                                              //the graph-Control which will illustrate the surrounding concepts of a given concept

        private List<string>[] callbackInputGraph = new List<string>[4];                //reduced concept list for better graph visuals
        private List<string>[] callbackInputFull = new List<string>[4];                 //complete list of related concepts
        private Dictionary<string, string> uriDict = new Dictionary<string, string>();  //dict for all nodes concept-name - uri
        private Dictionary<string, string> topConcepts = new Dictionary<string, string>();  //topConcepts - uris
        private string newCenter = null;                                                //concept name of the center-node

        private SparqlResultsCallback gridMemberOfCallback;                             //delegate for the memberOf-list 
        private SparqlResultsCallback graphCallback;                                    //delegate for all graph-related callbacks
        private SparqlResultsCallback propertiesCallback;                               //delegate for the propertiesOf-callback (all informations at the top of the page)
        private SparqlResultsCallback topConceptCallback; 
        private DataGrid callbackGrid = null;                                           //non-visual placeholder for the searchResult-DataGrid

        VirtuosoSkosQuery query = null;                                                 //our query objec

        private bool CTR_preessed = false;                                              //indicated whether the CTR-button is pressed
        private bool startUpGraphShown = true;                                          //indicetes whether the current graph is the startup-graph (legend)

        #region Constants
        private const string STR_More = "MORE..."; 
        #endregion

        /// <summary>
        /// construktor (nothing special here)
        /// InitializeComponent() activates the layout defined by the editor and is 
        /// therefore the auto-generated part of this partial class
        /// </summary>
        public SearchGraph()
        {
            InitializeComponent();

            query = new VirtuosoSkosQuery();

            gridMemberOfCallback = new SparqlResultsCallback(memberOfCallbackFkt);          //delegates are initiated
            graphCallback = new SparqlResultsCallback(graphCallbackFkt);
            propertiesCallback = new SparqlResultsCallback(propertiesCallbackFkt);
            topConceptCallback = new SparqlResultsCallback(topConceptCallbackFkt);

            this.Loaded += new RoutedEventHandler(Page_Loaded);                             //events are activated
            this.SizeChanged += new SizeChangedEventHandler(SearchGraph_SizeChanged);
            //this.MouseLeftButtonDown += new MouseButtonEventHandler(SearchGraph_MouseLeftButtonDown);

            query.topGraphConcepts(topConceptCallback);

            this.Arrange(new Rect(new Point(0, 0), this.RenderSize));
            this.UpdateLayout();
            this.memberOfDG.Focus();
            
        }
        /// <summary>
        /// will automatically change the outline of a graph if the parent-container-size changes
        /// </summary>
        /// <param name="sender">sender object</param>
        /// <param name="e">special Eventargs for this event (includes old size, new size and parent-container-object)</param>
        void SearchGraph_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (graphControl != null)
                graphControl.ChangeSize(e.NewSize);

            if (graphBox != null)
            {
                graphBox.Width = this.RenderSize.Width - graphBox.Margin.Left - graphBox.Margin.Right;
                graphBox.Height = this.RenderSize.Height - graphBox.Margin.Bottom - graphBox.Margin.Top;
            }

        }
        
        /// <summary>
        /// receives result-call from triple-store and generates columns, bindings and headers
        /// for the memberOf-DataGrid
        /// </summary>
        /// <param name="set">the resultset</param>
        /// <param name="state">additional sender object (no relevance here)</param>
        private void memberOfCallbackFkt(SparqlResultSet set, object state)
        {
            List<ReturnRow> zw = null;
            //thread-crossing!
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                zw = StaticHelper.convertSparqlResultToListReturnRow(set);        //convert SPARQLResultSet zu a List<ReturnRow> object
                callbackGrid = (state as DataGrid);                                 //state object carries the DataGrid which receives a new DataSource
                callbackGrid.AutoGenerateColumns = false;
                generateGridColumns(ref callbackGrid, zw);                          //initialize columns of the grid
                callbackGrid.ItemsSource = zw;                                      //will populate DataGrid automatically
            });
            
        }
        /// <summary>
        /// receives result-call from triple-store and fills the 
        /// textblocks of the title, alternative-tags and discription
        /// </summary>
        /// <param name="set">the resultset</param>
        /// <param name="state">additional sender object (no relevance here)</param>
        private void propertiesCallbackFkt(SparqlResultSet set, object state)
        {
            List<ReturnRow> zw = null;
            //thread-crossing!
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                this.altLabelTB.Text = "alternative names: ";
                zw = StaticHelper.convertSparqlResultToListReturnRow(set);        //convert SPARQLResultSet zu a List<ReturnRow> object

                foreach (ReturnRow row in zw)
                {
                    if (row.val_0.ToString().Contains("altLabel"))                  //fill text-blocks with triple-stroe answers
                        this.altLabelTB.Text = this.altLabelTB.Text + row.val_1.ToString() + " ";
                    if (row.val_0.ToString().Contains("scopeNote"))
                        this.discriptionTB.Text = row.val_1.ToString();
                    else
                        this.discriptionTB.Text = "";
                }
                this.prefLabelTB.Text = this.newCenter;
            });

        }

        /// <summary>
        /// receives result-call from triple-store and creates the input-lists for a new graph
        /// </summary>
        /// <param name="set">the resultset</param>
        /// <param name="state">additional sender object (no relevance here)</param>
        private void graphCallbackFkt(SparqlResultSet set, object state)
        {
            List<string> list = null;   //saves concept-names
            List<ReturnRow> zw = null;  //used as temporary data-holders
            //thread-crossing!
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                //generate a ReturnRow-list
                zw = StaticHelper.convertSparqlResultToListReturnRow(set);
                list = new List<string>();

                 if (zw != null)                                                    //fill uriDict
                 {
                     for (int i = 1; i < zw.Count; i++)
                     {
                         list.Add(zw[i].val_1);                                     //val_0 = uri, val_1=conceptname(by default)
                         string key = zw[i].val_1.ToString();

                         if (zw[i].val_0.GetType() == typeof(Uri))                  //if typeof(Uri)
                         {
                             if (!uriDict.Keys.Contains(key))
                                 uriDict.Add(zw[i].val_1, (zw[i].val_0 as Uri).AbsoluteUri);        //adds new uri to dict
                         }
                         else if (zw[i].val_0.GetType() == typeof(string))          //if typeof(string)
                         {
                             if (!uriDict.Keys.Contains(key))
                                 uriDict.Add(zw[i].val_1, zw[i].val_0);             //adds new uri to dict
                         }
                     }
                 }

                 callbackInputFull[(int)state] = list;                                      //callbackInputFull gets every result-node
                 if (list.Count > 7)                                                        //if there are more than 7 nodes in one direction-list
                 {
                     callbackInputGraph[(int)state] = list.Take(6).ToList();                //take first 6 and add an additional node to the (shown) graph-node-list
                     callbackInputGraph[(int)state].Add(STR_More);                          //create an additional custom node (MORE...)
                 }
                 else                                                                       //less than 8 nodes in one direction-list
                     callbackInputGraph[(int)state] = list;                                 //take whole list

                 bool notEmpty = true;
                 foreach (List<string> l in callbackInputFull)                              //checks if all direction-node-lists are in (since callbacks are ansynchronous)
                 {
                     if (l == null)
                         notEmpty = false;

                 }

                 if (notEmpty)                                                              //if all direction-node-lists are in place -> create graph
                 {
                     if (graphControl == null || startUpGraphShown)                         //if no graph exists or just a startup-graph -> init new graph
                         initNewGraph(this.callbackInputGraph, this.newCenter);             //new graph is initialized
                     else
                         graphControl.SetData(this.newCenter, this.callbackInputGraph);     //otherwise update graph-data with the new node-lists
                 }
            });
        }

        /// <summary>
        /// defines DataGrid-columns and add those to a referenced DataGrid
        /// </summary>
        /// <param name="grid">the referenced DataGrid</param>
        /// <param name="zw">source result-set</param>
        private void generateGridColumns(ref DataGrid grid, List<ReturnRow> zw)
        {
            grid.Columns.Clear();                                                                               //remove all columns

            foreach (PropertyInfo prop in zw[0].GetType().GetProperties())                                      //read first list entry to generate column headers
            {
                if (prop.GetValue(zw[0], null) != null)                                                         //if entry = null -> no column is needed for this property
                {
                    DataGridTextColumn col = new DataGridTextColumn();
                    col.Header = prop.GetValue(zw[0], null).ToString();                                         //header-text
                    col.Binding = new System.Windows.Data.Binding(prop.Name);                                   //data-binding of column

                    if (new string[] { "uri", "relevance" }.Contains(col.Header.ToString().ToLower())) //properties which have little relevance for a user are not visible
                        col.Visibility = System.Windows.Visibility.Collapsed;

                    grid.Columns.Add(col);
                }
            }
            zw.RemoveAt(0);                                                                                     //first row (column-headers) is removed
            DataGridTemplateColumn buttonCol = new DataGridTemplateColumn();                                    //new column which holds the buttons for initializing a graph with this concept as center-node
            if (grid.Equals(this.memberOfDG))
                buttonCol.CellTemplate = (DataTemplate)Resources["MemberButton"];
            else
                buttonCol.CellTemplate = (DataTemplate)Resources["ColButton"];

            grid.Columns.Insert(0, buttonCol);                                                                  //is placed at front
        }

        /// <summary>
        /// evaluates the button_click on one of the buttons in the memberOf-DataGrid
        /// </summary>
        /// <param name="sender">the button</param>
        /// <param name="e">not needed</param>
        private void MembersButton_Click(object sender, RoutedEventArgs e)
        {
            var row = DataGridRow.GetRowContainingElement(sender as FrameworkElement);
            string uri = (row.DataContext as ReturnRow).val_0.ToString();                               //val_0 is always an uri
            newCenter = "members of: " + (row.DataContext as ReturnRow).val_1.ToString();

            callbackInputFull[0] = callbackInputFull[2] = callbackInputFull[3] = new List<string>();    //reset the graph-input-node-lists
            callbackInputGraph[0] = callbackInputGraph[2] = callbackInputGraph[3] = new List<string>();

            callbackInputFull[1] = callbackInputGraph[1] = null;                                        //only narrower childs (list 1) should display members of collection (to resemble a member-of state)

            query.getMembersOf(graphCallback, uri, OrderDirection.ASC, 1);                              //initiates a memberOf-query
            this.memberOfDG.Focus();
        }
        /// <summary>
        /// evaluates the button_click on one of the buttons in searchReesult-DataGrid
        /// </summary>
        /// <param name="sender">the button</param>
        /// <param name="e">not needed</param>
        public void NavigateToGraphConcept(string conceptUri, string conceptName)
        {
            uriDict.Clear();
            newCenter = conceptName;                //name new center-node
            callbackInputFull[0] = callbackInputFull[1] = callbackInputFull[2] = callbackInputFull[3] = null;  //reset the graph-input-node-lists
            callbackInputGraph[0] = callbackInputGraph[1] = callbackInputGraph[2] = callbackInputGraph[3] = null;

            //call on the triple store to update data, answers will be received through the callback-functions
            query.memberOf(gridMemberOfCallback, conceptUri, OrderDirection.ASC, memberOfDG);  //first: fill memberOf-DataGrid
            query.broader(graphCallback, conceptUri, OrderDirection.NONE, 0);                  //now: fill node-lists with  broader, narrower, related ans sibling concepts of the center-node-concept
            query.narrower(graphCallback, conceptUri, OrderDirection.NONE, 1);
            query.siblings(graphCallback, conceptUri, OrderDirection.NONE, 2);
            query.relatedTo(graphCallback, conceptUri, OrderDirection.NONE, 3);
            query.propertiesOf(propertiesCallback, conceptUri, OrderDirection.NONE, null);     //get addition properties to fill discription and alternative tags

        } 


        /// <summary>
        /// event:on Page_Loaded: load statrtup-graph: aka:legend
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Page_Loaded(object sender, RoutedEventArgs e)
        {
            initStartupGraph();
        }
        /// <summary>
        /// generates a a simple graph which is intended as a legend for the dynamic 'real' graph
        /// (for details compare with initNewGraph())
        /// </summary>
        private void initStartupGraph()
        {
            startUpGraphShown = true;                                                       //yes this is a startup-graph
            graphControl = new GraphControl(new Size(graphBox.Width, graphBox.Height));     //new graph control
           
            //some legend informations
            string center = "current concept\nthe surrounding concepts are \nin a direct or indirect relation" +
                "\nto the current one\n(distinguished by direction & color)";
            List<string>[] inputLists = new List<string>[4];
            inputLists[0] = new List<string>() { "broader concepts" };
            inputLists[1] = new List<string>() { "narrower concepts" };
            inputLists[2] = new List<string>() { "sibling concepts" };
            inputLists[3] = new List<string>() { "related concepts" };
            
            //no click-logic on startup-graph please
            graphControl.NodeClicked -= new GraphControl.NodeClickedEventHandler(control_NodeClicked);
            //transfer data to the graph-control
            graphControl.SetData(center, inputLists);
            // Add the result to the current screen //
            this.graphBox.Child = graphControl.Graphic;
            this.graphBox.UpdateLayout();
        }

        /// <summary>
        /// generates a new graph-graphic
        /// </summary>
        /// <param name="children">the four node-lists which represents broader, narrower, related and sibling concepts of center-node-concept</param>
        /// <param name="center">the center-node-concept</param>
        private void initNewGraph(List<string>[] children, string center)
        {
            startUpGraphShown = false;                                                  //no this is the real deal
            graphControl = new GraphControl(new Size(graphBox.Width, graphBox.Height)); //a new graph control

            graphControl.SetData(center, children);                                     //transfer input-data zu the graph control

            // Add the result to the current screen //
            this.graphBox.Child = graphControl.Graphic;                                 // Add the result to the current screen
            this.graphBox.UpdateLayout();

            //activate click event for node-clicks
            graphControl.NodeClicked += new GraphControl.NodeClickedEventHandler(control_NodeClicked);                
        }
        /// <summary>
        /// event: is fired when clicking a node with the left mouse-button
        /// it centers the graph on the node just clicked and calls for its surrounding concepts from the triple-store
        /// or shows all nodes of a direction if the special node (MORE...) was clicked
        /// or implements a on-click-tagging mechanism
        /// </summary>
        /// <param name="e">special event-args: includes: name of the node clicked and its general direction (indicating broader, relatedto...)</param>
        void control_NodeClicked(NodeEventArgs e)
        {
            if (CTR_preessed)                                   //if ctrl-button is pressed while pressing left mouse button
            {
                if (!e.Title.Contains("members of "))    //not!
                {
                    string uri = "";
                    uriDict.TryGetValue(e.Title, out uri);        //get uri corresponding to the name of the clicked graph from uri-Dictionary
                    SparqlResultsCallback getReturnTagCallback = new SparqlResultsCallback(getReturnTagCallbackFkt);
                    if(uri != null)
                        query.getReturnTag(uri, getReturnTagCallback, null);
                }
            }
            else
            {
                if (e.Title == STR_More)   //all excluded concepts (due to the lage number of concepts in a specific direction) are singled out and shown in a new graph
                {
                    List<string>[] zw = new List<string>[4];        //temp-list
                    if (e.PrefDirX > 0)                             //evaluate the general direction of the clicked node
                        zw[3] = callbackInputFull[3];               //replace the node-list of this direction with the full node-list of the callbackInputFull 'node-save'
                    else if (e.PrefDirX < 0)
                        zw[2] = callbackInputFull[2];
                    else if (e.PrefDirY < 0)
                        zw[0] = callbackInputFull[0];
                    else if (e.PrefDirY > 0)
                        zw[1] = callbackInputFull[1];

                    for (int i = 0; i < zw.Count(); i++)            //each other direction gets an empty list
                    {
                        if (zw[i] == null)
                            zw[i] = new List<string>();
                    }

                    graphControl.SetData(this.newCenter, zw);        //update graph-data
                }
                else                                                 //update graph for the clicked concept
                {
                    if (e.Title.Contains("members of "))            //special treatment if node is central node of a member-of graph (when a button in the memberOf-DataGrid was clicked beforehand)
                        newCenter = e.Title.Replace("members of ", ""); 
                    else
                        newCenter = e.Title;

                    string uri = "";                
                    uriDict.TryGetValue(newCenter, out uri);        //get uri corresponding to the name of the clicked graph from uri-Dictionary

                    callbackInputFull[0] = callbackInputFull[1] = callbackInputFull[2] = callbackInputFull[3] = null;  //reset the graph-input-node-lists
                    callbackInputGraph[0] = callbackInputGraph[1] = callbackInputGraph[2] = callbackInputGraph[3] = null;

                    //call on the triple store to update data, answers will be received through the callback-functions
                    query.memberOf(gridMemberOfCallback, uri, OrderDirection.ASC, memberOfDG);              //first fill the memberOf DataGrid    
                    query.broader(graphCallback, uri, OrderDirection.NONE, 0);                              //now: fill node-lists with  broader, narrower, related ans sibling concepts of the center-node-concept
                    query.narrower(graphCallback, uri, OrderDirection.NONE, 1);
                    query.siblings(graphCallback, uri, OrderDirection.NONE, 2);
                    query.relatedTo(graphCallback, uri, OrderDirection.NONE, 3);
                    query.propertiesOf(propertiesCallback, uri, OrderDirection.NONE, null);                 //get addition properties to fill discription and alternative tags

                }
            }
            this.memberOfDG.Focus();
        }
        /// <summary>
        /// callback methode for the getReturnTagCallback delegate, adds a tag to the tag-list in the MainView 
        /// </summary>
        /// <param name="set">the query result set</param>
        /// <param name="iii">not used</param>
        private void getReturnTagCallbackFkt(SparqlResultSet set, object iii)
        {
            string altL ="";
            string desc ="";
            if(set.Results[0]["altLabels"] != null)
                   altL = (set.Results[0]["altLabels"] as LiteralNode).Value;
            if(set.Results[0]["description"] != null)
                   desc = (set.Results[0]["description"] as LiteralNode).Value;

            //create new list state with normal list and sticked list
            Api.Tags.Tag[] items = new Api.Tags.Tag[0];
            Api.Tags.Tag[] stickies = new Api.Tags.Tag[0];
            if (TabNavApp.Api.Common.ListController<TabNavApp.Api.Tags.Tag>.lastSearchStack.Count == 0)
                TabNavApp.Api.Common.ListController<TabNavApp.Api.Tags.Tag>.lastSearchStack.Push(new Api.Common.ListState<Api.Tags.Tag>(stickies, items));
            else      //push new state on the stack
            {
                items = TabNavApp.Api.Common.ListController<TabNavApp.Api.Tags.Tag>.lastSearchStack.Peek().ItemList;
                stickies = TabNavApp.Api.Common.ListController<TabNavApp.Api.Tags.Tag>.lastSearchStack.Peek().StickiedList;
                TabNavApp.Api.Common.ListController<TabNavApp.Api.Tags.Tag>.lastSearchStack.Push(new Api.Common.ListState<Api.Tags.Tag>(stickies, items));
            }

            TabNavApp.Api.Common.ListController<TabNavApp.Api.Tags.Tag>.lastSearchStack.Peek()  //ad selected concept as tag to the tag lsit
                .AddStickedItem(new Api.Tags.Tag()
                {
                    AltLabels = altL,
                    Description = desc,
                    Name = (set.Results[0]["name"] as LiteralNode).Value,
                    Uri = (set.Results[0]["uri"] as UriNode).Uri.AbsoluteUri
                });
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                MessageBox.Show("The tag: " + (set.Results[0]["name"] as LiteralNode).Value + " has been added to your tag list");
            });
        }
        /// <summary>
        /// sets the CTR-button state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void tabControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Ctrl)
                CTR_preessed = true;
        }

        private void tabControl_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Ctrl)
                CTR_preessed = false;
        }
        /// <summary>
        /// event methode: triggered by selecting a top concept from the drop-down-buttom
        /// </summary>
        /// <param name="sender">the drop-down-button</param>
        /// <param name="e">event args</param>
        private void topConceptCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string uri = null;
            topConcepts.TryGetValue(e.AddedItems[0].ToString(), out uri);   //get the corresponding graph-uri stored in this dictionary
            if (uri != null)
                NavigateToGraphConcept(uri, e.AddedItems[0].ToString());    //navigate to the selected concept
        }
        /// <summary>
        /// callback methode for the topConceptCallback delegate, fills the topConceptCB at startup
        /// </summary>
        /// <param name="set">the query-result</param>
        /// <param name="state">not used</param>
        private void topConceptCallbackFkt(SparqlResultSet set, object state)
        {
            //thread crossing
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                foreach(SparqlResult res in set)        //fill the dictionary with name - graph uri tuples
                    this.topConcepts.Add((res["conceptName"] as LiteralNode).Value, (res["topConcept"] as UriNode).Uri.AbsoluteUri);
                if(topConcepts.Count >0)
                    topConceptCB.ItemsSource = topConcepts.Keys;        //make concept-names the dataSource of the drop down buttom
            });
        }
    }
}