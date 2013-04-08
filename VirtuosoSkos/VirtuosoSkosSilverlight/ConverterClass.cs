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
using VDS.RDF.Query;
using VDS.RDF;

namespace VirtuosoSkosSilverlight
{
    public static class ConverterClass
    {
        private const int ReturnRowPropertyCount = 10;

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

    }
}
