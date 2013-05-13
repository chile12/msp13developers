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

namespace TabNavApp.Api.Documents
{

    public class DocumentUtils
    {
        public static string[] GetIDsFromString(string docs)
        {
            if (docs != null)
            {
                string[] documentIds = docs.Split(new String[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
                string[] result = new string[documentIds.Length];
                for(int i = 0; i < documentIds.Length; i++)
                {
                    result[i] = documentIds[i];
                }
                return result;               
            }
            return null;
        }
    }
}
