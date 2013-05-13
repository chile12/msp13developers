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

namespace TabNavApp
{

    public class TabNavListBox : ListBox
    {
        public List<DependencyObject> Childs { get; set; }

        public TabNavListBox()
        {
            Childs = new List<DependencyObject>();
        }

        public DependencyObject GetUIElementByItemIndex(int itemIndex)
        {
            return Childs[itemIndex];
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            DependencyObject dObject = base.GetContainerForItemOverride();
            Childs.Add(dObject);
            return (dObject);
        }
    }
}
