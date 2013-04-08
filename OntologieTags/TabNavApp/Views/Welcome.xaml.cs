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

namespace TabNavApp
{
    public partial class Welcome : Page
    {
        public Welcome()
        {
            InitializeComponent();

            Constants.graphUri = "http://localhost:8890/UNESCO";
            Constants.endpointAddress = "http://localhost:8890/sparql";
        }

        // Executes when the user navigates to this page.
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void okBT_Click(object sender, RoutedEventArgs e)
        {
            if (skosGraphUriTB.Text != "")
                Constants.graphUri = skosGraphUriTB.Text;
            if (sparqlEndpointUriTB.Text != "")
                Constants.endpointAddress = sparqlEndpointUriTB.Text;
        }
    }
}