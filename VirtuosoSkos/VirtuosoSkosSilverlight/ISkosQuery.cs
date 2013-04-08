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
using System.Linq;
using System.Text;
using VDS.RDF.Query;
using System.Collections.Generic;
using VDS.RDF;

namespace VirtuosoSkosSilverlight
{
    public enum OrderDirection
    {
        ASC, DESC, NONE
    }
    public enum Querys
    {
        Broader,
        Narrower,
        BroaderTransitve,
        NarrowerTransitive,
        BroaderSiblings,
        NarrowerSiblings,
        RelatedTo,
        PropertiesOf,
        TopGraphConcepts,
        SearchConcepts,
        MemberOf,
        GetMemberOf,
        GetTransitiveMemberOf
    }
    /// <summary>
    /// a template for creating triplestore-specific implementations of SPARQL-related data-acquisition-methodes
    /// this interface might be used for endpoint-communication or memory-based triplestores
    /// </summary>
    interface ISkosQuery
    {

        /// <summary>
        /// used for graph-navigation or visualization 
        /// returns all concepts which are skos:narrower to a given concept
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <param name="order">orderdirection of the resultset for conceptnames</param>
        /// <returns>all narrower concepts to 'concept' as SparqlResultSet -- should at least return concept-uris and names</returns>
        void narrower(SparqlResultsCallback callback, string concept, OrderDirection order, object state);
        /// <summary>
        /// used for graph-navigation or visualization 
        /// returns all concepts which are skos:narrower to a given concept -- without an ordered resultset
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <returns>all narrower concepts to 'concept' as SparqlResultSet -- should at least return concept-uris and names</returns>
        void narrower(SparqlResultsCallback callback, string concept);
        /// <summary>
        /// used for graph-navigation or visualization 
        /// returns all concepts which are skos:broader to a given concept
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <param name="order">orderdirection of the resultset for conceptnames</param>
        /// <returns>all broader concepts to 'concept' as SparqlResultSet -- should at least return concept-uris and names</returns>
        void broader(SparqlResultsCallback callback, string concept, OrderDirection order, object state);
        /// <summary>
        /// used for graph-navigation or visualization 
        /// returns all concepts which are skos:broader to a given concept -- without an ordered resultset
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <returns>all broader concepts to 'concept' as SparqlResultSet -- should at least return concept-uris and names</returns>
        void broader(SparqlResultsCallback callback, string concept);
        /// <summary>
        /// used for graph-navigation or visualization 
        /// returns all concepts which are transitively skos:broader to a given concept
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <param name="order">orderdirection of the resultset for conceptnames</param>
        /// <returns>all transitively broader concepts to 'concept' as SparqlResultSet -- should at least return concept-uris, concept names, and distance from the origin concept</returns>
        void broaderTransitive(SparqlResultsCallback callback, string concept, int maxDist, OrderDirection order, object state);
        /// <summary>
        /// used for graph-navigation or visualization 
        /// returns all concepts which are transitively skos:broader to a given concept -- without an ordered resultset
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <returns>all transitively broader concepts to 'concept' as SparqlResultSet -- should at least return concept-uris, concept names, and distance from the origin concept</returns>
        void broaderTransitive(SparqlResultsCallback callback, string concept, int maxDist);
        /// <summary>
        /// used for graph-navigation or visualization 
        /// returns all concepts which are transitively skos:narrower to a given concept
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <param name="order">orderdirection of the resultset for conceptnames</param>
        /// <returns>all transitively narrower concepts to 'concept' as SparqlResultSet -- should at least return concept-uris, concept names, and distance from the origin concept</returns>
        void narrowerTransitive(SparqlResultsCallback callback, string concept, int maxDist, OrderDirection order, object state);
        /// <summary>
        /// used for graph-navigation or visualization 
        /// returns all concepts which are transitively skos:narrower to a given concept -- without an ordered resultset
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <returns>all transitively narrower concepts to 'concept' as SparqlResultSet -- should at least return concept-uris, concept names, and distance from the origin concept</returns>
        void narrowerTransitive(SparqlResultsCallback callback, string concept, int maxDist);
        /// <summary>
        /// used for graph-navigation or visualization 
        /// returns all concepts which have at least one narrower/broader concepts of the narrower/broader concepts of a given concept
        /// therefore those concepts are on a similar level in the hierarchy pertaining to a related context
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <param name="order">orderdirection of the resultset for conceptnames</param>
        /// <returns>sibling- uris, names as SparqlResultSet</returns>
        void siblings(SparqlResultsCallback callback, string concept, OrderDirection order, object state);
        /// <summary>
        /// used for graph-navigation or visualization 
        /// returns all concepts which have at least one narrower/broader concepts of the narrower/broader concepts of a given concept
        /// therefore those concepts are on a similar level in the hierarchy pertaining to a related context
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <returns>sibling- uris, names as SparqlResultSet</returns>
        void siblings(SparqlResultsCallback callback, string concept);
        /// <summary>
        /// used for graph-navigation or visualization 
        /// returns all concepts which are skos:related to a given concept
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <param name="order">orderdirection of the resultset for conceptnames</param>
        /// <returns>related concept uris and names as SparqlResultSet</returns>
        void relatedTo(SparqlResultsCallback callback, string concept, OrderDirection order, object state);
        /// <summary>
        /// used for graph-navigation or visualization 
        /// returns all concepts which are skos:related to a given concept
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <returns>related concept uris and names as SparqlResultSet</returns>
        void relatedTo(SparqlResultsCallback callback, string concept);
        /// <summary>
        /// returns all properties & values which have nor uri-values and are therefor not used for graph-navigation
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <param name="order">orderdirection of the resultset for properties</param>
        /// <returns>all specified properties with their value for the given concept</returns>
        void propertiesOf(SparqlResultsCallback callback, string concept, OrderDirection order, object state);
        /// <summary>
        /// returns all properties & values which have nor uri-values and are therefor not used for graph-navigation
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <returns>all specified properties with their value for the given concept</returns>
        void propertiesOf(SparqlResultsCallback callback, string concept);
        /// <summary>
        /// returns all topconcepts of a given graph
        /// </summary>
        /// <param name="graphUri">the graph-uri as string</param>
        /// <param name="order">orderdirection of the resultset for conceptnames</param>
        /// <returns>topconcepts</returns>
        void topGraphConcepts(SparqlResultsCallback callback, string graphUri, OrderDirection order, object state);
        /// <summary>
        /// returns all topconcepts of the default graph
        /// </summary>
        /// <param name="order">orderdirection of the resultset for conceptnames</param>
        /// <returns>topconcepts</returns>
        void topGraphConcepts(SparqlResultsCallback callback, OrderDirection order);
        /// <summary>
        /// returns all topconcepts of the default graph
        /// </summary>
        /// <returns>topconcepts</returns>
        void topGraphConcepts(SparqlResultsCallback callback);
        /// <summary>
        /// searches for all words (-stopwords) of a searchstring in all LiteralNodes linked with skos:predicates 
        /// of given predicatelist (is provided via config-file) and weights all 'hits' for their relevance
        /// </summary>
        /// <param name="searchString">the searchstring</param>
        /// <param name="topKResults">number of results to be returned</param>
        /// <returns>returns topK results ordered by their weight (relevance) 
        /// List<string[]>: the resultlist (columns: uri, name, hit-predicate, weight)
        /// List<string>: columnheadlist: for constructing a DataTable with columnheads</returns>
        void searchTag(VirtuosoSkosSilverlight.VirtuosoSkosQuery.SearchResultCallback callback, string searchString, int topKResults);
        /// <summary>
        /// returns all collections a given concept is a member of (transitively!)
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <param name="order">orderdirection of the resultset for conceptnames</param>
        /// <returns>collections (uri, name) as SparqlResultSet</returns>
        void memberOf(SparqlResultsCallback callback, string concept, OrderDirection order, object state);
        /// <summary>
        /// returns all collections a given concept is a member of (transitively!)
        /// </summary>
        /// <param name="concept">concept-uri as string of the central concept</param>
        /// <returns>collections (uri, name) as SparqlResultSet</returns>
        void memberOf(SparqlResultsCallback callback, string concept);
        /// <summary>
        /// returns all members (collections or concepts) of a given collection
        /// </summary>
        /// <param name="collection">uri of the given collection</param>
        /// <param name="order">orderdirection of the resultset for conceptnames</param>
        /// <returns>memberlist (uri, name) as SparqlResultSet</returns>
        void getMembersOf(SparqlResultsCallback callback, string collection, OrderDirection order, object state);
        /// <summary>
        /// returns all members (collections or concepts) of a given collection
        /// </summary>
        /// <param name="collection">uri of the given collection</param>
        /// <returns>memberlist (uri, name) as SparqlResultSet</returns>
        void getMembersOf(SparqlResultsCallback callback, string collection);
        /// <summary>
        /// gets all members (transitively, only concepts) of a given collection
        /// </summary>
        /// <param name="collection">uri of the given collection</param>
        /// <param name="order">orderdirection of the resultset for conceptnames</param>
        /// <returns>memberlist (uri, name) as SparqlResultSet</returns>
        void getTransMembersOf(SparqlResultsCallback callback, string collection, OrderDirection order, object state);
        /// <summary>
        /// gets all members (transitively, only concepts) of a given collection
        /// </summary>
        /// <param name="collection">uri of the given collection</param>
        /// <returns>memberlist (uri, name) as SparqlResultSet</returns>
        void getTransMembersOf(SparqlResultsCallback callback, string collection);
    }
}
