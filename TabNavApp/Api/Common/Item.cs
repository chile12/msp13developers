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

namespace TabNavApp.Api.Common
{
    /// <summary>
    /// abstract class for all List-Items used in the ListBoxes
    /// </summary>
    public abstract class Item
    {
        public UserControl Control { get; set; }        //the corresponding ListItem this Item is DataBoundObject of
        public Brush Background { get; set; }           //the backgroundcolor of the corresponding ListItem
    }
}
