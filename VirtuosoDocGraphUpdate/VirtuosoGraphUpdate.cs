using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF;
using System.IO;
using VDS.RDF.Query;
using System.Xml;
using System.Threading;
using Microsoft.SharePoint;

namespace VirtuosoDocGraphUpdate
{
    public enum Entry { Generic, Document, Discussion, Survey, Issue, Page, Event }
    public class VirtuosoGraphUpdate
    {
        private SparqlRemoteEndpoint endpoint;

        private string skos;
        private string ctag;
        private string endpointUrl;
        private string docGraphUri;

        private string filePath;             

        private string queryPrefix;
        private string querySelect;
        private string queryFrom;
        private string queryWhere;
        private string queryWith;
        private string languageTag;

        private const string STR_SparqlEndpointUrl = "SparqlEndpointUrl";
        private const string STR_DocumentGraphUri = "DocumentGraphUri";
        private const string STR_SkosCoreUri = "SkosCoreUri";
        private const string STR_CommonTagUri = "CommonTagUri";
        private const string STR_LanguageTag = "DocGraphLanguageTag";
        private const string STR_DocEntryNumberLength = "0000000";
        private const string STR_DocumentGraph = "DocumentGraph";
        private const string STR_Language = "Language";
        private const string STR_Additional = "Additional";
        private const string STR_GraphSpecifics = "GraphSpecifics";
        private const string STR_SearchPredicates = "SearchPredicates";
        private const string STR_SearchStopWords = "SearchStopWords";
        private const string STR_Configurations = "Configurations";
        private const string STR_InnerXml = "InnerXml";

        public VirtuosoGraphUpdate(string featureRoot)
        {
            this.filePath = featureRoot;
            xmlConfigReader();
            endpoint = new SparqlRemoteEndpoint(new Uri(this.endpointUrl));

            queryPrefix = "PREFIX : <" + docGraphUri + "/> PREFIX skos: <" + skos + "> PREFIX ctag: <" + ctag + "> ";
            queryFrom = "FROM <" + docGraphUri + "> ";
        }

        public string getMaxEntryEntryOfGraph()
        {
            querySelect = "SELECT DISTINCT (MAX(?uri)) ";
            queryWhere = "WHERE { ?uri a :spItem. } ";
            SparqlResultSet set = endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere);
            string ret ="";
            if (set.Results.Count > 0 && set.Results[0][0] != null)
            {
                ret = (set.Results[0][0] as UriNode).Uri.AbsoluteUri;
                ret = (double.Parse(ret.Substring(ret.IndexOf("Entry") + 8))+1).ToString(STR_DocEntryNumberLength);
            }
            else
                ret = STR_DocEntryNumberLength;
            return ret;
        }

        public void insertEntry(string guid, string spListID, string name, DateTime created, Entry entry, string author = null, string spServer = "default")
        {
            string insertNumber = getMaxEntryEntryOfGraph();

            string queryInsert = "INSERT INTO <" + docGraphUri + "> {" +
            ":Entry" + insertNumber + " a 	:spItem ; " +
            ":spGuid \"" + guid + "\" ;" +
            ":spListID \"" + spListID + "\" ;" +
            ":name 	\"" + name + "\"@" + languageTag + "; " +
            ":created \"" + XmlConvert.ToString(created, XmlDateTimeSerializationMode.Utc) + "\";" +
            ":spListType :" + entry.ToString() + " ;";

            if (spServer != null && spServer != "")
                queryInsert += ":spServer \"" + spServer + "\"; ";

            if (author != null && spServer != "")
                queryInsert += ":author \"" + author + "\"; ";

            queryInsert = queryInsert.Remove(queryInsert.LastIndexOf(';'));
            queryInsert += ". }";

            endpoint.QueryWithResultSet(queryPrefix + queryInsert);
        }

        public void deleteEntryAndTagsByGuid(string spGuid)
        {
            queryWith = "WITH <" + docGraphUri + "> ";
            queryWhere = "DELETE {?doc ?p ?o. ?tag ?p2 ?o2.}" +
                "WHERE {{{?doc ?p ?o.} UNION {?tag ?p2 ?o2}}" +
                "OPTIONAL{?doc :tagged ?tag.} ?doc :spGuid ?guid." +
                "?doc a :spItem. FILTER (?guid = \"" + spGuid + "\")}";
            endpoint.QueryWithResultSet(queryPrefix + queryWith + queryWhere);
        }

        public string getEntryUriFromGuid(string spGuid)
        {
            querySelect = "SELECT ?uri ";
            queryWhere = "WHERE {?uri a :spItem. ?uri :spGuid ?guid. FILTER(?guid = \"" + spGuid + "\")}";
            SparqlResultSet set = endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere);

            if (set.Results.Count == 0 || set.Results[0][0] == null)
                return null;
            else
                return (set.Results[0][0] as UriNode).Uri.AbsoluteUri;
        }

        public void updateListIdByGuid(string spGuid, string newListID)
        {
            string queryInsert = "INSERT INTO <" + docGraphUri + "> {" +
                "?doc :spListID \"" + newListID + "\".}";
            queryWhere = "WHERE { ?doc :spGuid ?guid. FILTER (?guid = \"" + spGuid + "\")}";
            endpoint.QueryWithResultSet(queryPrefix + queryInsert + queryWhere);
        }

        public void deleteListIdByGuid(string spGuid, string listID)
        {
            queryWith = "WITH <" + docGraphUri + "> ";
            queryWhere = "DELETE {?doc ?spListID ?listID.} WHERE {?doc ?spListID ?listID. ?doc :spGuid ?guid. " +
                "?doc :spListID ?listID. Filter(?listID = \"" + listID + "\") FILTER(?guid = \"" + spGuid + "\")}";
            endpoint.QueryWithResultSet(queryPrefix + queryWith + queryWhere);
        }

        private void xmlConfigReader()
        {
            if (File.Exists(filePath))
            {
                XmlTextReader reader = new XmlTextReader(filePath);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.MoveToContent();
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(reader.ReadInnerXml());

                IEnumerable<XmlNode> childList = doc.FirstChild.ChildNodes.OfType<XmlNode>().Where(x => x.NodeType == XmlNodeType.Element);

                endpointUrl = childList.ElementAt(0).Attributes[STR_SparqlEndpointUrl].Value.ToString();
                docGraphUri = childList.ElementAt(0).Attributes[STR_DocumentGraphUri].Value.ToString();
                languageTag = childList.ElementAt(1).Attributes[STR_LanguageTag].Value.ToString();
                skos = childList.ElementAt(3).Attributes[STR_SkosCoreUri].Value.ToString();
                ctag = childList.ElementAt(3).Attributes[STR_CommonTagUri].Value.ToString();
                reader.Close();
            }
            else
            {
                throw new Exception("Configuration file was not found.");
            }
        }
    }
}
