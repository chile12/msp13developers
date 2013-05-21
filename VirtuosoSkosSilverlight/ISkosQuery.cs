using System;
namespace VirtuosoQuery.Silverlight.Skos
{
    /// <summary>
    /// orderdirections
    /// </summary>
    public enum OrderDirection
    {
        ASC, DESC, NONE
    }
    /// <summary>
    /// deprecated
    /// </summary>
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
    /// (for documentation: compare to implementation: VirtuosoSkosQuery)
    /// </summary>
    interface ISkosQuery
    {
        void broader(VDS.RDF.SparqlResultsCallback callback, string concept, VirtuosoQuery.Silverlight.Skos.OrderDirection order, object state);

        void broaderTransitive(VDS.RDF.SparqlResultsCallback callback, string concept, int maxDist, VirtuosoQuery.Silverlight.Skos.OrderDirection order, object state);

        void getMembersOf(VDS.RDF.SparqlResultsCallback callback, string collection, VirtuosoQuery.Silverlight.Skos.OrderDirection order, object state);

        void getReturnTag(string conceptUri, VDS.RDF.SparqlResultsCallback callback, object state);

        void getTransMembersOf(VDS.RDF.SparqlResultsCallback callback, string collection, VirtuosoQuery.Silverlight.Skos.OrderDirection order, object state);

        void memberOf(VDS.RDF.SparqlResultsCallback callback, string concept, VirtuosoQuery.Silverlight.Skos.OrderDirection order, object state);

        void narrower(VDS.RDF.SparqlResultsCallback callback, string concept, VirtuosoQuery.Silverlight.Skos.OrderDirection order, object state);

        void narrowerTransitive(VDS.RDF.SparqlResultsCallback callback, string concept, int maxDist, VirtuosoQuery.Silverlight.Skos.OrderDirection order, object state);

        void propertiesOf(VDS.RDF.SparqlResultsCallback callback, string concept, VirtuosoQuery.Silverlight.Skos.OrderDirection order, object state);

        void relatedTo(VDS.RDF.SparqlResultsCallback callback, string concept, VirtuosoQuery.Silverlight.Skos.OrderDirection order, object state);

        void searchTag(VirtuosoQuery.Silverlight.Skos.VirtuosoSkosQuery.SearchResultCallback callback, string searchString, int topKResults);

        void siblings(VDS.RDF.SparqlResultsCallback callback, string concept, VirtuosoQuery.Silverlight.Skos.OrderDirection order, object state);

        void topGraphConcepts(VDS.RDF.SparqlResultsCallback callback, string graphUri, VirtuosoQuery.Silverlight.Skos.OrderDirection order, object state);

    }
}
