using System;
namespace VirtuosoQuery.Silverlight.Entry
{
    public enum OrderDirection
    {
        ASC, DESC, NONE
    }
    /// <summary>
    /// a template for all entry-graph related queries needed
    /// (for documentation: copare to the implementation: EntryGraphQuery)
    /// </summary>
    interface IEntryGraphQuery
    {
        void deleteEntryAndTagsByGuid(string guid);

        void deleteEntryAndTagsByUri(string entryUri);

        void deleteTagByGuid(string docGuid, string skosTagUri);

        void deleteTagByUri(string docUri, string skosTagUri);

        void getEntriesByName(string searchString, VDS.RDF.SparqlResultsCallback callback, object state);

        void getEntriesByTags(System.Collections.Generic.List<string> tagUris, VDS.RDF.SparqlResultsCallback callback, object state);

        void getEntryProperties(string docUri, VDS.RDF.SparqlResultsCallback callback, object state, VirtuosoQuery.Silverlight.Entry.OrderDirection order);

        void getEntryTags(string docGuid, VDS.RDF.SparqlResultsCallback callback, object state, VirtuosoQuery.Silverlight.Entry.OrderDirection order);

        void getEntryUriByGuid(string guid, VDS.RDF.SparqlResultsCallback callback, object state);

        void insertEntries(System.Collections.Generic.List<VirtuosoQuery.ReturnDocument> entries);

        void insertEntry(string guid, string spListID, string name, DateTime created, VirtuosoQuery.Entry entry, System.Collections.Generic.List<string> tags, string author = null, string spServer = "default");

        void insertTagByEntryNr(string docNr, string skosConceptUri);

        void insertTagByEntryUri(string itemUri, string skosConceptUri);
    }
}
