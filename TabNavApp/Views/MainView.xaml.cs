using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;
using VirtuosoQuery;
using VirtuosoQuery.Silverlight.Skos;
using VirtuosoQuery.Silverlight.Docs;
using VDS.RDF;
using TabNavApp.Api.Documents;
using TabNavApp.Api.Common;
using System.Windows.Data;
using VDS.RDF.Query;
using System.Threading;
using TabNavApp.Api.Tags;
using System.Windows.Browser;

namespace TabNavApp.Views
{
    public partial class MainView : Page
    {
        public AutoResetEvent stopWaitHandle = new AutoResetEvent(false);  
        private VirtuosoSkosQuery query = null;                                                 //our query object
        private VDS.RDF.Query.SparqlRemoteEndpoint endpoint = null;                             //the endpoint object
        private VirtuosoSkosQuery.SearchResultCallback searchTagCall = null;
        private SparqlResultsCallback docQueryCallback = null;
        private SparqlResultsCallback docTagsCallback = null;   
        private DocGraphQuery docQuery = null;

        public delegate void EventHandler(object sender, EventArgs args);
        public event EventHandler TagButton_Clicked = null;

        public ListController<Document> documentController { get; set; }
        public ListController<Tag> tagController { get; set; }

        private ContextMenu docListContextM = new ContextMenu();
        private ContextMenu deleteTagsContextM = new ContextMenu();
        private ContextMenu tagListContextM = new ContextMenu();

        public MainView()
        {
            InitializeComponent();
            PropertyChangedCallback ItemSourceChangedCallback = new PropertyChangedCallback(ItemSourceChangedCallbackFkt);
            RegisterForNotification("ItemsSource", this.TagListBox, ItemSourceChangedCallback);
            TagButton_Clicked = new EventHandler(tagButtonClick);

            //Initialize list controller
            documentController = new ListController<Document>(DocumentListBox);
            tagController = new ListController<Tag>(TagListBox);
        }

        // Listen for change of the dependency property (in lieu of an event like ListBox.ItemSource_Changed)
        public void RegisterForNotification(string propertyName, FrameworkElement element, PropertyChangedCallback callback)
        { //Bind to a depedency property 
            Binding b = new Binding(propertyName) { Source = element };
            var prop = System.Windows.DependencyProperty.RegisterAttached
                ("ListenAttached" + propertyName, typeof(object), typeof(UserControl), new System.Windows.PropertyMetadata(callback));
            element.SetBinding(prop, b);
        }

        // Executes when the user navigates to this page.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (TabNavApp.Api.Common.ListController<TabNavApp.Api.Tags.Tag>.lastSearchStack != null 
                || TabNavApp.Api.Common.ListController<TabNavApp.Api.Documents.Document>.lastSearchStack != null)
            {
                if (TabNavApp.Api.Common.ListController<TabNavApp.Api.Documents.Document>.lastSearchStack != null)
                {
                    lastDocViewBT_Click(new object(), new RoutedEventArgs());
                }
                if (TabNavApp.Api.Common.ListController<TabNavApp.Api.Tags.Tag>.lastSearchStack != null)
                {
                    lastTagViewBT_Click(new object(), new RoutedEventArgs());
                }
            }
            else if (MainPage.DocumentsLoaded)
            {
                documentController.Add(MainPage.LoadedDocuments);
                documentController.Stick(MainPage.LoadedDocuments);
                documentController.Update(true);
            }

            ////endpoint, query and callback object are initiated
            Constants.InitializeDefault();
            endpoint = new VDS.RDF.Query.SparqlRemoteEndpoint(UriFactory.Create(Constants.endpointAddress), Constants.skosGraphUri);
            docQuery = new DocGraphQuery(endpoint, Constants.skosGraphUri, Constants.docGraphUri);
            query = new VirtuosoSkosQuery(endpoint, Constants.skosGraphUri, Constants.docGraphUri);
            searchTagCall = new VirtuosoSkosQuery.SearchResultCallback(searchResultCallback);
            docQueryCallback = new SparqlResultsCallback(docQueryCallbackFkt);
            docTagsCallback = new SparqlResultsCallback(getTagsOfDocCallbackFkt);
        }


        private void ItemSourceChangedCallbackFkt(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //foreach (Item it in tagController.Items)
            //    if (it.Control != null && it.Background != null)
            //        it.Control.Background = it.Background;
        }

        private void tagSearchBT_Click(object sender, RoutedEventArgs e)
        {
            query.searchTag(searchTagCall, tagSearchTB.Text, 50);
            tagController.Clear();
        }

        /// <summary>
        /// receives result-call from triple-store and fills the 
        /// search-result-DataGrid
        /// </summary>
        /// <param name="set">the resultset</param>
        private void searchResultCallback(Tuple<string[], List<object[]>> set)
        {
            //thread-crossing!
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                List<ReturnTag> zw = ConverterClass.convertListStringArrayToTagList(set);     //convert SPARQLResultSet
                List<Tag> tags = new List<Tag>();
                foreach(ReturnTag ret in zw)
                   tags.Add(new Api.Tags.Tag(){ Name = ret.prefLabel,  Uri = ret.uri,  AltLabels = ret.altLabels,  Description = ret.description});
                tagController.Add(tags.ToArray());
                tagController.Update(true);
            });
        }

        public void tagButtonClick(object sender, EventArgs e)
        {
            List<ReturnDocument> zw = new List<ReturnDocument>();
            foreach (Document doc in documentController.StickedItems)
            {
                if(doc.Tags == null)
                    doc.Tags = new Dictionary<string, string>();
                
                doc.Tags.Add(((sender as TabNavApp.Api.Tags.TagListItem).DataContext as Tag).Uri, ((sender as TabNavApp.Api.Tags.TagListItem).DataContext as Tag).Name);
                zw.Add(new ReturnDocument()
                {
                    Author = doc.Author,
                    CreationDate = doc.CreationDate,
                    ListType = Entry.Document,
                    UniqueID = doc.UniqueID,
                    Name = doc.Name,
                    ListID = doc.ListID,
                    server = doc.server,
                    Tags = doc.Tags.Keys.ToList()
                });
            }
            docQuery.insertEntries(zw);
            MessageBox.Show("All sticked documents were tagged with " + ((sender as TabNavApp.Api.Tags.TagListItem).DataContext as Tag).Name);
        }

        private void docSearchTB_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                docSearchBT_Click(sender, new RoutedEventArgs());
        }

        private void docSearchBT_Click(object sender, RoutedEventArgs e)
        {
            var Script = HtmlPage.Document.CreateElement("script");
            Script.SetAttribute("type", "text/javascript");
            Script.SetProperty("text", "function CloseDialog() { window.frameElement.commitPopup('Closed with OK result'); return false;}");
            //"return ExecuteOrDelayUntilScriptLoaded(" +
            //"function pop() { SP.UI.ModalDialog.close(SP.UI.DialogResult.OK); }, \"sp.js\");");
            //"var options = SP.UI.$create_DialogOptions(); options.width = 1080; options.height = 602;" +
            //"options.url = \"/_Layouts/SkosTagFeatures/MainPage/MainPage.aspx?\" + params;" +
            //"window.parent.SP.UI.ModalDialog.showModalDialog(options); }, \"sp.js\");}");
            HtmlPage.Document.DocumentElement.AppendChild(Script);
            this.Dispatcher.BeginInvoke(() => CloseWindow());
        }

        public void CloseWindow()
        {

            //HtmlPage.Window.Invoke("CloseDialog");
        }

        private void tagSearchTB_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                tagSearchBT_Click(sender, new RoutedEventArgs());
        }

        private void docQueryCallbackFkt(SparqlResultSet set, object state)
        {
            //thread-crossing!
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                List<ReturnDocument> zw = ConverterClass.convertSparqlResultToListReturnDocument(set);     //convert SPARQLResultSet
                List<Document> docs = new List<Document>();
                foreach (ReturnDocument doc in zw)
                {
                    docs.Add(new Api.Documents.Document()
                    {
                        Author = doc.Author,
                        CreationDate = doc.CreationDate,
                        ListID = doc.ListID,
                        ListType = doc.ListType,
                        Name = doc.Name,
                        server = doc.server,
                        UniqueID = doc.UniqueID
                    });
                    docQuery.getDocTags(doc.UniqueID, docTagsCallback, docs.Last(), VirtuosoQuery.Silverlight.Docs.OrderDirection.ASC);
                }
                documentController.Clear();
                documentController.Add(docs.ToArray());
                documentController.Update(true);
            });
        }

        private void searchDocsByStickyTagsBT_Click(object sender, RoutedEventArgs e)
        {
            List<string> tags = new List<string>();
            foreach(Tag tag in tagController.StickedItems)
                tags.Add(tag.Uri);
            if(tags.Count>0)
                docQuery.getDocsByTags(tags, docQueryCallback, null, VirtuosoQuery.Silverlight.Docs.OrderDirection.ASC);
        }

        private void getTagsOfStickyDocsBT_Click(object sender, RoutedEventArgs e)
        {
            this.tagController.ClearAll();
            foreach (Document doc in documentController.StickedItems)
                docQuery.getDocTags(doc.UniqueID, docTagsCallback, this.tagController, VirtuosoQuery.Silverlight.Docs.OrderDirection.ASC);
            tagController.Update(true);
        }

        private void getTagsOfDocCallbackFkt(SparqlResultSet set, object dict)
        {
                        //thread-crossing!
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                List<ReturnTag> zw = ConverterClass.convertSparqlResultToListReturnTag(set);

                if (dict.GetType() == typeof(Document))
                {
                    if ((dict as Document).Tags == null)
                        (dict as Document).Tags = new Dictionary<string, string>();
                    foreach (ReturnTag tag in zw)
                        (dict as Document).Tags.Add(tag.uri, tag.prefLabel);
                }
                else if (dict.GetType() == typeof(ListController<Tag>))
                {
                    List<Tag> tags = new List<Tag>();
                    foreach (ReturnTag tag in zw)
                        tags.Add(new Tag() { Uri = tag.uri, Description = tag.description, AltLabels = tag.altLabels, Name = tag.prefLabel });
                    (dict as ListController<Tag>).Add(tags.ToArray());
                    (dict as ListController<Tag>).Update(true);
                }
            });
        }

        void AllTagsOfDocument_Click(object sender, RoutedEventArgs e)
        {
            this.tagController.ClearAll();
            if(docListContextM.DataContext.GetType() == typeof(Document))
            {
                docQuery.getDocTags((docListContextM.DataContext as Document).UniqueID, docTagsCallback, this.tagController, VirtuosoQuery.Silverlight.Docs.OrderDirection.ASC);
            }
            tagController.Update(true);
        }

        void AllDocsOfTags_Click(object sender, RoutedEventArgs e)
        {
            if (docListContextM.DataContext != null && docListContextM.DataContext.GetType() == typeof(Document))
            {
                docQuery.getDocsByTags((docListContextM.DataContext as Document).Tags.Keys.ToList(), docQueryCallback, null, VirtuosoQuery.Silverlight.Docs.OrderDirection.ASC);
            }
            else if (tagListContextM != null && tagListContextM.DataContext.GetType() == typeof(Tag))
            {
                docQuery.getDocsByTags(new List<string>(){(tagListContextM.DataContext as Tag).Uri}, docQueryCallback, null, VirtuosoQuery.Silverlight.Docs.OrderDirection.ASC);
            }
            tagController.Update(true);
        }

        public void LoadTagListBoxContextMenu(object sender)
        {
            tagListContextM.Items.Clear();
            tagListContextM.DataContext = (sender as TagListItem).DataContext;

            MenuItem mItem = new MenuItem();
            mItem.Header = "show all Tags of this document";
            tagListContextM.Items.Add(mItem);
            mItem.Click += new RoutedEventHandler(AllDocsOfTags_Click);

            tagListContextM.IsOpen = true;
        }

        public void LoadDocumentListBoxContextMenu(object sender)
        {
            docListContextM.Items.Clear();
            docListContextM.DataContext = (sender as DocumentListItem).DataContext;

            MenuItem mItem = new MenuItem();
            mItem.Header = "show all Tags of this document";
            docListContextM.Items.Add(mItem);
            mItem.Click += new RoutedEventHandler(AllTagsOfDocument_Click);

            mItem = new MenuItem();
            mItem.Header = "show all documents with same Tags";
            docListContextM.Items.Add(mItem);
            mItem.Click += new RoutedEventHandler(AllDocsOfTags_Click);

            mItem = new MenuItem();
            mItem.Header = "delete Tags";
            mItem.DataContext = (sender as DocumentListItem).DataContext;
            docListContextM.Items.Add(mItem);
            mItem.Click += new RoutedEventHandler(loadDeleteTagsContextMenu);

            docListContextM.IsOpen = true;
        }

        void loadDeleteTagsContextMenu(object sender, RoutedEventArgs e)
        {
            deleteTagsContextM.Items.Clear();

            MenuItem mItem = new MenuItem();
            mItem.Header = "delete one Tag:";
            this.deleteTagsContextM.Items.Add(mItem);

            foreach (KeyValuePair<string, string> val in ((sender as MenuItem).DataContext as Document).Tags)
            {
                mItem = new MenuItem();
                mItem.Header = val.Value;
                mItem.DataContext = new Tuple<string, string, Document>(((sender as MenuItem).DataContext as Document).UniqueID, val.Key, (sender as MenuItem).DataContext as Document);
                this.deleteTagsContextM.Items.Add(mItem);
                mItem.Click += new RoutedEventHandler(deleteOneTag);
            }

            deleteTagsContextM.IsOpen = true;
        }

        void deleteOneTag(object sender, RoutedEventArgs e)
        {
            if (MessageBoxResult.OK == MessageBox.Show("You want to delete the Tag: " + (sender as MenuItem).Header + " ?", "Delete a Tag", MessageBoxButton.OKCancel))
            {
                //delete Tag between Doc with Guid xxx and Tag with Uri yyy
                docQuery.deleteTagByGuid(((sender as MenuItem).DataContext as Tuple<string, string, Document>).Item1, ((sender as MenuItem).DataContext as Tuple<string, string, Document>).Item2);
                //delete Tag from TagList in DataContext-Document
                ((sender as MenuItem).DataContext as Tuple<string, string, Document>).Item3.Tags.Remove(((sender as MenuItem).DataContext as Tuple<string, string, Document>).Item2);
            }
        }
        private void lastTagViewBT_Click(object sender, RoutedEventArgs e)
        {
            
            if (ListController<Tag>.lastSearchStack.Count > 0)
            {
                ListController<Tag>.nextSearchStack.Push(new ListState<Api.Tags.Tag>(tagController.StickedItems.ToArray(), tagController.Items.ToArray()));
                var state = ListController<Tag>.lastSearchStack.Pop();
                tagController.ClearAll();
                tagController.Add(state.ItemList);
                tagController.StickedItems = state.StickiedList.ToList();
                //tagController.Stick(state.stickiedList);
                tagController.Update(false);
            }
        }

        private void nextTagViewBT_Click(object sender, RoutedEventArgs e)
        {
            if (ListController<Tag>.nextSearchStack.Count > 0)
            {
                ListController<Tag>.lastSearchStack.Push(new ListState<Api.Tags.Tag>(tagController.StickedItems.ToArray(), tagController.Items.ToArray()));
                var state = ListController<Tag>.nextSearchStack.Pop();
                tagController.ClearAll();
                tagController.Add(state.ItemList);
                tagController.StickedItems = state.StickiedList.ToList();
                //tagController.Stick(state.stickiedList);
                tagController.Update(false);
            }
        }

        private void lastDocViewBT_Click(object sender, RoutedEventArgs e)
        {
            if (ListController<Document>.lastSearchStack.Count > 0)
            {
                ListController<Document>.nextSearchStack.Push(new ListState<Document>(documentController.StickedItems.ToArray(), documentController.Items.ToArray()));
                var state = ListController<Document>.lastSearchStack.Pop();
                documentController.ClearAll();
                documentController.Add(state.ItemList);
                documentController.StickedItems = state.StickiedList.ToList();
                //documentController.Stick(state.stickiedList);
                documentController.Update(false);
            }
        }

        private void nextDocViewBT_Click(object sender, RoutedEventArgs e)
        {
            if (ListController<Document>.nextSearchStack.Count > 0)
            {
                ListController<Document>.lastSearchStack.Push(new ListState<Document>(documentController.StickedItems.ToArray(), documentController.Items.ToArray()));
                var state = ListController<Document>.nextSearchStack.Pop();
                documentController.ClearAll();
                documentController.Add(state.ItemList);
                documentController.StickedItems = state.StickiedList.ToList();
                //documentController.Stick(state.stickiedList);
                documentController.Update(false);
            }
        }

        private void LayoutRoot_Unloaded(object sender, RoutedEventArgs e)
        {
            if (tagController != null)
            {
                tagController.Update(true);
            }
            if (documentController != null)
            {
                documentController.Update(true);
            }
        }
    }
}
