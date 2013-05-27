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
using System.Threading;
using Microsoft.SharePoint.Client;
using System.Windows.Browser;

namespace TabNavApp
{
    public partial class App : Application
    {
        public App()
        {
            this.Startup += this.Application_Startup;
            this.UnhandledException += this.Application_UnhandledException;

            InitializeComponent();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            ApplicationContext.Init(e.InitParams, SynchronizationContext.Current);
            //Get UniqueID array from input params
            string documentIds = null;
            string url = null;
            string listId = null;
            string docType = null;

            if (e.InitParams.ContainsKey("docs"))
                documentIds = e.InitParams["docs"];

            if (e.InitParams.ContainsKey("MS.SP.Url"))
                url = e.InitParams["MS.SP.Url"];

            if (e.InitParams.ContainsKey("listId"))
                listId = e.InitParams["listId"];

            if (e.InitParams.ContainsKey("docType"))
                docType = e.InitParams["docType"];

            //initialize the app with UniqueID array or null, in case there are no GUIDS available
            this.RootVisual = new MainPage(documentIds, docType, url, listId);
        }

        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            // If the app is running outside of the debugger then report the exception using
            // a ChildWindow graphControl.
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                // NOTE: This will allow the application to continue running after an exception has been thrown
                // but not handled. 
                // For production applications this error handling should be replaced with something that will 
                // report the error to the website and stop the application.
                e.Handled = true;
                ChildWindow errorWin = new ErrorWindow(e.ExceptionObject);
                errorWin.Show();
            }
        }
    }
}