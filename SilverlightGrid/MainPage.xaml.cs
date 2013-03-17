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
using System.Reflection;
using VirtuosoSkosSilverlight;
using VDS.RDF;
using VDS.RDF.Query;

namespace SilverlightGrid
{
    public partial class MainPage : UserControl
    {
        
        public MainPage()
        {
            InitializeComponent();
            SparqlResultsCallback callback = new SparqlResultsCallback(sparqlCallback);
        }

        /// <summary>
        /// receives result-call from triple-store and generates columns, bindings and headers
        /// </summary>
        /// <param name="set">the resultset</param>
        /// <param name="state">additional sender object (no relevance here)</param>
        private void sparqlCallback(SparqlResultSet set, object state)
        {
            List<ReturnRow> zw = null;
            //thread-crossing!
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                zw = ConverterClass.convertSparqlResultToListReturnRow(set);
                this.searchDG.AutoGenerateColumns = false;
                generateGridColumns(zw);
                this.searchDG.ItemsSource = zw;
            });
            
        }

        //needs to be passes as delegate if a search is carried out
        private void searchResultCallback(Tuple<string[], List<object[]>> set)
        {
            //thread-crossing!
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                List<ReturnRow> zw = ConverterClass.convertListStringArrayToDataTable(set);
                this.searchDG.AutoGenerateColumns = false;
                generateGridColumns(zw);
                this.searchDG.ItemsSource = zw;
            });
        }

        private void generateGridColumns(List<ReturnRow> zw)
        {
            foreach (PropertyInfo prop in zw[0].GetType().GetProperties())
            {
                if (prop.GetValue(zw[0], null) != null)
                {
                    DataGridTextColumn col = new DataGridTextColumn();
                    col.Header = prop.GetValue(zw[0], null).ToString();
                    col.Binding = new System.Windows.Data.Binding(prop.Name);

                    if (new string[]{"uri", "Relevance"}.Contains(col.Header.ToString()))
                        col.Visibility = System.Windows.Visibility.Collapsed;

                    this.searchDG.Columns.Add(col);
                }
            }
            zw.RemoveAt(0);
        }

        private void searchBT_Click(object sender, RoutedEventArgs e)
        {
            VirtuosoSkosQuery.SearchResultCallback searchCall = new VirtuosoSkosQuery.SearchResultCallback(searchResultCallback);
            VirtuosoSkosQuery query = new VirtuosoSkosQuery(new VDS.RDF.Query.SparqlRemoteEndpoint(UriFactory.Create("http://localhost:8890/sparql"), "http://localhost:8890/UNESCO"));
            query.searchTag(searchCall, searchTB.Text, 20);
        } 

    }
}
