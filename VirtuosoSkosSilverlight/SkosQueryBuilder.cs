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
using System.Globalization;
using System.Text.RegularExpressions;

namespace VirtuosoSkosSilverlight
{
    public enum QueryBlocks{
        SELECT, FROM, WHERE
    }

    public static class SkosQueryBuilder
    {
        private const string prefixSTR = "prefix";
        private static string xsd = SkosAndStoreSettings.xmlSchema;
        private static string skosGraph;                           //Graphuri des Ontologiegraphen (http://{Triplestore-IP}:{Port}/{Graphname})
        private static string queryPrefix = "PREFIX skos: <" + SkosAndStoreSettings.skosCore + "> ";
        private static string returnQuery;
        private static string queryFrom;
        private static string queryWhere;
        private static string queryOrder;
        private static string queryLanguageFilter = "FILTER langMatches( lang(?name), \"" + SkosAndStoreSettings.skosLanguage + "\" )";

        private static bool getPrefix(){
            foreach (System.Collections.DictionaryEntry obj in SkosAndStoreSettings.ResourceManager.GetResourceSet(CultureInfo.CurrentCulture, true, false))
            {
                if (obj.Key.ToString().Substring(0, prefixSTR.Length) == prefixSTR)
                {
                    queryPrefix += "PREFIX " + obj.Key.ToString().Substring(prefixSTR.Length) + " <" + obj.Value.ToString() + "> ";
                }
            }
            return true;
        }

        private static Tuple<int, int, int> getSfwPositions(string rawQuery)
        {
            Stack<int> selects = new Stack<int>();
            Stack<int> froms = new Stack<int>();


            int select = 0, from = 0, where = 0;
            int min = 0;

            while (true)
            {
                for (int i = 0; i < 2; i++)
                {
                    select = rawQuery.IndexOf(QueryBlocks.SELECT.ToString(), min);
                    from = rawQuery.IndexOf(QueryBlocks.FROM.ToString(), min);
                    where = rawQuery.IndexOf(QueryBlocks.WHERE.ToString(), min);

                    if (select >= 0) min = select;
                    if (from >= 0)
                    {
                        if (select <0 || from < min) min = from;
                    }
                    if (where >= 0)
                    {
                        if ((select<0 && from <0) || (where < min)) min = where;
                    } 
                }


                    if (select == min && Regex.Match(rawQuery.Substring(select, 10), @"\WSELECT\W", RegexOptions.IgnoreCase) != null)
                    {
                        selects.Push(select);
                        min += QueryBlocks.SELECT.ToString().Length;
                    }

                        else if (from == min && Regex.Match(rawQuery.Substring(from, 10), @"\WFROM\W", RegexOptions.IgnoreCase) != null)
                        {
                            while (froms.Count < selects.Count - 1)
                                froms.Push(-1);
                            froms.Push(from);
                            min += QueryBlocks.FROM.ToString().Length;
                        }
                    

                else if (where == min)
                {
                    if (Regex.Match(rawQuery.Substring(where, 10), @"\WWHERE\W", RegexOptions.IgnoreCase) != null)
                    {
                        if (selects.Count == 1)
                        {
                            if (froms.Count > 0)
                                return new Tuple<int, int, int>(selects.Peek(), froms.Peek(), where);
                            else
                                return new Tuple<int, int, int>(selects.Peek(), -1, where);
                        }
                        min += QueryBlocks.WHERE.ToString().Length;
                    }
                }
            }

            throw new Exception("this query is invalid or of an unsupported format");

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="query">selected query</param>
        /// <param name="order">orderdirecton of the resultset</param>
        /// <param name="uri">uri of the concept/collection or graph which is the pivot point of the specific query</param>
        /// <param name="graph">optional: query is restricted to a specific graph</param>
        /// <param name="paramList">all parameter of this query in order of their appearance (note: by convention extern parameters of a query are more than five characters long)</param>
        /// <returns></returns>
        public static string getQuery(Querys query, OrderDirection order, string graph, string[] paramList)
        {
            getPrefix();
            string returnQuery = queryPrefix;
            string rawQuery = SkosAndStoreSettings.ResourceManager.GetResourceSet(CultureInfo.CurrentCulture, true, false).GetString("Query" + query.ToString());
            List<string> parameter = new List<string>();

            MatchCollection m = Regex.Matches(rawQuery, @"\.*(?<!((as|AS) +))([?|$][A-z]{5,})");     //all letter-character substrings beginning with '?' or '$' which are not following AS or as

            Tuple<int, int, int> sfw = getSfwPositions(rawQuery);
            if (sfw.Item2 > 0)  //from clause exists
            {
                returnQuery += rawQuery.Substring(0, sfw.Item2);
                returnQuery += " ";
                if (graph != null)
                    returnQuery += "FROM <" + graph + ">";
                else
                    returnQuery += rawQuery.Substring(sfw.Item2, sfw.Item3 - sfw.Item2);
            }
            else
            {
                returnQuery += rawQuery.Substring(0, sfw.Item3);
            }
            returnQuery += " ";
            returnQuery += rawQuery.Remove(rawQuery.LastIndexOf('}'), 1).Substring(sfw.Item3) + ". ";

            if (m.Count != paramList.Length)
            {
                throw new Exception("parameter list is not of the right size");
            }
            
            for (int i = 0; i < m.Count; i++)
            {
                returnQuery += " ";
                if (paramList[i].Contains("http"))
                    returnQuery += "FILTER ( " + m[i].Value + " = <" + paramList[i] + "> ) ";
                else
                    returnQuery += "FILTER ( " + m[i].Value + " = " + paramList[i] + " ) ";
            }

            returnQuery += queryLanguageFilter;
            returnQuery += "} ";
            returnQuery += "ORDER BY " + order.ToString().Replace("NONE", "") + "(?name)";

            return returnQuery;
        }
    }
}
