using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using VDS.RDF.Query;
using VDS.RDF.Update;
using VDS.RDF;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;
using System.Xml;

namespace VirtuosoQuery.Silverlight.Entry
{
    /// <summary>
    /// class deals with any kind of querrý pertaining to the entry/tag-graph 
    /// (document is replaced by entry, since not only documents can be tagged)
    /// </summary>
    public class EntryGraphQuery : TripleStoreQuery, IEntryGraphQuery
    {
        private AutoResetEvent stopWaitHandle = new AutoResetEvent(false);          //handles thread stepping
        private AutoResetEvent entryInputWaitHandle = new AutoResetEvent(false);          //handles thread stepping
        private SparqlResultsCallback callbackMaxDoc;                               //some delegates
        private SparqlResultsCallback callbackMaxTag;
        private SparqlResultsCallback callbackUpdatedDoc;
        private SparqlResultsCallback insertEntryOrTagCallback = null;
        private SparqlResultsCallback checkForExistingTag = null;
        public SparqlRemoteEndpoint endpoint;                                       //the sparql-endpoint object
        private string skos;                                                        //skos-core-uri
        private string ctag;                                                        //common-tag uri
        private string tagGraph = null;                                             //skos(tag) graph uri
        private string docGraph = null;                                             //document (entry)-graph    uri
        
        //query-components
        private string skosGraphLanguageTag;                                        //the language to be used in any skos-graph related query
        private string docGraphLanguageTag;                                          //the language to be used in any entry-graph related query
        private string queryPrefix;                                                 //some query-parts (to be reused)
        private string querySelect;
        private string queryFrom;
        private string queryWhere;
        private string queryOrder;
        private string[] stopWords;                                                 //stopword list

        //constants
        private const string STR_DocEntry = "Entry";
        private const string STR_TagEntry = "TagEntry";

        private double newEntryNumber = 0;
        private double newTagNumber = 0;

        public EntryGraphQuery()
        {  
            if (StaticHelper.endpointUri != null)       //check if configurations are done loading
            {
                this.endpoint = new SparqlRemoteEndpoint(new Uri(StaticHelper.endpointUri));       
                this.docGraph = StaticHelper.docGraphUri;                                           //get configurations
                this.tagGraph = StaticHelper.skosGraphUri;
                this.skos = StaticHelper.skos;
                this.ctag = StaticHelper.ctag;
                this.docGraphLanguageTag = StaticHelper.DocGraphLanguageTag;                        //get the given language from config-file
                this.skosGraphLanguageTag = StaticHelper.SkosGraphLanguageTag;
                this.stopWords = StaticHelper.searchStopWords.Replace(" ", "").Replace("\n", "").Split(',');        //stopword-list

                callbackMaxDoc = new SparqlResultsCallback(callbackMaxEntryFkt);                    //delegates
                callbackMaxTag = new SparqlResultsCallback(callbackMaxTagFkt);
                callbackUpdatedDoc = new SparqlResultsCallback(callbackEntry);
                insertEntryOrTagCallback = new SparqlResultsCallback(insertEntryOrTagCallbackFkt);
                checkForExistingTag = new SparqlResultsCallback(checkForExistingTagCallbackFkt);

                //common query-parts
                queryPrefix = "PREFIX skos: <" + skos + "> PREFIX : <" + docGraph + "/> PREFIX tags: <" + 
                    tagGraph + "/> PREFIX ctag: <" + ctag + "> ";       //set simple query components
                queryFrom = "FROM <" + docGraph + "> FROM <" + tagGraph + "> ";

                //get the max entry / tag number used in the entry/tag-graph
                getMaxEntryOfGraph(callbackMaxDoc, null);
                getMaxTagOfGraph(callbackMaxTag, null);
                initialized = true;
            }
        }
        /// <summary>
        /// query: gets all tags of an entry
        /// </summary>
        /// <param name="docGuid">the Guid belonging to the entry</param>
        /// <param name="callback">delegates represents the methode to call with a query-result</param>
        /// <param name="state">optional reach-through object</param>
        /// <param name="order">order direction of the results</param>
        public void getEntryTags(string docGuid, SparqlResultsCallback callback, object state, OrderDirection order)
        {
            querySelect = " SELECT DISTINCT ?skosUri (str(?label) as ?tagLabel) (sql:group_concat(?altLabel , \", \") AS ?altLabels) (STR(?description) as ?description) ";
            queryWhere = "WHERE { ?uri :spGuid ?guid. ?uri :tagged ?tag. ?tag :means ?skosUri. ?skosUri skos:prefLabel ?label. " +
                "OPTIONAL{?skosUri skos:altLabel ?altLabel FILTER langMatches( lang(?altLabel ), \"" + skosGraphLanguageTag + "\" )} " +
                "OPTIONAL{?skosUri skos:scopeNote ?description FILTER langMatches( lang(?description), \"" + skosGraphLanguageTag + "\" )} " +
                "FILTER ( ?guid = \"" + docGuid + "\") " + " FILTER langMatches( lang(?label), \"" + skosGraphLanguageTag + "\" )} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?tag)";
            sendQuery(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, state);
        }
        /// <summary>
        /// query: gets all documents (entris) which have at least one of the given tags
        /// </summary>
        /// <param name="tagUris">list of uris from skos-tags</param>
        /// <param name="callback">delegates represents the methode to call with a query-result</param>
        /// <param name="state">optional reach-through object</param>
        public void getEntriesByTags(List<string> tagUris, SparqlResultsCallback callback, object state)
        {
            querySelect = "SELECT ?doc ?author (STR(?title) as ?title) ?guid ?spListID ?spListType ?created ?server (count(?title) as ?hits) ";
            queryWhere = "WHERE {?doc :tagged ?tag. ?tag :means ?skos. OPTIONAL{?doc :author ?author.} " +
                "OPTIONAL{?doc :spListID ?spListID.} OPTIONAL{?doc :spListType ?spListType.} ?doc :created ?created. OPTIONAL{?doc :spServer ?server.} " +
                "?doc :name ?title. ?doc :spGuid ?guid.  FILTER langMatches( lang(?title), \"" + docGraphLanguageTag + "\" ) FILTER (?skos IN (";

            foreach (string tagUri in tagUris)
                queryWhere += "<" + tagUri + ">,";

            queryWhere = queryWhere.Remove(queryWhere.Count() - 1);
            queryWhere += "))} ";

            queryOrder = "ORDER BY DESC(?hits) ?title";

            sendQuery(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, state);
        }
        /// <summary>
        /// query: looks for documents(entries) only by there names (normal text search)
        /// </summary>
        /// <param name="searchString">search words</param>
        /// <param name="callback">delegates represents the methode to call with a query-result</param>
        /// <param name="state">optional reach-through object</param>
        public void getEntriesByName(string searchString, SparqlResultsCallback callback, object state)
        {
            string[] keyWords = searchString.Replace(" ", "").Replace("\n", "").Split(',');
            querySelect = "SELECT ?doc ?author (STR(?title) as ?title) ?guid ?spListID ?spListType ?created ?server (count(?title) as ?hits) ";
            queryWhere = "WHERE {OPTIONAL{?doc :author ?author.} OPTIONAL{?doc :spListID ?spListID.} OPTIONAL{?doc :spListType ?spListType.} " +
                "?doc :spGuid ?guid. ?doc :created ?created. OPTIONAL{?doc :spServer ?server.} {";
            foreach (string key in keyWords)        //exclude words in stopword-list
            {
                if (!stopWords.Contains(key))  //not!
                    queryWhere += "{?doc :name ?title. FILTER(regex(str(?title), \"" + key + "\", \"i\"))} UNION ";
            }
            queryWhere = queryWhere.Remove(queryWhere.Count() - 6); //remove the last 'UNION'
            queryWhere += "} FILTER langMatches( lang(?title), \"" + docGraphLanguageTag + "\" )} ";
            queryOrder = "ORDER BY DESC(?hits) ?title";
            sendQuery(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, state);
        }
        /// <summary>
        /// query: returns the graph-uri of a document(entry) which has the given Guid
        /// </summary>
        /// <param name="guid">the Guid</param>
        /// <param name="callback">delegates represents the methode to call with a query-result</param>
        /// <param name="state">optional reach-through object</param>
        public void getEntryUriByGuid(string guid, SparqlResultsCallback callback, object state)
        {
            querySelect = "SELECT ?uri ";
            queryWhere = "WHERE { ?uri a :spItem. ?uri :spGuid ?guid. FILTER ( ?guid = \"" + guid + "\") } ";

            sendQuery(queryPrefix + querySelect + queryFrom + queryWhere, callback, state);
        }
        /// <summary>
        /// query: gets all properties of an entry
        /// </summary>
        /// <param name="docUri">the graph-uri of the entry</param>
        /// <param name="callback">delegates represents the methode to call with a query-result</param>
        /// <param name="state">optional reach-through object</param>
        /// <param name="order">order direction of the results</param>
        public void getEntryProperties(string docUri, SparqlResultsCallback callback, object state, OrderDirection order)
        {
            querySelect = "(str(?name) as ?title) ?author ?creationDate ?type ?spServer";
            queryWhere = "WHERE { ?uri :author ?author. ?uri :name ?name. ?uri :spListType ?type. " +
                "?uri :ceated ?creationDate. ?uri :spServer ?spServer. FILTER ( ?uri = <" + docUri + 
                ">) FILTER langMatches( lang(?title), \"" + docGraphLanguageTag + "\" )} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?tag)";
            sendQuery(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, state);
        }
        /// <summary>
        /// inserts multiple enties, by checking if the entry already exists first (next step: delegate: methode - insertEntryOrTagCallbackFkt)
        /// </summary>
        /// <param name="entries">list of documents(enties)</param>
        public void insertEntries(List<ReturnDocument> entries)
        {
            new Thread(() =>
            {
                foreach (ReturnDocument entry in entries)
                {
                    getEntryUriByGuid(entry.UniqueID, insertEntryOrTagCallback, entry);
                    entryInputWaitHandle.WaitOne();
                }
            }).Start();
        }
        /// <summary>
        /// update query: insert a new entry in the document(entry)-graph
        /// </summary>
        /// <param name="guid">Guid of entry</param>
        /// <param name="spListID">ListId which holds entry in Sharepoint</param>
        /// <param name="name">name</param>
        /// <param name="created">date of creation in Sharepoint</param>
        /// <param name="entry">type of the entry (document, discussion...)</param>
        /// <param name="tags">optional: list of tag-uris</param>
        /// <param name="author">the creator (Sharepoint-username</param>
        /// <param name="spServer">the Sharepint server address</param>
        public void insertEntry(string guid, string spListID, string name, DateTime created, VirtuosoQuery.Entry entry, List<string> tags, string author = null, string spServer = "default")
        {
            string insertNumber = NewEntryNumber().ToString("0000000");       //get the next entry-number not used yet
            //the query
            string queryInsert = "INSERT INTO <" + docGraph + "> {" +
            ":Entry" + insertNumber + " a 	:spItem ; " +
            ":spGuid \"" + guid + "\" ;" +
            ":spListID \"" + spListID + "\" ;" +
            ":name 	\"" + name + "\"@" + docGraphLanguageTag + "; " +
            ":created \"" + XmlConvert.ToString(created, XmlDateTimeSerializationMode.Utc) + "\";" +
            ":spListType :" + entry.ToString() + " ;";

            if(spServer != null && spServer != "")
                queryInsert += ":spServer \"" + spServer + "\"; ";
                
            if(author != null && spServer != "")
                queryInsert += ":author \"" + author + "\"; ";

            queryInsert = queryInsert.Remove(queryInsert.LastIndexOf(';'));
            queryInsert +=  ". }";

            //insert new entry here
            sendQuery(queryPrefix + queryInsert, callbackUpdatedDoc, null);
            ////////
            stopWaitHandle.WaitOne();   //wait for callback
            //////

            if (tags != null)       //if there are tags
            {
                foreach (string tag in tags)    //place new tag-entries (between the added antry and the given skos-tags (by uri) next step: delegate methode: checkForExistingTag
                {
                    querySelect = "SELECT ?tag ";
                    queryWhere = "WHERE {?uri :tagged ?tag. ?tag :means ?concept." +
                        "FILTER ( ?uri = :Entry" + insertNumber + ") " +
                        "FILTER ( ?concept = <" + tag + ">) } ";
                    sendQuery(queryPrefix + querySelect + queryFrom + queryWhere, checkForExistingTag, new string[]{insertNumber, tag});
                } 
            }
        }
        /// <summary>
        /// update query: insert a single tag between a Item-entry (e.g. document) and a skos-tag (from the skos-graph) by Item-uri
        /// </summary>
        /// <param name="itemUri">the uri pertaining to the sharepoint-item</param>
        /// <param name="skosConceptUri">the tag uri</param>
        public void insertTagByEntryUri(string itemUri, string skosConceptUri)
        {
            double newTag = NewTagNumber(); //get the next not used TagEntry-number
            string queryInsert = "INSERT INTO <" + docGraph + "> {" +
                "<" + itemUri + "> :tagged :TagEntry" + newTag.ToString("000000000") +
                ". :TagEntry" + newTag.ToString("000000000") + " a :Tag; :means <" + skosConceptUri + ">;" +
                "ctag:taggingDate \"" + XmlConvert.ToString(DateTime.Now, XmlDateTimeSerializationMode.Utc) +
                "\"; ctag:Label \"added later on\".}";
            sendQuery(queryPrefix + queryInsert, callbackUpdatedDoc, null);
        }
        /// <summary>
        /// update query: insert a single tag between a Item-entry (e.g. document) and a skos-tag (from the skos-graph) by entry-number
        /// </summary>
        /// <param name="docNr">number of the entry</param>
        /// <param name="skosConceptUri">the tag uri</param>
        public void insertTagByEntryNr(string docNr, string skosConceptUri)
        {
            double newTag = NewTagNumber();
            string queryInsert = "INSERT INTO <" + docGraph + "> {" +
                ":Entry" + docNr + " :tagged :TagEntry" + newTag.ToString("000000000") +
                ". :TagEntry" + newTag.ToString("000000000") + " a :Tag; :means <" + skosConceptUri + ">;" +
                "ctag:taggingDate \"" + XmlConvert.ToString(DateTime.Now, XmlDateTimeSerializationMode.Utc) +
                "\"; ctag:Label \"on creation\".}";
            sendQuery(queryPrefix + queryInsert, callbackUpdatedDoc, null);
        }
        /// <summary>
        /// update query: deletes a tag (not a skos-tag!) by the uri of the pertaining entry (e.g. document)
        /// </summary>
        /// <param name="docUri">the entry-uri the tag pertains to</param>
        /// <param name="skosTagUri">the skos-tag-uir the tag points to</param>
        public void deleteTagByUri(string docUri, string skosTagUri) 
        {
            string queryWith = "WITH <" + docGraph + "> ";
            queryWhere = "DELETE {?tag ?p ?o. ?s ?p ?tag.}" +
                "WHERE {{{?tag ?p ?o.} UNION {?s ?p ?tag}}" +
                "?tag :means ?skos. ?doc :tagged ?tag." +
                "FILTER (?doc = <" + docUri + ">) FILTER (?skos = <" + skosTagUri + ">)}";
            sendQuery(queryPrefix + queryWith + queryWhere, callbackUpdatedDoc, null);
        }
        /// <summary>
        /// update query: deletes a tag (not a skos-tag!) by the Guid of the pertaining entry (e.g. document)
        /// </summary>
        /// <param name="docUri">the entry-Guid the tag pertains to</param>
        /// <param name="skosTagUri">the skos-tag-uir the tag points to</param>
        public void deleteTagByGuid(string docGuid, string skosTagUri)
        {
            string queryWith = "WITH <" + docGraph + "> ";
            queryWhere = "DELETE {?tag ?p ?o. ?s ?p ?tag.}" +
                "WHERE {{{?tag ?p ?o.} UNION {?s ?p ?tag}}" +
                "?tag :means ?skos. ?doc :tagged ?tag. ?doc :spGuid ?guid." +
                "FILTER (?guid = \"" + docGuid + "\") FILTER (?skos = <" + skosTagUri + ">)}";
            sendQuery(queryPrefix + queryWith + queryWhere, callbackUpdatedDoc, null);
        }
        /// <summary>
        /// update query: deletes an entry and all pertaining tags by its uri
        /// </summary>
        /// <param name="entryUri">the uri of the entry</param>
        public void deleteEntryAndTagsByUri(string entryUri)
        {
            string queryWith = "WITH <" + docGraph + "> ";
            queryWhere = "DELETE {?doc ?p ?o. ?tag ?p2 ?o2.} " +
                "WHERE {{{?doc ?p ?o.} UNION {?tag ?p2 ?o2}} " +
                "OPTIONAL{?doc :tagged ?tag.} ?doc a :spItem. " +
                "FILTER (?doc = <" + entryUri + ">)}";
            sendQuery(queryPrefix + queryWith + queryWhere, callbackUpdatedDoc, null);
        }
        /// <summary>
        /// update query: deletes an entry and all pertaining tags by its Guid
        /// </summary>
        /// <param name="entryUri">the Guid of the entry</param>
        public void deleteEntryAndTagsByGuid(string guid)
        {
            string queryWith = "WITH <" + docGraph + "> ";
            queryWhere = "DELETE {?doc ?p ?o. ?tag ?p2 ?o2.} " +
                "WHERE {{{?doc ?p ?o.} UNION {?tag ?p2 ?o2}} " +
                "OPTIONAL{?doc :tagged ?tag.} ?doc a :spItem. " +
                "?doc :spGuid ?guid. FILTER (?guid = \"" + guid + "\")}";
            sendQuery(queryPrefix + queryWith + queryWhere, callbackUpdatedDoc, null);
        }
        /// <summary>
        /// deprecated
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="newListId"></param>
        public void updateListIDByGuid(string guid, string newListId)
        {
            string queryWith = "WITH <" + docGraph + "> ";
            queryWhere = "DELETE {?doc ?list ?id}" +
                "WHERE {?doc ?list ?id. ?doc :spListID ?id. ?doc :spGuid ?guid." +
                "FILTER (?guid = \"" + guid + "\") }";
            sendQuery(queryPrefix + queryWith + queryWhere, callbackUpdatedDoc, null);
            ////////
            stopWaitHandle.WaitOne();   //wait for callback
            //////
            string queryInsert = "INSERT INTO <" + docGraph + "> {" +
                "?doc :spListID \"" + newListId + "\".}";
            queryWhere = "WHERE { ?doc :spGuid ?guid. FILTER (?guid = \"" + guid + "\")}";
            sendQuery(queryPrefix + queryInsert + queryWhere, callbackUpdatedDoc, null);
        }
        /// <summary>
        /// a intermediate step to send queries, used for debugging purposes
        /// </summary>
        /// <param name="query"></param>
        /// <param name="callbackDelegate"></param>
        /// <param name="state"></param>
        private void sendQuery(string query, SparqlResultsCallback callbackDelegate, object state)
        {
            endpoint.QueryWithResultSet(query, callbackDelegate, state); 
            //stopWaitHandle.Set();   //signal thread to continue
        }
        /// <summary>
        /// query: gets the max. entry-number of the entry-graph
        /// </summary>
        /// <param name="callback">delegates represents the methode to call with a query-result</param>
        /// <param name="state">optional reach-through object</param>
        private void getMaxEntryOfGraph(SparqlResultsCallback callback, object state)
        {
            querySelect = "SELECT DISTINCT (MAX(?uri)) ";
            queryWhere = "WHERE { ?uri a :spItem. } ";
            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere, callback, state);
        }
        /// <summary>
        /// callback methode: answer to getMaxEntryOfGraph, sets the newEntryNumber
        /// </summary>
        /// <param name="set">teh result</param>
        /// <param name="obj">not used</param>
        void callbackMaxEntryFkt(SparqlResultSet set, object obj)
        {
            //0 hardcoded since a scalar-value is requested
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    string zw = (set.Results[0].Value(set.Results[0].Variables.ElementAt(0).ToString()) as UriNode).Uri.AbsoluteUri;
                    newEntryNumber = double.Parse(zw.Substring(zw.IndexOf(STR_DocEntry) + 8));
                }
                catch (NullReferenceException)
                {
                }
                stopWaitHandle.Set();   //signal thread to continue
            });
        }
        /// <summary>
        /// query: gets the max. tag-entry-number of the entry-graph
        /// </summary>
        /// <param name="callback">delegates represents the methode to call with a query-result</param>
        /// <param name="state">optional reach-through object</param>
        private void getMaxTagOfGraph(SparqlResultsCallback callback, object state)
        {
            querySelect = "SELECT DISTINCT (MAX(?uri)) ";
            queryWhere = "WHERE { ?uri a :Tag. } ";
            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere, callback, state);
        }
        /// <summary>
        /// callback methode: answer to getMaxTagOfGraph, sets the newTagNumber
        /// </summary>
        /// <param name="set">the result</param>
        /// <param name="obj">not used</param>
        void callbackMaxTagFkt(SparqlResultSet set, object obj)
        {
            //thread crossing
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                //0 hardcoded since a scalar-value is requested
                try
                {
                    string zw = (set.Results[0].Value(set.Results[0].Variables.ElementAt(0).ToString()) as UriNode).Uri.AbsoluteUri;
                    newTagNumber = double.Parse(zw.Substring(zw.IndexOf(STR_TagEntry) + 8));
                }
                catch (NullReferenceException)
                {
                }
                stopWaitHandle.Set();   //signal thread to continue
            });
        }
        /// <summary>
        /// a callback methode which sets the calling thread back in ation
        /// </summary>
        /// <param name="set">not used</param>
        /// <param name="state">not used</param>
        void callbackEntry(SparqlResultSet set, object state)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                stopWaitHandle.Set();
            });
        }
        /// <summary>
        /// returns the next entry-number to be used
        /// </summary>
        /// <returns>entry-number</returns>
        private double NewEntryNumber()
        {
            return ++newEntryNumber;
        }
        /// <summary>
        /// returns the next tag-number to be used
        /// </summary>
        /// <returns>tag-number</returns>
        private double NewTagNumber()
        {
            return ++newTagNumber;
        }
        /// <summary>
        /// callback methode: checks if tag already exists, if not: insert a new tag
        /// </summary>
        /// <param name="set">the result</param>
        /// <param name="state">not used</param>
        void checkForExistingTagCallbackFkt(SparqlResultSet set, object state)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (set.Results.Count == 0)
                {
                    if (Char.IsDigit((state as string[])[0][0]))
                        insertTagByEntryNr((state as string[])[0], (state as string[])[1]);
                    else
                        insertTagByEntryUri((state as string[])[0], (state as string[])[1]);
                }
            });
        }
        /// <summary>
        /// callback methode: checks if an entry already exists, if not: insert new entry with tags, else insert tags only
        /// </summary>
        /// <param name="set">not used</param>
        /// <param name="state">not used</param>
        void insertEntryOrTagCallbackFkt(SparqlResultSet set, object state)
        {
            ReturnDocument item = (state as ReturnDocument);
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (set.Results.Count == 0)     
                {
                    //insert new entry with tags (open in new thread or UI-thread gets stuck by more than one new entry
                    Deployment.Current.Dispatcher.BeginInvoke(() => { insertEntry(item.UniqueID, item.ListID, item.Name, DateTime.Parse(item.CreationDate), item.ListType, item.Tags, item.Author, item.server); });
                }
                else if (set.Results.Count == 1)    //
                {
                    foreach (string tag in item.Tags)   //insert new tags (by calling: checkForExistingTag)
                    {
                        querySelect = "SELECT ?tag ";
                        queryWhere = "WHERE {?uri :tagged ?tag. ?tag :means ?concept." +
                            "FILTER ( ?uri = <" + (set.Results[0][0] as UriNode).Uri.AbsoluteUri + ">) " +
                            "FILTER ( ?concept = <" + tag + ">) } ";
                        sendQuery(queryPrefix + querySelect + queryFrom + queryWhere, checkForExistingTag, new string[]{(set.Results[0][0] as UriNode).Uri.AbsoluteUri, tag});
                    }
                }
                else
                    throw new Exception("The document-graph used is faulty. A duplicate document-guid was detected: \n" + item.UniqueID + "\nPlease contact an administrator about this issue.");

                entryInputWaitHandle.Set();
            });
        }
    }
}
