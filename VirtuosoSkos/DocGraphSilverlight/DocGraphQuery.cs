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

namespace DocGraphSilverlight
{
    public enum Entry{  page, document }
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
        public SparqlRemoteEndpoint endpoint;
        private SparqlRemoteUpdateEndpoint upend;
        private string xsd = "http://www.w3.org/2001/XMLSchema#";                       //some standard schemata
        private string skos = "http://www.w3.org/2004/02/skos/core#";
        private string tagGraph = "http://localhost:8890/UNESCO";
        private string docGraph = "http://sharepoint.my/SPResourceTags";                          //Graphuri des Ontologiegraphen (http://{Triplestore-IP}:{Port}/{Graphname})
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

        private string lastGraphDoc = "";           //das neue oder zu bearbeitende Dokument (DocEntry-Nummer)
        private string lastGraphTag = "";            // -----"-----                   Tag

        public DocGraphQuery(SparqlRemoteUpdateEndpoint upendpoint, SparqlRemoteEndpoint endpoint)
        {
            callbackMaxDoc = new SparqlResultsCallback(callbackMaxDocFkt);
            callbackMaxTag = new SparqlResultsCallback(callbackMaxTagFkt);
            callbackUpdatedDoc = new SparqlResultsCallback(callbackDoc);
            this.endpoint = endpoint;
            this.upend = upendpoint;
            ///// TODO!
            this.languageTag = "en";//VirtuosoSkosSilverlight_dll.skosLanguage;        //get the given language from config-file
            ////
            queryPrefix = "PREFIX skos: <" + skos + "> PREFIX : <" + docGraph + "/> PREFIX tags: <" + tagGraph + "/> PREFIX ctag: <http://commontag.org/ns#> ";       //set simple query components
            queryFrom = "FROM <" + docGraph + "> FROM <" + tagGraph + "> ";
            queryLanguageFilter = "FILTER langMatches( lang(?name), \"" + languageTag + "\" ) ";
        }

        public void getDocTags(string docUri, SparqlResultsCallback callback, object state, OrderDirection order)
        {                
            new Thread(() =>
            {
                if (docUri == null)        //nimm letzten Eintrag
                {

                    getMaxDocEntryOfGraph(callbackMaxDoc, null);
                    ////////
                    stopWaitHandle.WaitOne();   //wait for callback
                    //////
                    docUri = lastGraphDoc;
                
                }
                querySelect = " SELECT DISTINCT (str(?name) as ?title) ?author (str(?label) as ?tag) ";
                queryWhere = "WHERE { ?uri :author ?author. ?uri :name ?name. ?uri :tagged ?tag." +
                    "?tag :means ?skos. ?skos skos:prefLabel ?label. FILTER ( ?uri = <" + docUri + ">) " +
                    queryLanguageFilter + " FILTER langMatches( lang(?label), \"" + languageTag + "\" )} ";
                queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?tag)";
                sendQuery(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, state);
                }).Start();
        }

        public void getDocProperties(string docUri, SparqlResultsCallback callback, object state, OrderDirection order)
        {
            querySelect = "(str(?name) as ?title) ?author ?createdBy ?creationDate ?spServer";
            queryWhere = "WHERE { ?uri :author ?author. ?uri :name ?name. ?uri :createdBy ?createdBy. " +
                "?uri :ceated ?creationDate. ?uri :spServer ?spServer. FILTER ( ?uri = <" + docUri + ">) " +
                queryLanguageFilter + "} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?tag)";
            sendQuery(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, state);
        }

        public void insertEntry(string name, Entry entry, List<string> tags, string author = null, string createdBy = null, string spServer = "default" )
        {
            double newConceptNumber = 0;
            new Thread(() =>
            {
                getMaxDocEntryOfGraph(callbackMaxDoc, null);
                ////////
                stopWaitHandle.WaitOne();   //wait for callback
                //////
                newConceptNumber = double.Parse(lastGraphDoc.Substring(lastGraphDoc.IndexOf(STR_DocEntry) + 8)) +1;
                string insertNumber = newConceptNumber.ToString("0000000");

                string queryInsert = "INSERT INTO <" + docGraph + "> {" +
                ":DocEntry" + insertNumber + " a 	:Doc ; " +
                ":name 	\"" + name + "\"@" + languageTag + "; " +
                ":created \"" + XmlConvert.ToString(DateTime.Now, XmlDateTimeSerializationMode.Utc) + "\";" +
                ":spServer \"" + spServer + "\"; ";
                
                if(author != null)
                    queryInsert += ":author \"" + author + "\"; ";
                if(createdBy != null)
                    queryInsert += ":createdBy \"" + createdBy + "\";";

                queryInsert = queryInsert.Remove(queryInsert.LastIndexOf(';'));
                queryInsert +=  ". }";

                sendQuery(queryPrefix + queryInsert, callbackUpdatedDoc, null);
                ////////
                stopWaitHandle.WaitOne();   //wait for callback
                //////

                getMaxTagOfGraph(callbackMaxTag, null);
                ////////
                stopWaitHandle.WaitOne();   //wait for callback
                ////////
                double newTag = double.Parse(lastGraphTag.Substring(lastGraphTag.IndexOf(STR_TagEntry) + 8)) + 1;
                foreach (string tag in tags)
                {
                    queryInsert = "INSERT INTO <" + docGraph + "> {" +
                        ":DocEntry" + insertNumber + " :tagged :TagEntry" + newTag.ToString("000000000") +
                        ". :TagEntry" + newTag.ToString("000000000") + " a :Tag; :means <" + tag + ">;" +
                        "ctag:taggingDate \"" + XmlConvert.ToString(DateTime.Now, XmlDateTimeSerializationMode.Utc) +
                        "\"; ctag:Label \"on creation\".}";
                    sendQuery(queryPrefix + queryInsert, callbackUpdatedDoc, null);
                    ////////
                    stopWaitHandle.WaitOne();   //wait for callback
                    //////
                    newTag++;
                }

            }).Start();
        }

        public void insertTag(string doc, string skosConcept)
        {
            new Thread(() =>
            {
                if (doc == null)        //nimm letzten Eintrag
                {

                    getMaxDocEntryOfGraph(callbackMaxDoc, null);
                    ////////
                    stopWaitHandle.WaitOne();   //wait for callback
                    //////
                    doc = lastGraphDoc;

                }
                getMaxTagOfGraph(callbackMaxTag, null);
                ////////
                stopWaitHandle.WaitOne();   //wait for callback
                //////
                double newTag = double.Parse(lastGraphTag.Substring(lastGraphTag.IndexOf(STR_TagEntry) + 8)) + 1;
                string queryInsert = "INSERT INTO <" + docGraph + "> {" +
                    "<" + doc + "> :tagged :TagEntry" + newTag.ToString("000000000") +
                    ". :TagEntry" + newTag.ToString("000000000") + " a :Tag; :means <" + skosConcept + ">;" +
                    "ctag:taggingDate \"" + XmlConvert.ToString(DateTime.Now, XmlDateTimeSerializationMode.Utc) +
                    "\"; ctag:Label \"added later on\".}";
                sendQuery(queryPrefix + queryInsert, callbackUpdatedDoc, null);
            }).Start();
        }

        public void deleteTag(string doc, string skosTag) 
        {
            string queryWith = "WITH <" + docGraph + "> ";
            queryWhere = "DELETE {?tag ?p ?o. ?s ?p ?tag.}" +
                "WHERE {{{?tag ?p ?o.} UNION {?s ?p ?tag}}" +
                "?tag :means ?skos. ?doc :tagged ?tag." +
                "FILTER (?doc = <" + doc + ">) FILTER (?skos = <" + skosTag + ">)}";
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
                lastGraphDoc = (set.Results[0].Value(set.Results[0].Variables.ElementAt(0).ToString()) as UriNode).Uri.AbsoluteUri;
            }
            catch (NullReferenceException)
            {
                lastGraphDoc = STR_DocEntry + "0";
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
                lastGraphTag = (set.Results[0].Value(set.Results[0].Variables.ElementAt(0).ToString()) as UriNode).Uri.AbsoluteUri;
            }
            catch (NullReferenceException)
            {
                lastGraphTag = STR_TagEntry + "0";
            }
            stopWaitHandle.Set();   //signal thread to continue
        }

        void callbackDoc(SparqlResultSet set, object state)
        {
            //stump for later use
            stopWaitHandle.Set();
        }
        
    }
}
