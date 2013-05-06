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

namespace TabNavApp.Api
{
    public class Tag
    {
        public string Name { get; set; }
        public string GUID { get; set; }
        public string AltLabels { get; set; }
        public string Description { get; set; }

        public Tag(string name, string guid, string altLabels, string description)
        {
            this.Name = name;
            this.GUID = guid;
            this.AltLabels = altLabels;
            this.Description = description;
        }
    }
}
