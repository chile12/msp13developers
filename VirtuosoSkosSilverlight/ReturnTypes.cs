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
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections;

namespace VirtuosoQuery
{
    public enum Entry { Generic, Document, Discussion, Survey, Issue, Page, Event }

    /// <summary>
    /// a dynamic member-collection for up to 10 different row-entries e.g. for a DataGrid in Silverlight
    /// </summary>
    public class ReturnRow
    {
        public dynamic val_0 { get; set; }
        public dynamic val_1 { get; set; }
        public dynamic val_2 { get; set; }
        public dynamic val_3 { get; set; }
        public dynamic val_4 { get; set; }
        public dynamic val_5 { get; set; }
        public dynamic val_6 { get; set; }
        public dynamic val_7 { get; set; }
        public dynamic val_8 { get; set; }
        public dynamic val_9 { get; set; }
    }

    public class ReturnTag
    {

        public string uri           { get; set; }
        public string prefLabel     { get; set; }
        public string altLabels     { get; set; }
        public string description   { get; set; }

        public ReturnTag() { }
        public ReturnTag (string uri, string prefLabel, string altLabels = "", string description = "")
        {
            this.uri = uri;
            this.prefLabel = prefLabel;
            this.altLabels = altLabels;
            this.description = description;
        }

        public override string ToString()
        {
            return uri + "\n" + prefLabel + "\n" + altLabels + "\n" + description;
        }
    }

    public class ReturnDocument
    {
        public string Name { get; set; }
        public string Author { get; set; }
        public string CreationDate { get; set; }
        public string UniqueID { get; set; }
        public string ListID { get; set; }
        public Entry ListType { get; set; }
        public string server { get; set; }
        public List<string> Tags { get; set; }

        public string CreationDate_Author
        {
            get { return CreationDate + ", " + Author; }
        }
    }

    public class ReturnTagList : ObservableCollection<ReturnTag>
    {
        public ReturnTagList() :base(){}
    }
}
