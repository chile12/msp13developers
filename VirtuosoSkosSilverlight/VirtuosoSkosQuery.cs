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

namespace VirtuosoQuery.Silverlight.Skos
{
    /// <summary>
    /// implements ISkosQuery: all queries pertaining to the skos-graph
    /// </summary>
    public class VirtuosoSkosQuery : TripleStoreQuery, ISkosQuery
    {
        public delegate void SearchResultCallback(Tuple<string[], List<object[]>> set);         //delegate for search results
        AutoResetEvent stopWaitHandle = new AutoResetEvent(false);                              //used for stopping a thread to wait for an other process to finish which then signales to proceed
        private SparqlResultSet stopGap = new SparqlResultSet();                                //ResultSet for temporary purposes
       
        private SparqlRemoteEndpoint endpoint;                                                   //a Sparql-endpint
        private Dictionary<string, string> prefixes = new Dictionary<string, string>();         //stores al prefixes needed in this context

        private string skosCore;                                                                //skos-core uri
        private string tagGraph = null;                                                         //skos-tag-graph uri
        private string docGraph = null;                                                         //enty-graph uri

        private const string prefixSTR = "prefix";                                              //query-components
        private string languageTag;
        private string queryPrefix;
        private string querySelect;
        private string queryFrom;
        private string queryWhere;
        private string queryOrder;
        private string queryLanguageFilter;                                                     //the laguage used

        /// <summary>
        /// constructor for a skos-query
        /// </summary>
        /// <param name="endpoint">needs an sparql-endpont to query on</param>
        public VirtuosoSkosQuery()
        {
            
            if (StaticHelper.endpointUri != null)
            {
                this.endpoint = new SparqlRemoteEndpoint(new Uri(StaticHelper.endpointUri));    //new endpoint-uri from the configurations
                this.docGraph = StaticHelper.docGraphUri;
                this.tagGraph = StaticHelper.skosGraphUri;
                this.languageTag = StaticHelper.SkosGraphLanguageTag;                   //get the given language from config-file
                this.skosCore = StaticHelper.skos;

                queryPrefix = "PREFIX skos: <" + skosCore + "> ";                       //set simple query components
                querySelect = "SELECT DISTINCT ?uri (str(?name) AS ?conceptname) ";
                queryFrom = "FROM <" + tagGraph + "> ";
                queryLanguageFilter = "FILTER langMatches( lang(?name), \"" + languageTag + "\" )";
                initialized = true;
            }
        }

        /// <summary>
        /// gets all concept-uris of predicate 'skos#narrower' from a given concept
        /// </summary>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param>
        /// <param name="concept">the given concept (the object of narrower triple)</param>
        /// <param name="order">the return-oder of the ResultSet</param>
        /// <param name="state">an optional reach-though object</param>
        public void narrower(SparqlResultsCallback callback, string concept, OrderDirection order, object state)
        {
            queryWhere = "WHERE { <" + concept + "> skos:narrower ?uri. ?uri <"             //fill out query components
                + skosCore + "prefLabel> ?name " + queryLanguageFilter + "} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";
            string query = querySelect + queryFrom + queryWhere + queryOrder;
            endpoint.QueryWithResultSet(query, callback, state);                    //place a query (callback; delegate for callhere(Resulset, object), state; a possible reach-trough object)

        }
        /// <summary>
        /// gets all concept-uris of predicate 'skos#narrower' from a given concept
        /// </summary>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param>
        /// <param name="concept">the given concept (the object of narrower triple)</param>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param>        
        /// <param name="state">an optional reach-though object</param>
        public void narrower(SparqlResultsCallback callback, string concept)
        {
            narrower(callback, concept, OrderDirection.NONE, null);
        }
        /// <summary>
        /// used for graph-navigation or visualization 
        /// returns all concepts which are skosCore:broader to a given concept
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param>      
        /// <param name="state">an optional reach-though object</param>
        public void broader(SparqlResultsCallback callback, string concept, OrderDirection order, object state)
        {
            queryWhere = "WHERE { <" + concept + "> skos:broader ?uri. ?uri <"
                + skosCore + "prefLabel> ?name FILTER langMatches( lang(?name), \"" + languageTag + "\" )} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";  

            //SparqlResultsCallback callback = new SparqlResultsCallback(sparqlCallback);
            string query = querySelect + queryFrom + queryWhere + queryOrder; 
            //SkosQueryBuilder.getQuery(Querys.Broader, OrderDirection.ASC, skosGraph.AbsoluteUri, new string[]{concept});
            endpoint.QueryWithResultSet(query, callback, state);

        }
        /// <summary>
        /// used for graph-navigation or visualization 
        /// returns all concepts which are skosCore:broader to a given concept -- without an ordered resultset
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param>        
        public void broader(SparqlResultsCallback callback, string concept)
        {
            broader(callback, concept, OrderDirection.NONE, null);
        }
        /// <summary>
        /// used for graph-navigation or visualization 
        /// returns all concepts which are transitively skosCore:broader to a given concept
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <param name="order">orderdirection of the resultset for conceptnames</param>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param>        
        /// <param name="state">an optional reach-though object</param>
        /// <param name="maxDist">max. number of transitive iterations</param>  
        public void broaderTransitive(SparqlResultsCallback callback, string concept, int maxDist, OrderDirection order, object state)
        {
            querySelect = "SELECT DISTINCT ?concept ?name ?distance ";
            queryWhere = "WHERE { { SELECT ?in ?concept  WHERE { ?in skos:broader ?concept } }" +
                "OPTION ( TRANSITIVE, t_distinct, t_in(?in), t_out(?concept), t_min (1), t_max(" + maxDist + "), t_step ('step_no') as ?distance ). " +
                "?concept skos:prefLabel ?name. " + queryLanguageFilter + ". FILTER ( ?in = <" + concept + "> ) }";
            queryOrder = "ORDER BY ?distance " + order.ToString().Replace("NONE", "") + "(?name)";  

            //SparqlResultsCallback callback = new SparqlResultsCallback(sparqlCallback);
            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, state);
            //string query = SkosQueryBuilder.getQuery(Querys.Broader, OrderDirection.ASC, skosGraph.AbsoluteUri, new string[] { concept, maxDist.ToString() });
            
        }
        /// <summary>
        /// used for graph-navigation or visualization 
        /// returns all concepts which are transitively skosCore:broader to a given concept -- without an ordered resultset
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param>     
        /// <param name="maxDist">max. number of transitive iterations</param>     
        public void broaderTransitive(SparqlResultsCallback callback,  string concept, int maxDist)
        {
            broaderTransitive(callback, concept, maxDist, OrderDirection.NONE, null);
        }
        /// <summary>
        /// used for graph-navigation or visualization 
        /// returns all concepts which are transitively skosCore:narrower to a given concept
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <param name="order">orderdirection of the resultset for conceptnames</param>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param>        
        /// <param name="state">an optional reach-though object</param>
        /// <param name="maxDist">max. number of transitive iterations</param>  
        public void narrowerTransitive(SparqlResultsCallback callback, string concept, int maxDist, OrderDirection order, object state)
        {
            querySelect = "SELECT DISTINCT ?concept ?name ?distance ";
            queryWhere = "WHERE { { SELECT ?in ?concept  WHERE { ?in skos:narrower ?concept } }" +
                "OPTION ( TRANSITIVE, t_distinct, t_in(?in), t_out(?concept), t_min (1), t_max(" + maxDist + "), t_step ('step_no') as ?distance ). " +
                "?concept skos:prefLabel ?name. " + queryLanguageFilter + ". FILTER ( ?in = <" + concept + "> ) }";
            queryOrder = "ORDER BY ?distance " + order.ToString().Replace("NONE", "") + "(?name)";

            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, state);

        }
        /// <summary>
        /// used for graph-navigation or visualization 
        /// returns all concepts which are transitively skosCore:narrower to a given concept
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param>     
        /// <param name="maxDist">max. number of transitive iterations</param>  
        public void narrowerTransitive(SparqlResultsCallback callback, string concept, int maxDist)
        {
           narrowerTransitive(callback, concept, maxDist, OrderDirection.NONE, null);
        }
        /// <summary>
        /// used for graph-navigation or visualization 
        /// returns all concepts which have at least one narrower/broader concepts of the narrower/broader concepts of a given concept
        /// therefore those concepts are on a similar level in the hierarchy pertaining to a related context
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <param name="order">orderdirection of the resultset for conceptnames</param>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param>        
        /// <param name="state">an optional reach-though object</param>
        public void siblings(SparqlResultsCallback callback, string concept, OrderDirection order, object state)
        {
            querySelect = "SELECT DISTINCT ?uri (str(?name) AS ?sibling) ";
            queryWhere = "WHERE { {{?concept skos:broader  ?z. ?z skos:narrower ?uri} UNION {?concept skos:narrower  ?y. ?y skos:broader ?uri}} " + 
                         "FILTER NOT EXISTS {{?concept skos:broader  ?uri} UNION {?concept skos:narrower  ?uri} UNION {?concept skos:related  ?uri}}. " +
                                    "?uri skos:prefLabel ?name FILTER langMatches( lang(?name), \"" + languageTag +
                                    "\" ) FILTER (?uri != <" + concept + ">) Filter (?concept = <" + concept + ">)} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";

            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, state);

        }
        /// <summary>
        /// used for graph-navigation or visualization 
        /// returns all concepts which have at least one narrower/broader concepts of the narrower/broader concepts of a given concept
        /// therefore those concepts are on a similar level in the hierarchy pertaining to a related context
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param>     
        public void siblings(SparqlResultsCallback callback, string concept)
        {
            siblings(callback, concept, OrderDirection.NONE, null);
        }
        /// <summary>
        /// used for graph-navigation or visualization 
        /// returns all concepts which are skosCore:related to a given concept
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <param name="order">orderdirection of the resultset for conceptnames</param>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param>        
        /// <param name="state">an optional reach-though object</param>
        public void relatedTo(SparqlResultsCallback callback, string concept, OrderDirection order, object state)
        {
            
            querySelect = "SELECT DISTINCT (str(?concept) AS ?uri) (str(?name) AS ?related) ";
            queryWhere = "WHERE { <" + concept + "> skos:related  ?concept." +
                                    "?concept skos:prefLabel ?name FILTER langMatches( lang(?name), \"" + languageTag + "\" )} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";

            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, state);

        }
        /// <summary>
        /// used for graph-navigation or visualization 
        /// returns all concepts which are skosCore:related to a given concept
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param>        
        public void relatedTo(SparqlResultsCallback callback, string concept)
        {
            relatedTo(callback, concept, OrderDirection.NONE, null);
        }
        /// <summary>
        /// returns all properties & values which have nor uri-values and are therefore not used for graph-navigation
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <param name="order">orderdirection of the resultset for properties</param>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param>        
        /// <param name="state">an optional reach-though object</param>
        public void propertiesOf(SparqlResultsCallback callback, string concept, OrderDirection order, object state)
        {
            querySelect = "SELECT DISTINCT ?pred ?obj ";
            queryWhere = "WHERE {<" + concept + "> ?pred ?obj " +
                "FILTER (?pred != skos:broader && ?pred != skos:narrower && " +
                "?pred != skos:broaderTransitive && ?pred != skos:narrowerTransitive &&" +
                "?pred != skos:related && ?pred != skos:prefLabel)" +
                "FILTER (langMatches( lang(?obj), \"" + languageTag + "\" ) || !(langMatches( lang(?obj), \"*\" )))}";

            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";

            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, state);

        }
        /// <summary>
        /// returns all properties & values which have nor uri-values and are therefore not used for graph-navigation
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param>    
        public void propertiesOf(SparqlResultsCallback callback, string concept)
        {
            propertiesOf(callback, concept, OrderDirection.NONE, null);
        }
        /// <summary>
        /// returns all topconcepts of a given graph
        /// </summary>
        /// <param name="graphUri">the graph-uri as string</param>
        /// <param name="order">orderdirection of the resultset for conceptnames</param>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param>        
        /// <param name="state">an optional reach-though object</param>
        public void topGraphConcepts(SparqlResultsCallback callback, string graphUri, OrderDirection order, object state)
        {
            querySelect = "SELECT DISTINCT ?topConcept ?conceptName ";
            string from = "FROM <" + graphUri + "> ";
            queryWhere = "WHERE { ?topConcept skos:topConceptOf ?x. " +
                "?topConcept skos:prefLabel ?conceptName. FILTER langMatches( lang(?conceptName), \"" + languageTag + "\" )} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?conceptName)";

            endpoint.QueryWithResultSet(queryPrefix + querySelect + from + queryWhere + queryOrder, callback, state);

        }
        /// <summary>
        /// returns all topconcepts of a given graph
        /// </summary>
        /// <param name="order">orderdirection of the resultset for conceptnames</param>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param>     
        public void topGraphConcepts(SparqlResultsCallback callback, OrderDirection order)
        {
            topGraphConcepts(callback, tagGraph, order, null);
        }
        /// <summary>
        /// returns all topconcepts of a given graph
        /// </summary>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param>   
        public void topGraphConcepts(SparqlResultsCallback callback)
        {
            topGraphConcepts(callback, tagGraph, OrderDirection.NONE, null);
        }
        /// <summary>
        /// searches for all words (-stopwords) of a searchstring in all LiteralNodes linked with skos:predicates 
        /// of given predicatelist (is provided via config-file) and weights all 'hits' for their relevance
        /// for some additional information on the used algorithm, please consult the documentation
        /// </summary>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param>
        /// <param name="searchString">the input string from the user</param>
        /// <param name="topKResults">max number of results</param>
        public void searchTag(SearchResultCallback callback, string searchString, int topKResults)
        {
            //constants
            int relevance = 5;
            int hitPos = 4;
            int altLabels = 2;
            int description = 3;
            int uri = 0;

            string STR_uri = "uri";
            string STR_hit = "hit";
            string STR_concept = "concept";


            List<object[]> resultList = new List<object[]>();   //saves uri of a hit, name of the hit concept, predicate which lead to the hit, the actual hit and the cummulated relevance
            List<string> uriList = new List<string>();          //saves every uri in order of their appearence - is in sync with resultlist and relevanceList - used for fast position-retrival of an uri
            List<float> relevanceList = new List<float>();      //seperate list for accumulating the relevance of a certain hit - will be added to the result list at the end
            List<string> hitSaver = new List<string>();         //list for savekeeping of distinct hits for all predicates
            Regex regex = new Regex(@"[ ]{2,}");
            searchString = regex.Replace(searchString, @" ");   //delete alle extra whitespaces
            string[] stopWords = StaticHelper.searchStopWords.Replace(" ", "").Replace("\n", "").Split(',');        //get stopword-list from config-file
            

            //remove stop-words
            string[] keywords = searchString.Split(' ');
            int checkForEmptyString = 0;                    //counts empty strings in keywordlist after the were checked against stopword-list
            for (int i = 0; i < keywords.Count(); i++)
            {
                if (stopWords.Contains(keywords[i]))
                {
                    keywords[i] = null;
                    checkForEmptyString++;
                }
            }
            if (checkForEmptyString == keywords.Count())    //if all strings are empty -> return without result
                return;

            //take relevant skos:predicates from settings file, the order of their entry is also of importance
            //the first entry has the highest relevance, prefLabel gets an extra 100% relevance in the course of this algorithm
            string[] predicates = StaticHelper.searchPredicates.Replace(';', ',').Replace(" ", "").Replace("\n", "").Split(',');

            querySelect = "SELECT ?uri (str(?name) as ?concept) ?hit ";

            //asynchronous callbacks make it necessary to synchronize 
            new Thread(() =>
            {
                //algorithm starts here
                foreach (string key in keywords)                            //every given keyword
                {
                    if (key != null)
                    {
                        List<string> zwHitSaver = new List<string>();       //temporary list for saving hits

                        for (int i = 0; i < predicates.Count(); i++)        //every predicate given by the config file
                        {
                            //construct the WHERE-part of the query
                            queryWhere = "WHERE { ?uri skos:prefLabel ?name. ?uri skos:" + predicates[i] + " ?hit." +
                                "FILTER (regex(?hit, \"" + key + "\", \"i\")) FILTER (langMatches( lang(?hit), \"" +
                                languageTag + "\") && langMatches(lang(?hit), \"*\"))" +
                                "FILTER (langMatches( lang(?name), \"" + languageTag + "\")) }";

                            //new special callback-delegate
                            SparqlResultsCallback callthere = new SparqlResultsCallback(callhere);
                            //place a query
                            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere, callthere, null);
                                
                            ////////
                            stopWaitHandle.WaitOne(); //stop thread to wait for callback
                            ///////

                            //take the result of the callback-answer
                            List<string> sameUrisForPredicateList = new List<string>();

                            foreach (SparqlResult res in stopGap.Results)
                            {
                                zwHitSaver.Add((res.Value(STR_hit) as LiteralNode).Value);          //add hits
                                string thisUri = (res.Value(STR_uri) as UriNode).Uri.AbsoluteUri;   //save the uri of the concept where the hit occured
                                sameUrisForPredicateList.Add(thisUri);                              //add uri

                                //count words of the hit(-text) -> many words mean this hit looses in importance
                                int wordCount = (res.Value(STR_hit) as LiteralNode).Value.Count() - (res.Value(STR_hit) as LiteralNode).Value.Replace(" ", "").Count() + 1;
                                //number of hits on the same hit(-text) -> importance of hit increases exponential
                                float numberOfIdentHits = (hitSaver.Where(x => x == (res.Value(STR_hit) as LiteralNode).Value).Count() + 1);
                                //number of hits in same same predicates of one concept (decreases exponential)
                                float numberOfHitsForPredicate = sameUrisForPredicateList.Where(x => x == thisUri).Count();

                                //the value (relevance) added to a specific concept for this predicate and keyword is calculated
                                float addValue = (float)(predicates.Count() - i) * numberOfIdentHits * numberOfIdentHits / (float)(Math.Min(wordCount, 5) * numberOfHitsForPredicate * numberOfHitsForPredicate);

                                //if (predicates[i] == "prefLabel")   //predicate gets an extra factor of 2
                                //    addValue = addValue *2f;
                                if (addValue < 0.1)
                                    addValue = 0.1f;

                                if (uriList.Contains(thisUri))      //add calculated value to the previous value cumulated by this concept
                                {
                                    int pos = uriList.IndexOf(thisUri);
                                    relevanceList[pos] = relevanceList[pos] + addValue;
                                    if (!resultList[pos][hitPos].ToString().Contains((res.Value(STR_hit) as LiteralNode).Value))
                                        resultList[pos][hitPos] = resultList[pos][hitPos] + "; " + (res.Value(STR_hit) as LiteralNode).Value;
                                }
                                else               //or add new concept to result list
                                {
                                    //adding {uri, conceptname, predicate, hitstring, placeholder for hitvalue}
                                    resultList.Add(new object[] { thisUri, (res.Value(STR_concept) as LiteralNode).Value, null, null, predicates[i], (res.Value(STR_hit) as LiteralNode).Value, 0f });
                                    uriList.Add((res.Value(STR_uri) as UriNode).Uri.AbsoluteUri);
                                    relevanceList.Add(addValue);
                                }
                            }
                            hitSaver.AddRange(zwHitSaver.Distinct());       //save all hits of this resultset in hitSaver 
                        }
                    }
                }

                for (int i = 0; i < relevanceList.Count; i++)
                {               //complete resultlist -> add relevance value
                    resultList[i][relevance] = (object)relevanceList[i];
                    SparqlResultsCallback additionalSearchResultInfoCallback = new SparqlResultsCallback(callForAdditinalInfo);
                    string additionalInfoQuery = "SELECT (sql:group_concat(?altLabel , \", \") AS ?altLabels) (STR(?description) as ?description) " +
                        "FROM <" + tagGraph + "> " +
                        "WHERE {OPTIONAL{?uri skos:altLabel ?altLabel. ?uri skos:scopeNote ?description. } " +
                        "FILTER (?uri = <" + resultList[i][uri].ToString() + ">) FILTER (langMatches(lang(?altLabel), \"" + languageTag + "\")) " +
                        "FILTER (langMatches(lang(?description), \"" + languageTag + "\"))} GROUP BY ?uri ?description";

                    object[] addInfoResult = new object[2];
                    endpoint.QueryWithResultSet(additionalInfoQuery, additionalSearchResultInfoCallback, addInfoResult);
                    ////////
                    stopWaitHandle.WaitOne(); //stop thread to wait for callback
                    ///////
                    resultList[i][altLabels] = addInfoResult[0];
                    resultList[i][description] = addInfoResult[1];
                }
                //order result-list by relevance
                resultList = resultList.OrderByDescending(x => float.Parse(x[relevance].ToString())).ToList();
                if (resultList.Count > topKResults)
                    resultList.RemoveRange(topKResults, resultList.Count - topKResults);

                //construct return-tuple
                Tuple<string[], List<object[]>> retTuple = new Tuple<string[], List<object[]>>(new string[] { "Uri", "Concept", "altLabels", "description", "Predicate", "Hit", "Relevance" }, resultList);
                callback.Invoke(retTuple);  //return result
            }).Start();
        }
        /// <summary>
        /// returns all collections a given concept is a member of (transitively!)
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <param name="order">orderdirection of the resultset for conceptnames</param>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param>        
        /// <param name="state">an optional reach-though object</param>
        public void memberOf(SparqlResultsCallback callback, string concept, OrderDirection order, object state)
        {
            querySelect = "SELECT ?uri (str(?name) as ?name) ";
            queryWhere = "WHERE { {SELECT ?s ?uri WHERE { ?uri skos:member ?s } } " +
                "OPTION ( TRANSITIVE, t_distinct, t_in(?s), t_out(?uri),t_min (1)) ." +
                "?uri skos:prefLabel ?name. " +
                queryLanguageFilter + "FILTER ( ?s = <" + concept + "> )} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";

            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, state);

        }
        /// <summary>
        /// returns all collections a given concept is a member of (transitively!)
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param>  
        public void memberOf(SparqlResultsCallback callback, string concept)
        {
            memberOf(callback, concept, OrderDirection.NONE, null);
        }
        /// <summary>
        /// returns all members (collections or concepts) of a given collection
        /// </summary>
        /// <param name="collection">uri of the given collection</param>
        /// <param name="order">orderdirection of the resultset for conceptnames</param>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param>        
        /// <param name="state">an optional reach-though object</param>
        public void getMembersOf(SparqlResultsCallback callback, string collection, OrderDirection order, object state)
        {
            querySelect = "SELECT ?member (str(?name) as ?name)";
            queryWhere = "WHERE {<" + collection + "> skos:member ?member. ?member skos:prefLabel ?name." +
                "FILTER langMatches( lang(?name), \"" + languageTag + "\" ) } ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";

            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, state);

        }
        /// <summary>
        /// returns all members (collections or concepts) of a given collection
        /// </summary>
        /// <param name="collection">uri of the given collection</param>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param> 
        public void getMembersOf(SparqlResultsCallback callback, string collection)
        {
            getMembersOf(callback, collection, OrderDirection.NONE, null);
        }
        /// <summary>
        /// gets all members (transitively, only concepts) of a given collection
        /// </summary>
        /// <param name="collection">uri of the given collection</param>
        /// <param name="order">orderdirection of the resultset for conceptnames</param>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param>        
        /// <param name="state">an optional reach-though object</param>
        public void getTransMembersOf(SparqlResultsCallback callback, string collection, OrderDirection order, object state)
        {
            querySelect = "SELECT ?concept (str(?name) as ?name) ";
            queryWhere = "WHERE { {SELECT ?s ?concept WHERE { ?s skos:member ?concept } } " +
                "OPTION ( TRANSITIVE, t_distinct, t_in(?s), t_out(?concept),t_min (1)) ." +
                "?concept skos:prefLabel ?name. ?concept a skos:Concept. " +
                queryLanguageFilter + "FILTER ( ?s = <" + collection + "> )} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";

            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder, callback, state);

        }
        /// <summary>
        /// gets all members (transitively, only concepts) of a given collection
        /// </summary>
        /// <param name="collection">uri of the given collection</param>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param> 
        public void getTransMembersOf(SparqlResultsCallback callback, string collection)
        {
            getTransMembersOf(callback, collection, OrderDirection.NONE, null);
        }
        /// <summary>
        /// Callbackfunction for temporary search results
        /// </summary>
        /// <param name="result">Sparql-ResultSet from Triple-Store</param>
        /// <param name="state">not needed</param>
        private void callhere(SparqlResultSet result, object state)
        {
            stopGap = null;
            stopGap = result;       //class-object gets result
            stopWaitHandle.Set();   //signal thread of the search-algorithm to continue
        }
        /// <summary>
        /// Callbackfunction for additional search result informations (altLabels + description)
        /// </summary>
        /// <param name="result">Sparql-ResultSet from Triple-Store</param>
        /// <param name="state">not needed</param>
        private void callForAdditinalInfo(SparqlResultSet result, object state)
        {
            if (result.Count>0)
            {
                if (result.Results[0].Value(result.Variables.ElementAt(0)) != null)
                    (state as object[])[0] = result.Results[0].Value(result.Variables.ElementAt(0));       //class-object gets result
                if (result.Results[0].Value(result.Variables.ElementAt(1)) != null)
                    (state as object[])[1] = result.Results[0].Value(result.Variables.ElementAt(1));
            }
            stopWaitHandle.Set();   //signal thread of the search-algorithm to continue
        }
        /// <summary>
        /// returns the concept-uri with the highest concept-number
        /// </summary>
        /// <param name="callback">the callback-delegate</param>
        /// <param name="state">reach-through object</param>
        public void getMaxConceptOfGraph(SparqlResultsCallback callback, object state)
        {
            querySelect = "SELECT DISTINCT (MAX(?uri)) ";
            queryWhere = "WHERE { ?uri a skos:Concept. } ";
            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere , callback, state);
        }
        /// <summary>
        /// special query for creation of 'ReturnTag'-objects, gets the needed properties to create a ReturnTag
        /// </summary>
        /// <param name="conceptUri">the tag-uri</param>
        /// <param name="callback">callback-delegate the triple-store uses to callback with results</param>        
        /// <param name="state">an optional reach-though object</param>
        public void getReturnTag(string conceptUri, SparqlResultsCallback callback, object state)
        {
            querySelect = "SELECT ?uri (str(?name) as ?name)  (sql:group_concat(?altLabel , \", \") AS ?altLabels) (STR(?description) as ?description) ";
            queryWhere = "WHERE {OPTIONAL{?uri skos:altLabel ?altLabel FILTER (langMatches(lang(?altLabel), \"" + languageTag + "\")) ?uri skos:scopeNote ?description FILTER (langMatches(lang(?description), \"" + languageTag + "\"))} " +
                "?uri skos:prefLabel ?name. FILTER (?uri = <" + conceptUri + ">) " +
                       queryLanguageFilter + "} GROUP BY ?uri ?description ?name";
            endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere, callback, state);
        }
    }
}
