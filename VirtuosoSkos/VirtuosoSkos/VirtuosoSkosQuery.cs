using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using VDS.RDF.Query;
using VDS.RDF;
using System.Linq;
using System.Data;
using System.Text.RegularExpressions;

namespace VirtuosoSkos
{
    public class VirtuosoSkosQuery : ISkosQuery
    {
        private const string prefixSTR = "prefix";
        private string xsd = SkosAndStoreSettings.Default.xmlSchema;
        private string skos = SkosAndStoreSettings.Default.skosCore;
        public SparqlRemoteEndpoint endpoint;

        private Dictionary<string, string> prefixes = new Dictionary<string,string>();
        private Uri skosGraph;                               //Graphuri des Ontologiegraphen (http://{Triplestore-IP}:{Port}/{Graphname})
        private string languageTag;

        private string queryPrefix;
        private string querySelect;
        private string queryFrom;
        private string queryWhere;
        private string queryOrder;
        private string queryLanguageFilter;

        public VirtuosoSkosQuery(SparqlRemoteEndpoint endpoint)
        {
            this.endpoint = endpoint;
            SkosAndStoreSettings.Default.Reload();
            foreach (SettingsProperty prop in SkosAndStoreSettings.Default.Properties){
                if(prop.Name.Substring(0,prefixSTR.Length) == prefixSTR){
                    prefixes.Add(prop.Name.Substring(prefixSTR.Length), prop.DefaultValue.ToString());
                }
            }

            this.skosGraph = UriFactory.Create(endpoint.DefaultGraphs[0]);
            this.languageTag = SkosAndStoreSettings.Default.skosLanguage;

            queryPrefix = "PREFIX skos: <" + skos + "> ";
            querySelect = "SELECT DISTINCT ?concept (str(?name) AS ?conceptname) ";
            queryFrom = "FROM <" + skosGraph.AbsoluteUri + "> ";
            queryLanguageFilter = "FILTER langMatches( lang(?name), \"" + languageTag + "\" )";
        }

        /// <summary>
        /// gets all concepturis of predicate 'skos#narrower' from a given concept
        /// uses SPARQL 1.1 concepts
        /// </summary>
        /// <param name="concept">concepturi or abrivation</param>
        /// <returns></returns>
        public SparqlResultSet narrower(string concept, OrderDirection order)
        {
            queryWhere = "WHERE { <" + concept + "> <" + skos + "narrower> " + " ?concept. ?concept <"
                + skos + "prefLabel> ?name " + queryLanguageFilter + "} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE","") + "(?name)";

            SparqlResultSet set = endpoint.QueryWithResultSet(querySelect + queryFrom + queryWhere + queryOrder);

            return set;
        }
        public SparqlResultSet narrower(string concept)
        {
            return narrower(concept, OrderDirection.NONE);
        }

        public SparqlResultSet broader(string concept, OrderDirection order)
        {
            queryWhere = "WHERE { <" + concept + "> <" + skos + "broader> " + " ?concept. ?concept <"
                + skos + "prefLabel> ?name FILTER langMatches( lang(?name), \"" + languageTag + "\" )} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";

            SparqlResultSet set = endpoint.QueryWithResultSet(querySelect + queryFrom + queryWhere + queryOrder);

            return set;
        }
        public SparqlResultSet broader(string concept)
        {
            return broader(concept, OrderDirection.NONE);
        }

        public SparqlResultSet broaderTransitive(string concept, int maxDist, OrderDirection order)
        {
            querySelect = "SELECT DISTINCT ?concept ?name ?distance ";
            queryWhere = "WHERE { { SELECT ?in ?concept  WHERE { ?in skos:broader ?concept } }" +
                "OPTION ( TRANSITIVE, t_distinct, t_in(?in), t_out(?concept), t_min (1), t_max(" + maxDist + "), t_step ('step_no') as ?distance ). " +
                "?concept skos:prefLabel ?name. " + queryLanguageFilter + ". FILTER ( ?in = <" + concept + "> ) }";
            queryOrder = "ORDER BY ?distance " + order.ToString().Replace("NONE", "") + "(?name)";

            SparqlResultSet set = endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder);

            return set;
        }
        public SparqlResultSet broaderTransitive(string concept, int maxDist)
        {
            return broaderTransitive(concept, maxDist, OrderDirection.NONE);
        }

        public SparqlResultSet narrowerTransitive(string concept, int maxDist, OrderDirection order)
        {
            querySelect = "SELECT DISTINCT ?concept ?name ?distance ";
            queryWhere = "WHERE { { SELECT ?in ?concept  WHERE { ?in skos:narrower ?concept } }" +
                "OPTION ( TRANSITIVE, t_distinct, t_in(?in), t_out(?concept), t_min (1), t_max(" + maxDist + "), t_step ('step_no') as ?distance ). " +
                "?concept skos:prefLabel ?name. " + queryLanguageFilter + ". FILTER ( ?in = <" + concept + "> ) }";
            queryOrder = "ORDER BY ?distance " + order.ToString().Replace("NONE", "") + "(?name)";

            SparqlResultSet set = endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder);

            return set;
        }
        public SparqlResultSet narrowerTransitive(string concept, int maxDist)
        {
            return narrowerTransitive(concept, maxDist, OrderDirection.NONE);
        }

        public SparqlResultSet siblings(string concept, OrderDirection order)
        {
            queryWhere = "WHERE { {<" + concept + "> skos:broader  ?z. ?z skos:narrower ?concept.} UNION {<" + concept + "> skos:narrower ?y. ?y skos:broader ?concept}. " +
                                    "?concept skos:prefLabel ?name. " +
                                    "FILTER langMatches( lang(?name), \"" + languageTag + 
                                    "\" ) FILTER (?concept != <" + concept + ">)} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";

            SparqlResultSet set = endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder);
            
            return set;
        }
        public SparqlResultSet siblings(string concept)
        {
            return siblings(concept, OrderDirection.NONE);
        }

        public SparqlResultSet relatedTo(string concept, OrderDirection order)
        {
            queryWhere = "WHERE { <" + concept + "> skos:related  ?concept." +
                                    "?concept skos:prefLabel ?name FILTER langMatches( lang(?name), \"" + languageTag + "\" )} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";

            SparqlResultSet set = endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder);

            return set;
        }
        public SparqlResultSet relatedTo(string concept)
        {
            return relatedTo(concept, OrderDirection.NONE);
        }

        public SparqlResultSet propertiesOf(string concept, OrderDirection order)
        {
            querySelect = "SELECT DISTINCT ?pred ?obj ";
            queryWhere = "WHERE {<" + concept + "> ?pred ?obj " + 
                "FILTER (?pred != skos:broader && ?pred != skos:narrower && " + 
                "?pred != skos:broaderTransitive && ?pred != skos:narrowerTransitive &&" + 
                "?pred != skos:related && ?pred != skos:prefLabel)" + 
                "FILTER (langMatches( lang(?obj), \"" + languageTag + "\" ) || !(langMatches( lang(?obj), \"*\" )))}";

            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";

            SparqlResultSet set = endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder);
            return set;
        }
        public SparqlResultSet propertiesOf(string concept)
        {
            return propertiesOf(concept, OrderDirection.NONE);
        }

        public SparqlResultSet topGraphConcepts(string graphUri, OrderDirection order)
        {
            querySelect = "SELECT DISTINCT ?topConcept ?conceptName ";
            string from = "FROM <" + graphUri + "> ";
            queryWhere = "WHERE { ?topConcept skos:topConceptOf ?x. " +
                "?topConcept skos:prefLabel ?conceptName. FILTER langMatches( lang(?conceptName), \"" + languageTag + "\" )} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?conceptName)";

            SparqlResultSet set = endpoint.QueryWithResultSet(queryPrefix + querySelect + from + queryWhere + queryOrder);
            return set;
        }
        public SparqlResultSet topGraphConcepts(OrderDirection order)
        {
            return topGraphConcepts(skosGraph.AbsoluteUri, order);
        }
        public SparqlResultSet topGraphConcepts()
        {
            return topGraphConcepts(skosGraph.AbsoluteUri, OrderDirection.NONE);
        }

        public Tuple<string[], List<object[]>> searchTag(string searchString, int topKResults)
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
                return null;
            
            //take relevant skos:predicates from settings file, the order of their entry is also of importance
            //the first entry has the highest relevance, prefLabel gets an extra 50% relevance in the course of this algorithm
            string[] predicates = SkosAndStoreSettings.Default.searchPredicates.Replace(';', ',').Replace(" ","").Split(',');

            querySelect = "SELECT ?uri (str(?name) as ?concept) ?hit ";

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

                        SparqlResultSet set = endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere);

                        List<string> sameUrisForPredicateList = new List<string>();

                        foreach (SparqlResult res in set.Results)
                        {
                            zwHitSaver.Add((res.Value(STR_hit) as LiteralNode).Value);
                            string thisUri = (res.Value(STR_uri) as UriNode).Uri.AbsoluteUri;
                            sameUrisForPredicateList.Add(thisUri);

                            int wordCount = (res.Value(STR_hit) as LiteralNode).Value.Count() - (res.Value(STR_hit) as LiteralNode).Value.Replace(" ", "").Count() + 1;
                            float numberOfIdentHits = (hitSaver.Where(x => x == (res.Value(STR_hit) as LiteralNode).Value).Count() + 1);
                            float numberOfHitsForPredicate = sameUrisForPredicateList.Where(x => x == thisUri).Count();

                            float addValue = (float)(predicates.Count() - i) * numberOfIdentHits * numberOfIdentHits / (float)(Math.Min(wordCount, 5) * numberOfHitsForPredicate * numberOfHitsForPredicate);


                            //prefLabel gets a 100% boost (yes, hardcoded!)
                            if (predicates[i] == "prefLabel")
                                addValue = addValue * 2f;
                            if (addValue < 0.1)
                                addValue = 0.1f;

                            if (uriList.Contains(thisUri))
                            {
                                int pos = uriList.IndexOf(thisUri);
                                relevanceList[pos] = relevanceList[pos] + addValue;
                                if(!resultList[pos][hitPos].ToString().Contains((res.Value(STR_hit) as LiteralNode).Value))
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
            for(int i=0;i<relevanceList.Count;i++)
                resultList[i][relevance] = (object)relevanceList[i];

            resultList = resultList.OrderByDescending(x => float.Parse(x[relevance].ToString())).ToList();
            if(resultList.Count>topKResults)
                resultList.RemoveRange(topKResults, resultList.Count-topKResults);

            Tuple<string[], List<object[]>> retTuple = new Tuple<string[], List<object[]>>(new string[] { "Uri", "Concept", "Predicate", "Hit", "Relevance" }, resultList);
            return retTuple;
        }

        public SparqlResultSet memberOf(string concept, OrderDirection order)
        {
            querySelect = "SELECT ?concept (str(?name) as ?name) ";
            queryWhere = "WHERE { {SELECT ?s ?concept WHERE { ?concept skos:member ?s } } " +
                "OPTION ( TRANSITIVE, t_distinct, t_in(?s), t_out(?concept),t_min (1)) ." +
                "?concept skos:prefLabel ?name. " +
                queryLanguageFilter + "FILTER ( ?s = <" + concept + "> )} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";

            SparqlResultSet set = endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder);

            return set;
        }
        public SparqlResultSet memberOf(string concept)
        {
            return memberOf(concept, OrderDirection.NONE);
        }

        public SparqlResultSet getMembersOf(string collection, OrderDirection order)
        {
            querySelect = "SELECT ?member (str(?name) as ?name)";
            queryWhere = "WHERE {<" + collection + "> skos:member ?member. ?member skos:prefLabel ?name." +
                "FILTER langMatches( lang(?name), \"" + languageTag + "\" ) } ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";

            SparqlResultSet set = endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder);

            return set;
        }
        public SparqlResultSet getMembersOf(string collection)
        {
            return getMembersOf(collection, OrderDirection.NONE);
        }

        public SparqlResultSet getTransMembersOf(string collection, OrderDirection order)
        {
            querySelect = "SELECT ?concept (str(?name) as ?name) ";
            queryWhere = "WHERE { {SELECT ?s ?concept WHERE { ?s skos:member ?concept } } " + 
                "OPTION ( TRANSITIVE, t_distinct, t_in(?s), t_out(?concept),t_min (1)) ." + 
                "?concept skos:prefLabel ?name. ?concept a skos:Concept. " +
                queryLanguageFilter + "FILTER ( ?s = <" + collection + "> )} ";
            queryOrder = "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";

            SparqlResultSet set = endpoint.QueryWithResultSet(queryPrefix + querySelect + queryFrom + queryWhere + queryOrder);

            return set;
        }
        public SparqlResultSet getTransMembersOf(string collection)
        {
            return getTransMembersOf(collection, OrderDirection.NONE);
        }
    }
}
