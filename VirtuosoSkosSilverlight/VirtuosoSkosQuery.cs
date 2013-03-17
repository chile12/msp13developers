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
using VDS.RDF;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
namespace VirtuosoSkosSilverlight
{
    public class VirtuosoSkosQuery// : ISkosQuery
    {
        public delegate void SearchResultCallback(Tuple<string[], List<object[]>> set);
        AutoResetEvent stopWaitHandle = new AutoResetEvent(false);
        private SparqlResultSet stopGap = new SparqlResultSet();
        private const string prefixSTR = "prefix";
        private string xsd = SkosAndStoreSettings.xmlSchema;
        private string skos = SkosAndStoreSettings.skosCore;
        public SparqlRemoteEndpoint endpoint;

        private Dictionary<string, string> prefixes = new Dictionary<string, string>();
        private Uri skosGraph;                               //Graphuri des Ontologiegraphen (http://{Triplestore-IP}:{Port}/{Graphname})
        private string languageTag;

        private string queryPrefix;
        private string querySelect;
        private string queryFrom;
        private string queryWhere;
        private string queryOrder;
        private string queryLanguageFilter;

        SparqlResultSet callbackSet = null;

        public VirtuosoSkosQuery(SparqlRemoteEndpoint endpoint)
        {
            this.endpoint = endpoint;

            foreach (System.Collections.DictionaryEntry obj in SkosAndStoreSettings.ResourceManager.GetResourceSet(CultureInfo.CurrentCulture, true, false))
            {
                if (obj.Key.ToString().Substring(0, prefixSTR.Length) == prefixSTR)
                {
                    prefixes.Add(obj.Key.ToString().Substring(prefixSTR.Length), obj.Value.ToString());
                }
            }

            this.skosGraph = UriFactory.Create(endpoint.DefaultGraphs[0]);
            this.languageTag = SkosAndStoreSettings.skosLanguage;

            queryPrefix = "PREFIX skos: <" + skos + "> ";
            querySelect = "SELECT DISTINCT ?concept (str(?name) AS ?conceptname) ";
            queryFrom = "FROM <" + skosGraph.AbsoluteUri + "> ";
            queryLanguageFilter = "FILTER langMatches( lang(?name), \"" + languageTag + "\" )";
        }

        /// <summary>
        /// gets all concept-uris of predicate 'skos#narrower' from a given concept
        /// uses SPARQL 1.1 concepts
        /// </summary>
        /// <param name="concept">concepturi or abrivation</param>
        /// <returns></returns>
        public void narrower(SparqlResultsCallback callback, string concept, OrderDirection order)
        {
            //queryWhere = "WHERE { <" + concept + "> <" + skos + "narrower> " + " ?concept. ?concept <"
            //    + skos + "prefLabel> ?name " + queryLanguageFilter + "} ";
            //queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";  querySelect + queryFrom + queryWhere + queryOrder

            endpoint.QueryWithResultSet(SkosQueryBuilder.getQuery(Querys.Narrower, OrderDirection.ASC, null, new string[]{concept}) , callback, null);

        }
        public void narrower(SparqlResultsCallback callback, string concept)
        {
            narrower(callback, concept, OrderDirection.NONE);
        }

        public void broader(SparqlResultsCallback callback, string concept, OrderDirection order)
        {
            //queryWhere = "WHERE { <" + concept + "> <" + skos + "broader> " + " ?concept. ?concept <"
            //    + skos + "prefLabel> ?name FILTER langMatches( lang(?name), \"" + languageTag + "\" )} ";
            //queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";  querySelect + queryFrom + queryWhere + queryOrder,

            //SparqlResultsCallback callback = new SparqlResultsCallback(sparqlCallback);
            string query = SkosQueryBuilder.getQuery(Querys.Broader, OrderDirection.ASC, skosGraph.AbsoluteUri, new string[]{concept});
            endpoint.QueryWithResultSet(query, callback, null);

        }
        public void broader(SparqlResultsCallback callback, string concept)
        {
            broader(callback, concept, OrderDirection.NONE);
        }

        public void broaderTransitive(SparqlResultsCallback callback,  string concept, int maxDist, OrderDirection order)
        {
            querySelect = "SELECT DISTINCT ?concept ?name ?distance ";
            queryWhere = "WHERE { { SELECT ?in ?concept  WHERE { ?in skos:broader ?concept } }" +
                "OPTION ( TRANSITIVE, t_distinct, t_in(?in), t_out(?concept), t_min (1), t_max(" + maxDist + "), t_step ('step_no') as ?distance ). " +
                "?concept skos:prefLabel ?name. " + queryLanguageFilter + ". FILTER ( ?in = <" + concept + "> ) }";
            queryOrder = "ORDER BY ?distance " + order.ToString().Replace("NONE", "") + "(?name)";  

            //SparqlResultsCallback callback = new SparqlResultsCallback(sparqlCallback);
            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, null);
            //string query = SkosQueryBuilder.getQuery(Querys.Broader, OrderDirection.ASC, skosGraph.AbsoluteUri, new string[] { concept, maxDist.ToString() });
            
        }
        public void broaderTransitive(SparqlResultsCallback callback,  string concept, int maxDist)
        {
            broaderTransitive(callback, concept, maxDist, OrderDirection.NONE);
        }

        public void narrowerTransitive(SparqlResultsCallback callback, string concept, int maxDist, OrderDirection order)
        {
            querySelect = "SELECT DISTINCT ?concept ?name ?distance ";
            queryWhere = "WHERE { { SELECT ?in ?concept  WHERE { ?in skos:narrower ?concept } }" +
                "OPTION ( TRANSITIVE, t_distinct, t_in(?in), t_out(?concept), t_min (1), t_max(" + maxDist + "), t_step ('step_no') as ?distance ). " +
                "?concept skos:prefLabel ?name. " + queryLanguageFilter + ". FILTER ( ?in = <" + concept + "> ) }";
            queryOrder = "ORDER BY ?distance " + order.ToString().Replace("NONE", "") + "(?name)";

            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, null);

        }
        public void narrowerTransitive(SparqlResultsCallback callback, string concept, int maxDist)
        {
           narrowerTransitive(callback, concept, maxDist, OrderDirection.NONE);
        }

        public void broaderSiblings(SparqlResultsCallback callback, string concept, OrderDirection order)
        {
            queryWhere = "WHERE { <" + concept + "> skos:broader  ?z. ?z skos:narrower ?concept." +
                                    "?concept skos:prefLabel ?name FILTER langMatches( lang(?name), \"" + languageTag +
                                    "\" ) FILTER (?concept != <" + concept + ">)} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";

            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, null);

        }
        public void broaderSiblings(SparqlResultsCallback callback, string concept)
        {
            broaderSiblings(callback, concept, OrderDirection.NONE);
        }

        public void narrowerSiblings(SparqlResultsCallback callback, string concept, OrderDirection order)
        {
            queryWhere = "WHERE { <" + concept + "> skos:narrower  ?z. ?z skos:broader ?concept." +
                                    "?concept skos:prefLabel ?name FILTER langMatches( lang(?name), \"" + languageTag +
                                    "\" ) FILTER (?concept != <" + concept + ">)} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";

            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, null);

        }
        public void narrowerSiblings(SparqlResultsCallback callback, string concept)
        {
            narrowerSiblings(callback, concept, OrderDirection.NONE);
        }

        public void relatedTo(SparqlResultsCallback callback, string concept, OrderDirection order)
        {
            queryWhere = "WHERE { <" + concept + "> skos:related  ?concept." +
                                    "?concept skos:prefLabel ?name FILTER langMatches( lang(?name), \"" + languageTag + "\" )} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";

            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, null);

        }
        public void relatedTo(SparqlResultsCallback callback, string concept)
        {
            relatedTo(callback, concept, OrderDirection.NONE);
        }

        public void propertiesOf(SparqlResultsCallback callback, string concept, OrderDirection order)
        {
            querySelect = "SELECT DISTINCT ?pred ?obj ";
            queryWhere = "WHERE {<" + concept + "> ?pred ?obj " +
                "FILTER (?pred != skos:broader && ?pred != skos:narrower && " +
                "?pred != skos:broaderTransitive && ?pred != skos:narrowerTransitive &&" +
                "?pred != skos:related && ?pred != skos:prefLabel)" +
                "FILTER (langMatches( lang(?obj), \"" + languageTag + "\" ) || !(langMatches( lang(?obj), \"*\" )))}";

            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";

            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, null);

        }
        public void propertiesOf(SparqlResultsCallback callback, string concept)
        {
            propertiesOf(callback, concept, OrderDirection.NONE);
        }

        public void topGraphConcepts(SparqlResultsCallback callback, string graphUri, OrderDirection order)
        {
            querySelect = "SELECT DISTINCT ?topConcept ?conceptName ";
            string from = "FROM <" + graphUri + "> ";
            queryWhere = "WHERE { ?topConcept skos:topConceptOf ?x. " +
                "?topConcept skos:prefLabel ?conceptName. FILTER langMatches( lang(?conceptName), \"" + languageTag + "\" )} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?conceptName)";

            endpoint.QueryWithResultSet(queryPrefix + querySelect + from + queryWhere + queryOrder, callback, null);

        }
        public void topGraphConcepts(SparqlResultsCallback callback, OrderDirection order)
        {
            topGraphConcepts(callback, skosGraph.AbsoluteUri, order);
        }
        public void topGraphConcepts(SparqlResultsCallback callback)
        {
            topGraphConcepts(callback, skosGraph.AbsoluteUri, OrderDirection.NONE);
        }

        public void searchTag(SearchResultCallback callback, string searchString, int topKResults)
        {
            int relevance = 4;
            int hitPos = 3;
            string STR_uri = "uri";
            string STR_hit = "hit";
            string STR_concept = "concept";
            List<object[]> resultList = new List<object[]>();   //saves uri of a hit, name of the hit concept, predicate which lead to the hit, the actual hit and the cummulated relevance
            List<string> uriList = new List<string>();          //saves every uri in order of their appearence - is in sync with resultlist and relevanceList - used for fast position-retrival of an uri
            List<float> relevanceList = new List<float>();      //seperate list for accumulating the relevance of a certain hit - will be added to the result list at the end
            List<string> hitSaver = new List<string>();         //list for savekeeping of distinct hits for all predicates
            Regex regex = new Regex(@"[ ]{2,}");
            searchString = regex.Replace(searchString, @" ");   //delete alle extra whitespaces
            string[] keywords = searchString.Split(' ');
            //kill every word with less than 4 letters (in lieu of stop-word-list)
            int checkForEmptyString = 0;
            for (int i = 0; i < keywords.Count(); i++)
            {
                if (keywords[i].Count() < 4)
                {
                    keywords[i] = null;
                    checkForEmptyString++;
                }
            }
            if (checkForEmptyString == keywords.Count())
                return;

            //take relevant skos:predicates from settings file, the order of their entry is also of importance
            //the first entry has the highest relevance, prefLabel gets an extra 50% relevance in the course of this algorithm
            string[] predicates = SkosAndStoreSettings.searchPredicates.Replace(';', ',').Replace(" ", "").Split(',');

            querySelect = "SELECT ?uri (str(?name) as ?concept) ?hit ";

            //asynchronous callbacks make it necessary to synchronize 
            new Task(() =>
            {
                //algorithm starts here
                foreach (string key in keywords)
                {
                    if (key != null)
                    {
                        List<string> zwHitSaver = new List<string>();

                        for (int i = 0; i < predicates.Count(); i++)
                        {

                            queryWhere = "WHERE { ?uri skos:prefLabel ?name. ?uri skos:" + predicates[i] + " ?hit." +
                                "FILTER (regex(?hit, \"" + key + "\", \"i\")) FILTER (langMatches( lang(?hit), \"" +
                                languageTag + "\") && langMatches(lang(?hit), \"*\"))" +
                                "FILTER (langMatches( lang(?name), \"" + languageTag + "\")) }";


                            SparqlResultsCallback callthere = new SparqlResultsCallback(callhere);
                            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere, callthere, null);
                                
                            ////////
                            stopWaitHandle.WaitOne(); //stop thread to wait for callback
                            ///////

                            List<string> sameUrisForPredicateList = new List<string>();

                            foreach (SparqlResult res in stopGap.Results)
                            {
                                zwHitSaver.Add((res.Value(STR_hit) as LiteralNode).Value);
                                string thisUri = (res.Value(STR_uri) as UriNode).Uri.AbsoluteUri;
                                sameUrisForPredicateList.Add(thisUri);

                                int wordCount = (res.Value(STR_hit) as LiteralNode).Value.Count() - (res.Value(STR_hit) as LiteralNode).Value.Replace(" ", "").Count() + 1;
                                float numberOfIdentHits = (hitSaver.Where(x => x == (res.Value(STR_hit) as LiteralNode).Value).Count() + 1);
                                float numberOfHitsForPredicate = sameUrisForPredicateList.Where(x => x == thisUri).Count();

                                float addValue = (float)(predicates.Count() - i) * numberOfIdentHits * numberOfIdentHits / (float)(Math.Min(wordCount, 5) * numberOfHitsForPredicate * numberOfHitsForPredicate);

                                if (predicates[i] == "prefLabel")
                                    addValue = addValue * 2f;
                                if (addValue < 0.1)
                                    addValue = 0.1f;

                                if (uriList.Contains(thisUri))
                                {
                                    int pos = uriList.IndexOf(thisUri);
                                    relevanceList[pos] = relevanceList[pos] + addValue;
                                    if (!resultList[pos][hitPos].ToString().Contains((res.Value(STR_hit) as LiteralNode).Value))
                                        resultList[pos][hitPos] = resultList[pos][hitPos] + "; " + (res.Value(STR_hit) as LiteralNode).Value;
                                }
                                else
                                {
                                    //adding {uri, conceptname, predicate, hitstring, placeholder for hitvalue}
                                    resultList.Add(new object[] { thisUri, (res.Value(STR_concept) as LiteralNode).Value, predicates[i], (res.Value(STR_hit) as LiteralNode).Value, 0f });
                                    uriList.Add((res.Value(STR_uri) as UriNode).Uri.AbsoluteUri);
                                    relevanceList.Add(addValue);
                                }
                            }
                            hitSaver.AddRange(zwHitSaver.Distinct());
                        }
                    }
                }

                for (int i = 0; i < relevanceList.Count; i++)
                    resultList[i][relevance] = (object)relevanceList[i];

                resultList = resultList.OrderByDescending(x => float.Parse(x[relevance].ToString())).ToList();
                if (resultList.Count > topKResults)
                    resultList.RemoveRange(topKResults, resultList.Count - topKResults);

                Tuple<string[], List<object[]>> retTuple = new Tuple<string[], List<object[]>>(new string[] { "Uri", "Concept", "Predicate", "Hit", "Relevance" }, resultList);
                callback.Invoke(retTuple);
            }).Start();
        }

        public void memberOf(SparqlResultsCallback callback, string concept, OrderDirection order)
        {
            querySelect = "SELECT ?concept (str(?name) as ?name) ";
            queryWhere = "WHERE { {SELECT ?s ?concept WHERE { ?concept skos:member ?s } } " +
                "OPTION ( TRANSITIVE, t_distinct, t_in(?s), t_out(?concept),t_min (1)) ." +
                "?concept skos:prefLabel ?name. " +
                queryLanguageFilter + "FILTER ( ?s = <" + concept + "> )} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";

            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, null);

        }
        public void memberOf(SparqlResultsCallback callback, string concept)
        {
            memberOf(callback, concept, OrderDirection.NONE);
        }

        public void getMembersOf(SparqlResultsCallback callback, string collection, OrderDirection order)
        {
            querySelect = "SELECT ?member (str(?name) as ?name)";
            queryWhere = "WHERE {<" + collection + "> skos:member ?member. ?member skos:prefLabel ?name." +
                "FILTER langMatches( lang(?name), \"" + languageTag + "\" ) } ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";

            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, null);

        }
        public void getMembersOf(SparqlResultsCallback callback, string collection)
        {
            getMembersOf(callback, collection, OrderDirection.NONE);
        }

        public void getTransMembersOf(SparqlResultsCallback callback, string collection, OrderDirection order)
        {
            querySelect = "SELECT ?concept (str(?name) as ?name) ";
            queryWhere = "WHERE { {SELECT ?s ?concept WHERE { ?s skos:member ?concept } } " +
                "OPTION ( TRANSITIVE, t_distinct, t_in(?s), t_out(?concept),t_min (1)) ." +
                "?concept skos:prefLabel ?name. ?concept a skos:Concept. " +
                queryLanguageFilter + "FILTER ( ?s = <" + collection + "> )} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";

            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, null);

        }
        public void getTransMembersOf(SparqlResultsCallback callback, string collection)
        {
            getTransMembersOf(callback, collection, OrderDirection.NONE);
        }

        private void callhere(SparqlResultSet result, object state)
        {
                stopGap = result; 
                stopWaitHandle.Set();   //signal thread of the search-algorithm to continue
        }

    }
}
