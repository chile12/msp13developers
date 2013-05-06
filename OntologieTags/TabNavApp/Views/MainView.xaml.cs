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
using VirtuosoSkosSilverlight;
using DocGraphSilverlight;
using VDS.RDF;

namespace TabNavApp.Views
{
    public partial class MainView : Page
    {
        VirtuosoSkosQuery query = null;                                                 //our query object
        VDS.RDF.Query.SparqlRemoteEndpoint endpoint = null;                             //the endpoint object
        VirtuosoSkosQuery.SearchResultCallback searchTagCall = null;
        Document[] selectedDocs;

        public MainView(Document[] selectedDocs)
        {
            InitializeComponent();

            this.selectedDocs = selectedDocs;
            //endpoint and query object are initiated
            endpoint = new VDS.RDF.Query.SparqlRemoteEndpoint(UriFactory.Create(Constants.endpointAddress), Constants.graphUri);
            query = new VirtuosoSkosQuery(endpoint);
            searchTagCall = new VirtuosoSkosQuery.SearchResultCallback(searchResultCallback);
            
        }

        public MainView() : this(null) { }
      

        // Executes when the user navigates to this page.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            
        }

        private void tagSearchBT_Click(object sender, RoutedEventArgs e)
        {
            query.searchTag(searchTagCall, tagSearchTB.Text, 50);
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
                List<ReturnTag> zw = ConverterClass.convertListStringArrayToTagList(set);     //convert SPARQLResultSet to a DataTable object

                tagSearchLB.ItemsSource = zw;

            });
        }
    }
}
