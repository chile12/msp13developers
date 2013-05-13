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
    public abstract class Item
    {
        public UserControl Control { get; set; }
        public Brush Background { get; set; }
    }
}
