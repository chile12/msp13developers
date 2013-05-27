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
using VirtuosoQuery.Silverlight.Entry;
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
    /// <summary>
    /// the tagging-page provides full tagging control
    /// in general: all references to 'Document' means any entry of any type (e.g. Event,  Discussion or Documnet)
    /// </summary>
    public partial class MainView : Page
    {
        public AutoResetEvent stopWaitHandle = new AutoResetEvent(false);                      // 
        private VirtuosoSkosQuery query = null;                                                 //our query object
        private VirtuosoSkosQuery.SearchResultCallback searchTagCall = null;                    //callback deleagat for tag-search calls
        private SparqlResultsCallback docQueryCallback = null;                                  //callback delegate for document-searchs
        private SparqlResultsCallback docTagsCallback = null;                                   //callback delegate for 'all tags of this document'
        private EntryGraphQuery docQuery = null;                                                //query object for document related queries

        
        public delegate void EventHandler(object sender, EventArgs args);                       //
        public event EventHandler TagButton_Clicked = null;
        public event VirtuosoQuery.StaticHelper.EventHandler configLoaded = null;    //

        public ListController<Document> documentController { get; set; }
        public ListController<Tag> tagController { get; set; }

        private ContextMenu docListContextM = new ContextMenu();
        private ContextMenu deleteTagsContextM = new ContextMenu();
        private ContextMenu tagListContextM = new ContextMenu();

        private const int INT_topKresults = 30;

        public MainView()
        {
            InitializeComponent();
            PropertyChangedCallback ItemSourceChangedCallback = new PropertyChangedCallback(ItemSourceChangedCallbackFkt);
            RegisterForNotification("ItemsSource", this.TagListBox, ItemSourceChangedCallback);
            TagButton_Clicked = new EventHandler(tagButtonClick);
            configLoaded = new VirtuosoQuery.StaticHelper.EventHandler(configLoadedCallbackFkt);

            //Initialize list controller
            documentController = new ListController<Document>(DocumentListBox);
            tagController = new ListController<Tag>(TagListBox);

            VirtuosoQuery.StaticHelper.xmlConfigReader(MainPage.ConfigFileUrl, configLoaded); 
        }

        public void CloseWindowNavigateTo(string navigateToUrl)
        {
            ScriptObject so = HtmlPage.Window.GetProperty("closeWindow") as ScriptObject;
            so.InvokeSelf(new object[] { navigateToUrl });
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
            ////endpoint, query and callback object are initiated
            Constants.InitializeDefault();

            if (TabNavApp.Api.Common.ListController<TabNavApp.Api.Tags.Tag>.lastSearchStack.Count > 0 
                || TabNavApp.Api.Common.ListController<TabNavApp.Api.Documents.Document>.lastSearchStack.Count > 0)
            {
                if (TabNavApp.Api.Common.ListController<TabNavApp.Api.Documents.Document>.lastSearchStack.Count > 0)
                {
                    lastDocViewBT_Click(new object(), new RoutedEventArgs());
                }
                if (TabNavApp.Api.Common.ListController<TabNavApp.Api.Tags.Tag>.lastSearchStack.Count > 0)
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

            searchTagCall = new VirtuosoSkosQuery.SearchResultCallback(searchResultCallback);
            docQueryCallback = new SparqlResultsCallback(docQueryCallbackFkt);
            docTagsCallback = new SparqlResultsCallback(getTagsOfDocCallbackFkt);
            Thread.Sleep(500);
        }


        private void ItemSourceChangedCallbackFkt(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        private void tagSearchBT_Click(object sender, RoutedEventArgs e)
        {
            if (tagSearchTB.Text.Length > 2)
            {
                query.searchTag(searchTagCall, tagSearchTB.Text, INT_topKresults);
                tagController.Clear();
            }
            else
                MessageBox.Show("At least 3 characters needed.");
        }

        /// <summary>
        /// receives result-call from triple-store and fills the 
        /// search-result-DataGrid
        /// </summary>
        /// <param name="set">the resultset</param>
        private void searchResultCallback(Tuple<string[], List<object[]>> set)
        {
           // Deployment.Current.Dispatcher.BeginInvoke(() => { MessageBox.Show("antwort erhalten " + set.Item2.Count.ToString()); });
            //thread-crossing!
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                List<ReturnTag> zw = StaticHelper.convertListStringArrayToTagList(set);     //convert SPARQLResultSet
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
                string key = ((sender as TabNavApp.Api.Tags.TagListItem).DataContext as Tag).Uri;
                string value = ((sender as TabNavApp.Api.Tags.TagListItem).DataContext as Tag).Name;

                if (!doc.Tags.Keys.Contains(key))   //not!
                {
                    doc.Tags.Add(key, value);
                    zw.Add(new ReturnDocument()
                    {
                        Author = doc.Author,
                        CreationDate = doc.CreationDate,
                        ListType = doc.ListType,
                        UniqueID = doc.UniqueID,
                        Name = doc.Name,
                        ListID = doc.ListID,
                        server = doc.server,
                        Tags = doc.Tags.Keys.ToList()
                    }); 
                }
            }
            docQuery.insertEntries(zw);
            MessageBox.Show("All sticked entries were tagged with " + ((sender as TabNavApp.Api.Tags.TagListItem).DataContext as Tag).Name);
        }

        private void docSearchTB_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                docSearchBT_Click(sender, new RoutedEventArgs());
        }

        private void docSearchBT_Click(object sender, RoutedEventArgs e)
        {
            if (docSearchTB.Text.Length > 2)
            {
                docQuery.getEntriesByName(docSearchTB.Text, docQueryCallback, null);
                documentController.Clear();
            }
            else
                MessageBox.Show("At least 3 characters needed.");
        }

        public void CloseWindow()
        {

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
                List<ReturnDocument> zw = StaticHelper.convertSparqlResultToListReturnDocument(set);     //convert SPARQLResultSet
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
                    docQuery.getEntryTags(doc.UniqueID, docTagsCallback, docs.Last(), VirtuosoQuery.Silverlight.Entry.OrderDirection.ASC);
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
                docQuery.getEntriesByTags(tags, docQueryCallback, null);
        }

        private void getTagsOfStickyDocsBT_Click(object sender, RoutedEventArgs e)
        {
            this.tagController.Clear();
            foreach (Document doc in documentController.StickedItems)
                docQuery.getEntryTags(doc.UniqueID, docTagsCallback, this.tagController, VirtuosoQuery.Silverlight.Entry.OrderDirection.ASC);
            tagController.Update(true);
        }

        private void getTagsOfDocCallbackFkt(SparqlResultSet set, object dict)
        {
                        //thread-crossing!
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                List<ReturnTag> zw = StaticHelper.convertSparqlResultToListReturnTag(set);

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
                    //(dict as ListController<Tag>).Stick(tags.ToArray());
                    (dict as ListController<Tag>).Update(true);
                }
            });
        }

        void AllTagsOfDocument_Click(object sender, RoutedEventArgs e)
        {
            this.tagController.ClearAll();
            if(docListContextM.DataContext.GetType() == typeof(Document))
            {
                docQuery.getEntryTags((docListContextM.DataContext as Document).UniqueID, docTagsCallback, this.tagController, VirtuosoQuery.Silverlight.Entry.OrderDirection.ASC);
            }
            tagController.Update(true);
        }

        void AllDocsOfTags_Click(object sender, RoutedEventArgs e)
        {
            documentController.ClearAll();
            if (docListContextM.DataContext != null && docListContextM.DataContext.GetType() == typeof(Document))
            {
                docQuery.getEntriesByTags((docListContextM.DataContext as Document).Tags.Keys.ToList(), docQueryCallback, null);
            }
            else if (tagListContextM != null && tagListContextM.DataContext.GetType() == typeof(Tag))
            {
                docQuery.getEntriesByTags(new List<string>(){(tagListContextM.DataContext as Tag).Uri}, docQueryCallback, null);
            }
        }

        public void LoadTagListBoxContextMenu(object sender)
        {
            tagListContextM.Items.Clear();
            tagListContextM.DataContext = (sender as TagListItem).DataContext;

            MenuItem mItem = new MenuItem();
            mItem.Header = "show all Tags of this entry";
            tagListContextM.Items.Add(mItem);
            mItem.Click += new RoutedEventHandler(AllDocsOfTags_Click);

            tagListContextM.IsOpen = true;
        }

        public void LoadDocumentListBoxContextMenu(object sender)
        {
            docListContextM.Items.Clear();
            docListContextM.DataContext = (sender as DocumentListItem).DataContext;

            MenuItem mItem = new MenuItem();
            mItem.Header = "show all Tags of this entry";
            docListContextM.Items.Add(mItem);
            mItem.Click += new RoutedEventHandler(AllTagsOfDocument_Click);

            mItem = new MenuItem();
            mItem.Header = "show all entries with same Tags";
            docListContextM.Items.Add(mItem);
            mItem.Click += new RoutedEventHandler(AllDocsOfTags_Click);

            mItem = new MenuItem();
            mItem.Header = "show Tags";
            mItem.DataContext = (sender as DocumentListItem).DataContext;
            docListContextM.Items.Add(mItem);
            mItem.Click += new RoutedEventHandler(loadDeleteTagsContextMenu);

            mItem = new MenuItem();
            mItem.Header = "delete Tags";
            mItem.DataContext = (sender as DocumentListItem).DataContext;
            docListContextM.Items.Add(mItem);
            mItem.Click += new RoutedEventHandler(loadDeleteTagsContextMenu);

            mItem = new MenuItem();
            mItem.Header = "get tag-suggestions from FOX";
            mItem.DataContext = (sender as DocumentListItem).DataContext;
            docListContextM.Items.Add(mItem);
            mItem.Click +=new RoutedEventHandler(getFoxSuggestions);

            docListContextM.IsOpen = true;
        }

        void getFoxSuggestions(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This is a placeholder for a future implementation of an automated tag-proposal-feature based on the FOX-Framework");

            //uncomment if automated tag-suggestion-service is implemented

            //Document doc = docListContextM.DataContext as Document;
            //if (docListContextM.DataContext is Document)
            //{
            //    AsyncDocumentLoader loader = new AsyncDocumentLoader(MainPage.SiteUrl);
            //    loader.OpenDocumentFileStream(doc.UniqueID, doc.ListID);
            //}
        }

        void loadDeleteTagsContextMenu(object from, RoutedEventArgs e)
        {
            deleteTagsContextM.Items.Clear();
            MenuItem sender = from as MenuItem;
            MenuItem mItem = new MenuItem();

            if (sender.Header.ToString().Contains("delete"))
            {
                mItem.Header = "delete one Tag:";
                this.deleteTagsContextM.Items.Add(mItem);
            }

            if (((sender as MenuItem).DataContext as Document).Tags != null)
            {
                foreach (KeyValuePair<string, string> val in ((sender as MenuItem).DataContext as Document).Tags)
                {
                    mItem = new MenuItem();
                    mItem.Header = val.Value;
                    mItem.DataContext = new Tuple<string, string, Document>(((sender as MenuItem).DataContext as Document).UniqueID, val.Key, (sender as MenuItem).DataContext as Document);
                    this.deleteTagsContextM.Items.Add(mItem);

                    if (sender.Header.ToString().Contains("delete"))
                        mItem.Click += new RoutedEventHandler(deleteOneTag);
                } 
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

        private void configLoadedCallbackFkt(object sender, EventArgs e)
        {
            docQuery = new EntryGraphQuery();
            query = new VirtuosoSkosQuery();
        }
    }
}
