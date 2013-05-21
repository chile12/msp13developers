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
    public class LoadDocumentEventArgs
    {
        public Document[] Documents { get; private set; }
        public List List { get; private set; }

        public LoadDocumentEventArgs(Document[] Documents, List List)
        {
            this.Documents = Documents;
            this.List = List;
        }
    }

    public class FindDocumentEventArgs
    {
        public List List { get; private set; }

        public FindDocumentEventArgs(List List)
        {
            this.List = List;
        }
    }

    public class AsyncDocumentLoader
    {
        public delegate void FindDocumentDoneHandler(object sender, FindDocumentEventArgs e);

        public delegate void LoadDocumentDoneHanlder(object sender, LoadDocumentEventArgs e);

        public event FindDocumentDoneHandler FindDocumentDone;

        public event LoadDocumentDoneHanlder LoadDocumentDone;

        private string siteUrl;

        private string listId;

        private string[] documentIds;

        protected void OnFindDocumentDone(object sender, FindDocumentEventArgs e)
        {
            if (FindDocumentDone != null)
            {
                FindDocumentDone(this, e);
            }
        }

        protected void OnLoadDocumentDone(object sender, LoadDocumentEventArgs e)
        {
            if (LoadDocumentDone != null)
            {
                LoadDocumentDone(this, e);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="siteUrl"></param>
        public AsyncDocumentLoader(string siteUrl)
        {
            this.siteUrl = siteUrl;
        }

        /// <summary>
        /// Verify whether a document is somewhere in the sharepoint farm and return the containing list
        /// </summary>
        /// <param name="documentId">the document id</param>
        public void FindDocument(string documentId)
        {
            this.documentIds = new string[] { documentId };
            ThreadPool.QueueUserWorkItem(FindDocumentThread);
        }

        /// <summary>
        /// Verify whether a document is in a specific list and return the list if so
        /// </summary>
        /// <param name="documentId">the document id</param>
        /// <param name="listId">the list id</param>
        public void FindDocument(string documentId, string listId)
        {
            this.documentIds = new string[] { documentId };
            this.listId = listId;
            ThreadPool.QueueUserWorkItem(FindDocumentInListThread);
        }

        /// <summary>
        /// Find and load a list of documents in a list
        /// </summary>
        /// <param name="documentIds">the document ids</param>
        /// <param name="listId">the list id</param>
        public void LoadDocuments(string[] documentIds, string listId)
        {
            this.documentIds = documentIds;
            this.listId = listId;
            ThreadPool.QueueUserWorkItem(LoadDocumentsThread);
        }

        private void FindDocumentThread(object state)
        {
            List list = null;
            try
            {
                using (ClientContext context = new ClientContext(siteUrl))
                {
                    ListCollection allLists = context.Web.Lists;
                    context.Load(allLists);
                    context.ExecuteQuery();

                    foreach (List currentList in allLists)
                    {
                        if (currentList.BaseType != BaseType.None && currentList.BaseType != BaseType.Unused)
                        {
                            context.Load(currentList);
                            context.ExecuteQuery();
                           
                            ListItemCollection col = currentList.GetItems(createDocumentCamlQuery());
                            context.Load(col);
                            context.ExecuteQuery();

                            if (col.Count > 0)
                            {
                                list = currentList;
                                break;
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
                OnFindDocumentDone(this, new FindDocumentEventArgs(list));
            });
        }

        private void FindDocumentInListThread(object state)
        {
            List list = null;
            try
            {
                using (ClientContext context = new ClientContext(siteUrl))
                {
                    List currentList = context.Web.Lists.GetById(new Guid(listId));
                    context.Load(currentList);
                    context.ExecuteQuery();
                    ListItemCollection col = currentList.GetItems(createDocumentCamlQuery());
                    context.Load(col);
                    context.ExecuteQuery();

                    if (col.Count > 0)
                    {
                        list = currentList;
                    }
                }
            }
            catch (Exception)
            {
                //error loading items
            }
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                OnFindDocumentDone(this, new FindDocumentEventArgs(list));
            });
        }

        private void LoadDocumentsThread(object state)
        {
            Document[] docs = null;
            List list = null;
            try
            {
                using (ClientContext context = new ClientContext(siteUrl))
                {
                    List currentList = context.Web.Lists.GetById(new Guid(listId));
                    context.Load(currentList);
                    context.ExecuteQuery();
                    list = currentList;
      
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
                OnLoadDocumentDone(this, new LoadDocumentEventArgs(docs, list));
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
