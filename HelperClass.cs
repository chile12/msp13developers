using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using VDS.RDF.Query;
using System.Data;
using VDS.RDF;

namespace VirtuosoSkos
{
    public static class HelperClass
    {
        public static DataTable convertSparqlResultToDataTable(SparqlResultSet set){
            DataTable table = new DataTable();
            try
            {
                if (set.Results[0] != null)
                {
                    for (int i = 0; i < set.Variables.Count(); i++)
                        table.Columns.Add(set.Variables.ElementAt(i));

                    foreach (SparqlResult res in set.Results)
                    {
                        List<object> itemList = new List<object>();
                        for (int i = 0; i < set.Variables.Count(); i++)
                        {
                            object zw = res.Value(set.Variables.ElementAt(i));
                            if (zw.GetType() == typeof(LiteralNode))
                                itemList.Add(Convert.ChangeType((zw as LiteralNode).Value, table.Columns[i].DataType));
                            else if (zw.GetType() == typeof(UriNode))
                                itemList.Add(Convert.ChangeType((zw as UriNode).Uri.AbsoluteUri, table.Columns[i].DataType));
                        }
                        table.Rows.Add(itemList.ToArray());
                    }
                }
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            return table;
        }

        public static DataTable convertListStringArrayToDataTable(Tuple<string[], List<object[]>> input)
        {
            DataTable table = new DataTable();
            if (input != null && input.Item1 != null)
            {
                foreach (string col in input.Item1)
                    table.Columns.Add(col);

                foreach (object[] row in input.Item2)
                    table.Rows.Add(row);
            }
            return table;
        }

    }
}
