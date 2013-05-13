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

namespace VirtuosoQuery.Silverlight.Docs
{
    public enum OrderDirection
    {
        ASC, DESC, NONE
    }
    
    public class DocGraphQuery
    {
        private AutoResetEvent stopWaitHandle = new AutoResetEvent(false);   
        private SparqlResultsCallback callbackMaxDoc;
        private SparqlResultsCallback callbackMaxTag;
        private SparqlResultsCallback callbackUpdatedDoc;
        private SparqlResultsCallback insertEntryOrTagCallback = null;
        private SparqlResultsCallback checkForExistingTag = null;
        public SparqlRemoteEndpoint endpoint;
        private string xsd = "http://www.w3.org/2001/XMLSchema#";                       //some standard schemata
        private string skos = "http://www.w3.org/2004/02/skos/core#";
        private string tagGraph = null;
        private string docGraph = null;                          //Graphuri des Ontologiegraphen (http://{Triplestore-IP}:{Port}/{Graphname})
        //query-components
        private string languageTag;
        private string queryPrefix;
        private string querySelect;
        private string queryFrom;
        private string queryWhere;
        private string queryOrder;
        private string queryLanguageFilter;

        private const string STR_DocEntry = "DocEntry";
        private const string STR_TagEntry = "TagEntry";

        private double newConceptNumber = 0;
        private double newTagNumber = 0;

        public DocGraphQuery(SparqlRemoteEndpoint endpoint, string skosGraphUri, string docGraphUri)
        {
            callbackMaxDoc = new SparqlResultsCallback(callbackMaxDocFkt);
            callbackMaxTag = new SparqlResultsCallback(callbackMaxTagFkt);
            callbackUpdatedDoc = new SparqlResultsCallback(callbackDoc);
            insertEntryOrTagCallback = new SparqlResultsCallback(insertEntryOrTagCallbackFkt);
            checkForExistingTag = new SparqlResultsCallback(checkForExistingTagCallbackFkt);
            this.docGraph = docGraphUri;
            this.tagGraph = skosGraphUri;
            this.endpoint = endpoint;
            ///// TODO!
            this.languageTag = "en";//VirtuosoSkosSilverlight_dll.skosLanguage;        //get the given language from config-file
            ////
            queryPrefix = "PREFIX skos: <" + skos + "> PREFIX : <" + docGraph + "/> PREFIX tags: <" + tagGraph + "/> PREFIX ctag: <http://commontag.org/ns#> ";       //set simple query components
            queryFrom = "FROM <" + docGraph + "> FROM <" + tagGraph + "> ";
            queryLanguageFilter = "FILTER langMatches( lang(?name), \"" + languageTag + "\" ) ";

            getMaxDocEntryOfGraph(callbackMaxDoc, null);
            getMaxTagOfGraph(callbackMaxTag, null);
        }

        public void getDocTags(string docGuid, SparqlResultsCallback callback, object state, OrderDirection order)
        {
            querySelect = " SELECT DISTINCT ?skosUri (str(?label) as ?tagLabel) (sql:group_concat(?altLabel , \", \") AS ?altLabels) (STR(?description) as ?description) ";
            queryWhere = "WHERE { ?uri :spGuid ?guid. ?uri :tagged ?tag. ?tag :means ?skosUri. ?skosUri skos:prefLabel ?label. " +
                "OPTIONAL{?skosUri skos:altLabel ?altLabel FILTER langMatches( lang(?altLabel ), \"" + languageTag + "\" )} " +
                "OPTIONAL{?skosUri skos:scopeNote ?description FILTER langMatches( lang(?description), \"" + languageTag + "\" )} " +  
                "FILTER ( ?guid = \"" + docGuid + "\") " + " FILTER langMatches( lang(?label), \"" + languageTag + "\" )} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?tag)";
            sendQuery(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, state);
        }

        public void getDocsByTags(List<string> tagUris, SparqlResultsCallback callback, object state, OrderDirection order)
        {
            querySelect = "SELECT ?doc ?author (STR(?title) as ?title) ?guid ?spListID ?spListType ?created ?server (count(?title) as ?hits) ";
            queryWhere = "WHERE {?doc :tagged ?tag. ?tag :means ?skos. OPTIONAL{?doc :author ?author.} " +
                "OPTIONAL{?doc :spListID ?spListID.} OPTIONAL{?doc :spListType ?spListType.} ?doc :created ?created. OPTIONAL{?doc :spServer ?server.} " +
                "?doc :name ?title. ?doc :spGuid ?guid.  FILTER langMatches( lang(?title), \"" + languageTag + "\" ) FILTER (?skos IN (";

            foreach (string tagUri in tagUris)
                queryWhere += "<" + tagUri + ">,";

            queryWhere = queryWhere.Remove(queryWhere.Count() - 1);
            queryWhere += "))} ";

            queryOrder = "ORDER BY DESC(?hits) ?title";

            sendQuery(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, state);
        }

        public void getDocByGuid(string guid, SparqlResultsCallback callback, object state)
        {
            querySelect = "SELECT ?uri ";
            queryWhere = "WHERE { ?uri a :Doc. ?uri :spGuid ?guid. FILTER ( ?guid = \"" + guid + "\") } ";

            sendQuery(queryPrefix + querySelect + queryFrom + queryWhere, callback, state);
        }

        public void getDocProperties(string docUri, SparqlResultsCallback callback, object state, OrderDirection order)
        {
            querySelect = "(str(?name) as ?title) ?author ?creationDate ?type ?spServer";
            queryWhere = "WHERE { ?uri :author ?author. ?uri :name ?name. ?uri :spListType ?type. " +
                "?uri :ceated ?creationDate. ?uri :spServer ?spServer. FILTER ( ?uri = <" + docUri + ">) " +
                queryLanguageFilter + "} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?tag)";
            sendQuery(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, state);
        }

        public void insertEntries(List<ReturnDocument> entries)
        {
            foreach (ReturnDocument entry in entries)
            {
                getDocByGuid(entry.UniqueID, insertEntryOrTagCallback, entry);
            }
        }

        public void insertEntry(string guid, string spListID, string name, DateTime created, Entry entry, List<string> tags, string author = null, string spServer = "default")
        {
            new Thread(() =>
            {
                string insertNumber = NewConceptNumber().ToString("0000000");

                string queryInsert = "INSERT INTO <" + docGraph + "> {" +
                ":DocEntry" + insertNumber + " a 	:Doc ; " +
                ":spGuid \"" + guid + "\" ;" +
                ":spListID \"" + spListID + "\" ;" +
                ":name 	\"" + name + "\"@" + languageTag + "; " +
                ":created \"" + XmlConvert.ToString(created, XmlDateTimeSerializationMode.Utc) + "\";" +
                ":spListType :" + entry.ToString() + " ;";

                if(spServer != null && spServer != "")
                    queryInsert += ":spServer \"" + spServer + "\"; ";
                
                if(author != null && spServer != "")
                    queryInsert += ":author \"" + author + "\"; ";

                queryInsert = queryInsert.Remove(queryInsert.LastIndexOf(';'));
                queryInsert +=  ". }";

                sendQuery(queryPrefix + queryInsert, callbackUpdatedDoc, null);
                ////////
                stopWaitHandle.WaitOne();   //wait for callback
                //////

                if (tags != null)
                {
                    foreach (string tag in tags)
                    {
                        querySelect = "SELECT ?tag ";
                        queryWhere = "WHERE {?uri :tagged ?tag. ?tag :means ?concept." +
                            "FILTER ( ?uri = :DocEntry" + insertNumber + ") " +
                            "FILTER ( ?concept = <" + tag + ">) } ";
                        sendQuery(queryPrefix + querySelect + queryFrom + queryWhere, checkForExistingTag, new string[]{insertNumber, tag});
                    } 
                }

            }).Start();
        }

        public void insertTagByDocUri(string doc, string skosConcept)
        {
            double newTag = NewTagNumber();
            string queryInsert = "INSERT INTO <" + docGraph + "> {" +
                "<" + doc + "> :tagged :TagEntry" + newTag.ToString("000000000") +
                ". :TagEntry" + newTag.ToString("000000000") + " a :Tag; :means <" + skosConcept + ">;" +
                "ctag:taggingDate \"" + XmlConvert.ToString(DateTime.Now, XmlDateTimeSerializationMode.Utc) +
                "\"; ctag:Label \"added later on\".}";
            sendQuery(queryPrefix + queryInsert, callbackUpdatedDoc, null);
        }

        public void insertTagByDocNr(string docNr, string skosConcept)
        {
            double newTag = NewTagNumber();
            string queryInsert = "INSERT INTO <" + docGraph + "> {" +
                ":DocEntry" + docNr + " :tagged :TagEntry" + newTag.ToString("000000000") +
                ". :TagEntry" + newTag.ToString("000000000") + " a :Tag; :means <" + skosConcept + ">;" +
                "ctag:taggingDate \"" + XmlConvert.ToString(DateTime.Now, XmlDateTimeSerializationMode.Utc) +
                "\"; ctag:Label \"on creation\".}";
            sendQuery(queryPrefix + queryInsert, callbackUpdatedDoc, null);
        }

        public void deleteTagByUri(string docUri, string skosTagUri) 
        {
            string queryWith = "WITH <" + docGraph + "> ";
            queryWhere = "DELETE {?tag ?p ?o. ?s ?p ?tag.}" +
                "WHERE {{{?tag ?p ?o.} UNION {?s ?p ?tag}}" +
                "?tag :means ?skos. ?doc :tagged ?tag." +
                "FILTER (?doc = <" + docUri + ">) FILTER (?skos = <" + skosTagUri + ">)}";
            sendQuery(queryPrefix + queryWith + queryWhere, callbackUpdatedDoc, null);
        }

        public void deleteTagByGuid(string docGuid, string skosTagUri)
        {
            string queryWith = "WITH <" + docGraph + "> ";
            queryWhere = "DELETE {?tag ?p ?o. ?s ?p ?tag.}" +
                "WHERE {{{?tag ?p ?o.} UNION {?s ?p ?tag}}" +
                "?tag :means ?skos. ?doc :tagged ?tag. ?doc :spGuid ?guid." +
                "FILTER (?guid = \"" + docGuid + "\") FILTER (?skos = <" + skosTagUri + ">)}";
            sendQuery(queryPrefix + queryWith + queryWhere, callbackUpdatedDoc, null);
        }

        public void deleteDocAndTags(string doc)
        {
            string queryWith = "WITH <" + docGraph + "> ";
            queryWhere = "DELETE {?doc ?p ?o. ?tag ?p2 ?o2.}" +
                "WHERE {{{?doc ?p ?o.} UNION {?tag ?p2 ?o2}}" +
                "?doc :tagged ?tag." +
                "FILTER (?doc = <" + doc + ">)}";
            sendQuery(queryPrefix + queryWith + queryWhere, callbackUpdatedDoc, null);
        }

        private void sendQuery(string query, SparqlResultsCallback callbackDelegate, object state)
        {
            endpoint.QueryWithResultSet(query, callbackDelegate, state); 
            //stopWaitHandle.Set();   //signal thread to continue
        }

        public void getMaxDocEntryOfGraph(SparqlResultsCallback callback, object state)
        {
            querySelect = "SELECT DISTINCT (MAX(?uri)) ";
            queryWhere = "WHERE { ?uri a :Doc. } ";
            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere, callback, state);
        }

        void callbackMaxDocFkt(SparqlResultSet set, object obj)
        {
            //0 hardcoded since a scalar-value is requested
            try
            {
                string zw = (set.Results[0].Value(set.Results[0].Variables.ElementAt(0).ToString()) as UriNode).Uri.AbsoluteUri;
                newConceptNumber = double.Parse(zw.Substring(zw.IndexOf(STR_DocEntry) + 8));
            }
            catch (NullReferenceException)
            {
            }
            stopWaitHandle.Set();   //signal thread to continue
        }

        public void getMaxTagOfGraph(SparqlResultsCallback callback, object state)
        {
            querySelect = "SELECT DISTINCT (MAX(?uri)) ";
            queryWhere = "WHERE { ?uri a :Tag. } ";
            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere, callback, state);
        }

        void callbackMaxTagFkt(SparqlResultSet set, object obj)
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
        }

        void callbackDoc(SparqlResultSet set, object state)
        {
            //stump for later use
            stopWaitHandle.Set();
        }

        private double NewConceptNumber()
        {
            return ++newConceptNumber;
        }

        private double NewTagNumber()
        {
            return ++newTagNumber;
        }

        void checkForExistingTagCallbackFkt(SparqlResultSet set, object state)
        {
            if (set.Results.Count == 0)
            {
                if (Char.IsDigit((state as string[])[0][0]))
                    insertTagByDocNr((state as string[])[0], (state as string[])[1]);
                else
                    insertTagByDocUri((state as string[])[0], (state as string[])[1]);
            }
        }

        void insertEntryOrTagCallbackFkt(SparqlResultSet set, object state)
        {
            ReturnDocument item = (state as ReturnDocument);
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (set.Results.Count == 0)
                {
                    insertEntry(item.UniqueID, item.ListID, item.Name, DateTime.Parse(item.CreationDate), item.ListType, item.Tags, item.Author, item.server);
                }
                else if (set.Results.Count == 1)
                {
                    foreach (string tag in item.Tags)
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
            });
        }
    }
}
