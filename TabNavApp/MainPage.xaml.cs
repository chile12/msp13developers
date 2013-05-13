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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.SharePoint.Client;
using System.Threading;
using System.Text;
using TabNavApp.Api.Documents;
using System.Windows.Browser;

namespace TabNavApp
{
    public partial class MainPage : UserControl
    {
        public static bool DocumentsLoaded;
        public static Document[] LoadedDocuments;

        public static TabNavApp.Api.Common.ListController<Document> docSearchListSaver { get; set; }
        public static TabNavApp.Api.Common.ListController<TabNavApp.Api.Tags.Tag> tagSearchListSaver { get; set; }

        public string loadGraphConceptUri { get; set; }
        public string loadGraphConceptName { get; set; }

        public MainPage(string documentIdString, string siteUrl, string listId)
        {
            InitializeComponent();
            string[] documentIds = DocumentUtils.GetIDsFromString(documentIdString);
            AsyncDocumentLoader asnycDocLoader = new AsyncDocumentLoader(siteUrl, listId, documentIds);
            asnycDocLoader.WorkDone += new AsyncDocumentLoader.WorkDoneHandler(asnycDocLoader_WorkDone);
            asnycDocLoader.LoadDocuments();
            progressBar.IsIndeterminate = true;
        }

        void asnycDocLoader_WorkDone(object sender, WorkDoneEventArgs e)
        {
            LoadedDocuments = e.Documents;
            if (LoadedDocuments != null)
            {
                DocumentsLoaded = true;
            }
            progressBar.Visibility = Visibility.Collapsed;
            ContentFrame.Navigate(new Uri("/MainView", UriKind.Relative));
        }

        // After the Frame navigates, ensure the HyperlinkButton representing the current page is selected
        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            foreach (UIElement child in LinksStackPanel.Children)
            {
                HyperlinkButton hb = child as HyperlinkButton;
                if (hb != null && hb.NavigateUri != null)
                {
                    if (hb.NavigateUri.ToString().Equals(e.Uri.ToString()))
                    {
                        VisualStateManager.GoToState(hb, "ActiveLink", true);
                    }
                    else
                    {
                        VisualStateManager.GoToState(hb, "InactiveLink", true);
                    }
                }
            }

            if (ContentFrame.Content.GetType() == typeof(SearchGraph))
            {
                if (loadGraphConceptName != null && loadGraphConceptUri != null)
                    (ContentFrame.Content as SearchGraph).NavigateToGraphConcept(loadGraphConceptUri, loadGraphConceptName);
                loadGraphConceptName = null;
                loadGraphConceptUri = null;
            }
            //if (ContentFrame.Content.GetType() == typeof(TabNavApp.Views.MainView))
            //{
            //    if (tagSearchListSaver != null)
            //        (ContentFrame.Content as TabNavApp.Views.MainView).tagController = tagSearchListSaver;
            //    if (docSearchListSaver != null)
            //        (ContentFrame.Content as TabNavApp.Views.MainView).documentController = docSearchListSaver;
            //}
        }

        // If an error occurs during navigation, show an error window
        private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            Exception ex = e.Exception;

            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
            }

            e.Handled = true;
            ChildWindow errorWin = new ErrorWindow(ex);
            errorWin.Show();
        }
    }
}