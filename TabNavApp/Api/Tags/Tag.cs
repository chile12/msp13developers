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
using TabNavApp.Api.Common;


namespace TabNavApp.Api.Tags
{
    /// <summary>
    /// the data object behind every tagListItem
    /// </summary>
    public class Tag : Item
    {
        public string Uri { get; set; }
        public string Name { get; set; }
        public string AltLabels { get; set; }
        public string Description { get; set; }

        //public Tag(string name, string uri, string altLabels, string description)
        //{
        //    this.Name = name;
        //    this.Uri = uri;
        //    this.AltLabels = altLabels;
        //    this.Description = description;
        //}
    }
}
