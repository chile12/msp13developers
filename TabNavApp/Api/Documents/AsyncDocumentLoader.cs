using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.SharePoint.Client;

namespace TabNavApp.Api.Documents
{
    public class WorkDoneEventArgs
    {
        public Document[] Documents { get; private set; }

        public WorkDoneEventArgs(Document[] documents)
        {
            this.Documents = documents;
        }
    }

    public class AsyncDocumentLoader
    {
        public delegate void WorkDoneHandler(object sender, WorkDoneEventArgs e);

        public event WorkDoneHandler WorkDone;

        private string siteUrl;

        private string listId;

        private string[] documentIds;

        private Document[] docs;

        protected void OnWorkDone(object sender, WorkDoneEventArgs e)
        {
            if (WorkDone != null)
            {
                WorkDone(this, e);
            }
        }

        public AsyncDocumentLoader(string siteUrl, string listId, string[] documentIds)
        {
            this.siteUrl = siteUrl;
            this.listId = listId;
            this.documentIds = documentIds;
        }

        public void LoadDocuments()
        {
            ThreadPool.QueueUserWorkItem(LoadDocumentsThread);
        }


        private void LoadDocumentsThread(object state)
        {
            docs = null;
            try
            {
                using (ClientContext context = new ClientContext(siteUrl))
                {
                    List currentList = context.Web.Lists.GetById(new Guid(listId));
                    context.Load(currentList);
                    context.ExecuteQuery();

                    if (currentList.BaseType == BaseType.DocumentLibrary)
                    {
                        ListItemCollection col = currentList.GetItems(createDocumentCamlQuery());
                        context.Load(col);
                        context.ExecuteQuery();

                        //if resultset not empty, load file
                        if (col.Count > 0)
                        {
                            docs = new Document[col.Count];
                            int i = 0;
                            foreach (ListItem item in col)
                            {
                                File file = item.File;
                                context.Load(file, f => f.Name, f => f.Author, f => f.TimeCreated);
                                User author = file.Author;
                                context.Load(author, a => a.LoginName);
                                context.ExecuteQuery();
                                
                                docs[i] = new Document()
                                {
                                    Name = file.Name,
                                    CreationDate = file.TimeCreated.ToString(),
                                    Author = file.Author.LoginName,
                                    UniqueID = item["UniqueId"].ToString(),
                                    ListID = listId
                                };
                                i++;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                //error loading items
            }
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                OnWorkDone(this, new WorkDoneEventArgs(docs));
            });
        }


        private CamlQuery createDocumentCamlQuery()
        {
            CamlQuery qry = new CamlQuery();
            StringBuilder sb = new StringBuilder();
            sb.Append("<View><Query><Where><In><FieldRef Name='UniqueId'/><Values>");
            foreach (string documentId in documentIds)
                sb.Append("<Value Type='Lookup'>" + documentId + "</Value>");
            sb.Append("</Values></In></Where></Query></View>");
            qry.ViewXml = sb.ToString();
            return qry;
        }
    }




}
