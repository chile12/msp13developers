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
using TabNavApp.Api.Common;
using VirtuosoQuery;

namespace TabNavApp.Api.Documents
{
    /// <summary>
    /// the data object behind every DocumentListItem
    /// </summary>
    public class Document : Item
    {
        public string Name { get; set; }
        public string Author { get; set; }
        public string CreationDate { get; set; }
        public string UniqueID { get; set; }
        public string ListID { get; set; }
        public Entry ListType { get; set; }
        public string server { get; set; }
        public Dictionary<string, string> Tags { get; set; }

        public string CreationDate_Author
        {
            get { return CreationDate + ", " + Author; }
        }
    }

}
