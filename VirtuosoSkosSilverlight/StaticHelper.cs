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
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Dynamic;
using System.Xml;
using VDS.RDF.Query;
using VDS.RDF;
using System.IO;
using System.Threading;

namespace VirtuosoQuery
{
    public static class StaticHelper
    {
        private static AutoResetEvent stopWaitHandle = new AutoResetEvent(false);
        public delegate void EventHandler(object sender, EventArgs args);  

        private const string STR_SparqlEndpointUrl = "SparqlEndpointUrl";
        private const string STR_DocumentGraphUri = "DocumentGraphUri";
        private const string STR_SkosGraphUri = "SkosGraphUri";
        private const string STR_SkosCoreUri = "SkosCoreUri";
        private const string STR_CommonTagUri = "CommonTagUri";
        private const string STR_DocGraphLanguageTag = "DocGraphLanguageTag";
        private const string STR_SkosGraphLanguageTag = "SkosGraphLanguageTag";
        private const string STR_DocEntryNumberLength = "0000000";
        private const string STR_DocumentGraph = "DocumentGraph";
        private const string STR_Language = "Language";
        private const string STR_Additional = "Additional";
        private const string STR_GraphSpecifics = "GraphSpecifics";
        private const string STR_SearchPredicates = "SearchPredicates";
        private const string STR_SearchStopWords = "SearchStopWords";
        private const string STR_Configurations = "Configurations";
        private const string STR_InnerXml = "InnerXml";
        private const int ReturnRowPropertyCount = 10;  
        
        public static string SkosGraphLanguageTag {get; private set;}
        public static string DocGraphLanguageTag { get; private set; }
        public static string skos { get; private set; }
        public static string ctag { get; private set; }
        public static string endpointUri { get; private set; }
        public static string docGraphUri { get; private set; }
        public static string skosGraphUri { get; private set; }
        public static string searchPredicates { get; private set; }
        public static string searchStopWords { get; private set; }

        public static void xmlConfigReader(string configUri, EventHandler configLoaded)
        {
            WebRequest request = HttpWebRequest.Create(configUri);
            Tuple<WebRequest, EventHandler> tuple = new Tuple<WebRequest, EventHandler>(request, configLoaded);
            request.BeginGetResponse(
                new AsyncCallback(wc_DownloadStringCompleted), tuple);
        }

        static void wc_DownloadStringCompleted(IAsyncResult ar)
        {
            Tuple<WebRequest, EventHandler> tuple = (ar.AsyncState as Tuple<WebRequest, EventHandler>);
            WebResponse response = tuple.Item1.EndGetResponse(ar);
            Stream responseStream = response.GetResponseStream();
            //try
            //{
                using (XmlReader reader = XmlReader.Create(responseStream))
                {
                    reader.ReadToFollowing(STR_DocumentGraph);
                    endpointUri = reader.GetAttribute(STR_SparqlEndpointUrl);
                    docGraphUri = reader.GetAttribute(STR_DocumentGraphUri);
                    skosGraphUri = reader.GetAttribute(STR_SkosGraphUri);
                    reader.ReadToFollowing(STR_Language);
                    DocGraphLanguageTag = reader.GetAttribute(STR_DocGraphLanguageTag);
                    SkosGraphLanguageTag = reader.GetAttribute(STR_SkosGraphLanguageTag);
                    reader.ReadToFollowing(STR_SearchPredicates);
                    reader.MoveToContent();
                    searchPredicates = reader.ReadElementContentAsString();
                    reader.ReadToFollowing(STR_SearchStopWords);
                    reader.MoveToContent();
                    searchStopWords = reader.ReadElementContentAsString();
                    reader.ReadToFollowing(STR_Additional);
                    skos = reader.GetAttribute(STR_SkosCoreUri);
                    ctag = reader.GetAttribute(STR_CommonTagUri);
                }
            //}
            //catch (Exception ex)
            //{
            //    throw new Exception("Error while loading configuration file: " + ex.Message);
            //}
                tuple.Item2.Invoke(null, new EventArgs());
        }

        public static List<ReturnRow> convertSparqlResultToListReturnRow(SparqlResultSet set)
        {
            List<ReturnRow> table = new List<ReturnRow>();
            ReturnRow iii = new ReturnRow();

            //first row = columnheader
            for(int i =0; i< set.Variables.Count();i++)
                iii.GetType().GetProperties()[i].SetValue(iii, set.Variables.ElementAt(i), null);
            table.Add(iii);

            if (set!=null)
            {
                try
                {
                    if (set.Results[0] != null)
                    {
                        foreach (SparqlResult res in set.Results)
                        {
                            ReturnRow ret = new ReturnRow();

                            for (int i = 0; i < (int)(Math.Min(set.Variables.Count(), ReturnRowPropertyCount)) ; i++)
                            {
                                object zwi = res.Value(set.Variables.ElementAt(i));
                                if (zwi.GetType() == typeof(LiteralNode))
                                    ret.GetType().GetProperties()[i].SetValue(ret, ((zwi as LiteralNode).Value), null);
                                else if (zwi.GetType() == typeof(UriNode))
                                    ret.GetType().GetProperties()[i].SetValue(ret, ((zwi as UriNode).Uri), null);
                            }

                            table.Add(ret);
                        }
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                } 
            }
            return table;
        }

        public static List<ReturnDocument> convertSparqlResultToListReturnDocument(SparqlResultSet set)
        {
            List<ReturnDocument> retList = new List<ReturnDocument>();
            if (set != null)
            {
                try
                {
                    if (set.Results[0] != null)
                    {
                        foreach (SparqlResult res in set.Results)
                        {
                            ReturnDocument ret = new ReturnDocument();
                            string listType = (res["spListType"] as UriNode).Uri.AbsoluteUri;

                            if((res["author"] as LiteralNode) != null)
                                ret.Author = (res["author"] as LiteralNode).Value;
                            ret.CreationDate = (res["created"] as LiteralNode).Value;
                            if((res["spListID"] as LiteralNode) != null)
                                ret.ListID = (res["spListID"] as LiteralNode).Value;
                            ret.Name = (res["title"] as LiteralNode).Value;
                            ret.UniqueID = (res["guid"] as LiteralNode).Value;
                            ret.ListType = (Entry)Enum.Parse(typeof(Entry), listType.Substring(listType.LastIndexOf('/')+1), true);
                            if((res["server"] as LiteralNode) != null)
                                ret.server = (res["server"] as LiteralNode).Value;

                            retList.Add(ret);
                        }
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                }
            }
            return retList;
        }

        public static List<ReturnTag> convertSparqlResultToListReturnTag(SparqlResultSet set)
        {
            List<ReturnTag> retList = new List<ReturnTag>();
            if (set != null)
            {
                try
                {
                    if (set.Results[0] != null)
                    {
                        foreach (SparqlResult res in set.Results)
                        {
                            ReturnTag ret = new ReturnTag();

                            if ((res["altLabels"] as LiteralNode) != null)
                                ret.altLabels = (res["altLabels"] as LiteralNode).Value;
                            if ((res["description"] as LiteralNode) != null)
                                ret.description = (res["description"] as LiteralNode).Value;
                            ret.prefLabel = (res["tagLabel"] as LiteralNode).Value;
                            ret.uri = (res["skosUri"] as UriNode).Uri.AbsoluteUri;

                            retList.Add(ret);
                        }
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                }
            }
            return retList;
        }

        public static List<ReturnRow> convertListStringArrayToDataTable(Tuple<string[], List<object[]>> input)
        {
            List<ReturnRow> list = new List<ReturnRow>();
            if (input != null && input.Item1 != null)
            {
                ReturnRow headers = new ReturnRow();
                for (int i = 0; i < input.Item1.Count(); i++)
                    headers.GetType().GetProperties()[i].SetValue(headers, input.Item1[i], null);
                list.Add(headers);

                foreach (object[] row in input.Item2)
                {
                    ReturnRow ret = new ReturnRow();

                    for (int i = 0; i < (int)(Math.Min(input.Item1.Count(), ReturnRowPropertyCount)); i++)
                    {
                            ret.GetType().GetProperties()[i].SetValue(ret, row[i], null);
                    }
                    list.Add(ret);
                }
            }
            return list;
        }

        public static List<ReturnTag> convertListStringArrayToTagList(Tuple<string[], List<object[]>> input)
        {
            List<ReturnTag> list = new List<ReturnTag>();
            if (input != null && input.Item1 != null)
            {
                //ReturnRow headers = new ReturnRow();
                //for (int i = 0; i < input.Item1.Count(); i++)
                //    headers.GetType().GetProperties()[i].SetValue(headers, input.Item1[i], null);
                //list.Add(new ReturnTag();

                foreach (object[] row in input.Item2)
                {
                    ReturnTag ret = new ReturnTag();

                    for (int i = 0; i < (int)(Math.Min(input.Item1.Count(), ReturnRowPropertyCount)); i++)
                    {
                        if (input.Item1[i] == "Uri")
                            ret.uri = row[i].ToString();
                        if (input.Item1[i] == "Concept")
                            ret.prefLabel = row[i].ToString();
                        if (input.Item1[i] == "altLabels" && row[i] != null)
                            ret.altLabels = row[i].ToString();
                        if (input.Item1[i] == "description" && row[i] != null)
                            ret.description = row[i].ToString();
                    }
                    list.Add(ret);
                }
            }
            return list;
        }
    }
}
