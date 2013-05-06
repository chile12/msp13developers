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
using TabNavApp.Api;

namespace TabNavApp
{
    public partial class MainPage : UserControl
    {
        private string[] guids;
        private string listId;
        private string siteUrl;

        private Document[] initDocs;

        public MainPage(string[] guids, string siteUrl, string listId)
        {
            InitializeComponent();

            this.guids = guids;
            this.listId = listId;
            this.siteUrl = siteUrl;
            ThreadPool.QueueUserWorkItem(new WaitCallback(InitializeDocuments));
        }
        private void InitializeDocuments(Object stateInfo)
        {
            
            using (ClientContext context = new ClientContext(siteUrl))
            {
                List currentList = context.Web.Lists.GetById(new Guid(listId));
                context.Load(currentList);
                context.ExecuteQuery();

                if (currentList.BaseType == BaseType.DocumentLibrary)
                {
                    ListItemCollection col = currentList.GetItems(createCamlQuery());
                    context.Load(col);
                    context.ExecuteQuery();

                    //if resultset not empty, load file
                    if (col.Count > 0)
                    {
                        initDocs = new Document[col.Count];
                        int i = 0;
                        foreach (ListItem item in col)
                        {
                            File file = item.File;
                            context.Load(file, f => f.Title, f => f.Author, f => f.TimeCreated);
                            context.ExecuteQuery();

                            initDocs[i] = new Document() { Name = file.Title, CreationDate = file.TimeCreated.ToString(),
                                                           Author = file.Author.ToString(),
                                                           GUID = item["UniqueId"].ToString()
                            };
                            i++;
                        }
                    }
                }
            }
            //done


        }

        private CamlQuery createCamlQuery()
        {
            CamlQuery qry = new CamlQuery();
            StringBuilder sb = new StringBuilder();
            sb.Append("<View><Query><Where><In><FieldRef Name='UniqueId'/><Values>");
            foreach (string guid in guids)
                sb.Append("<Value Type='Lookup'>" + guid + "</Value>");
            sb.Append("</Values></In></Where></Query></View>");
            qry.ViewXml = sb.ToString();
            return qry;
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