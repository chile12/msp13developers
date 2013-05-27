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
using TabNavApp.Api.Common;
using TabNavApp.Views;
using System.Text;
using VirtuosoQuery.Silverlight.Entry;
using System.Threading;
using System.Windows.Media.Imaging;

namespace TabNavApp.Api.Documents
{
    /// <summary>
    /// this User-Control is used as ListBoxItem in the DocumentSearchListBox
    /// </summary>
    public partial class DocumentListItem : UserControl
    {
        private MainView MainView;      //the MainView
        private Document Item;              //the DataBoundItem as DataSource for this
        /// <summary>
        /// ...
        /// </summary>
        public DocumentListItem()
        {
            InitializeComponent();
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(ListItem_DataContextChanged);
        }
        /// <summary>
        /// event: triggered right after creation
        /// </summary>
        /// <param name="sender">this</param>
        /// <param name="e"></param>
        void ListItem_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Initialize(sender, e);
                       
            //set the background-color if item is sticked
            if (Item.Background != null)
                this.LayoutRoot.Background = Item.Background;
            else
                this.LayoutRoot.Background = Constants.brush_default;
        }

        /// <summary>
        /// initializes Item and gets the parent of the parent-container
        /// </summary>
        /// <param name="sender">this</param>
        /// <param name="e"></param>
        private void Initialize(object sender, DependencyPropertyChangedEventArgs e)
        {
            getMainViewFromSender(sender);
            Item = this.DataContext as Document;            //DataContext is a Document
            Item.Control = this;

            Icon.Source = new BitmapImage(new Uri("../../Icons/" + Item.ListType.ToString() + ".png", UriKind.Relative));
        }
        /// <summary>
        /// since the parent of this control is not been set, the corresponding MainView is somewhere up the visual tree
        /// this methode seeks for the MainView object
        /// </summary>
        /// <param name="sender"></param>
        private void getMainViewFromSender(object sender)
        {
            DependencyObject mainView = sender as DependencyObject;
            while (mainView != null)
            {
                if (mainView is TabNavApp.Views.MainView)
                    break;

                mainView = VisualTreeHelper.GetParent(mainView);
            }
            this.MainView = mainView as MainView;
        }
        /// <summary>
        /// mouse event: catches double-clicks and toggles sticky-state
        /// </summary>
        /// <param name="sender">this</param>
        /// <param name="e"></param>
        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MainView.documentController.ToggleStickied((this.DataContext as Document));
                MainView.documentController.Update(true);
            }
        }
        /// <summary>
        /// mouse event: catches right-clicks -> calles the contextmenu-load methode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LayoutRoot_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            MainView.LoadDocumentListBoxContextMenu(this);
        }
        /// <summary>
        /// button event: redirects to the list where this document is listetd
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Document doc = Item as Document;
            AsyncDocumentLoader asyncLoader = new AsyncDocumentLoader(MainPage.SiteUrl);
            asyncLoader.FindDocumentDone += new AsyncDocumentLoader.FindDocumentDoneHandler(asyncLoader_WorkDone);
            asyncLoader.FindDocument(doc.UniqueID, doc.ListID);
        }
        /// <summary>
        /// jan
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void asyncLoader_WorkDone(object sender, FindDocumentEventArgs e)
        {
            if (e.List != null)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(MainPage.SiteUrl);
                sb.Append(e.List.DefaultViewUrl);
                MainView.CloseWindowNavigateTo(sb.ToString());
            }
            else
            {
                //Document was moved or deleted
                Document doc = Item as Document;
                AsyncDocumentLoader asyncLoader = new AsyncDocumentLoader(MainPage.SiteUrl);
                asyncLoader.FindDocumentDone += new AsyncDocumentLoader.FindDocumentDoneHandler(asyncLoader_FindDocumentDone);
                asyncLoader.FindDocument(doc.UniqueID);
            }
        }
        /// <summary>
        /// jan
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void asyncLoader_FindDocumentDone(object sender, FindDocumentEventArgs e)
        {
            EntryGraphQuery docQuery = new EntryGraphQuery();//(new VDS.RDF.Query.SparqlRemoteEndpoint(new Uri(Constants.endpointAddress)), Constants.skosGraphUri, Constants.docGraphUri);

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                while (VirtuosoQuery.StaticHelper.endpointUri == null)
                    Thread.Sleep(50);
                if (e.List != null)
                {
                    //docQuery.updateListIDByGuid((this.DataContext as Document).UniqueID, e.List.Id.ToString().Replace("{", "").Replace("}", ""));
                }
                else
                {
                    //docQuery.deleteEntryAndTagsByGuid((this.DataContext as Document).UniqueID);
                }
            });
        }       
    }
}
