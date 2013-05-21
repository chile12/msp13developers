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

namespace TabNavApp
{
    public static class Constants
    {
        //public static string endpointAddress {get; set;}        
        //public static string skosGraphUri { get; set; }         
        //public static string docGraphUri { get; set; }          
        public static Brush brush_stickied {get; set;}
        public static Brush brush_default {get; set;}
        
        public static void InitializeDefault()
        {
            brush_default = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
            brush_stickied = new SolidColorBrush(Color.FromArgb(255, 219, 244, 204));


            //endpointAddress = "http://localhost:8890/sparql";
            //skosGraphUri = "http://localhost:8890/UNESCO";
            //docGraphUri = "http://sharepoint.my/SPResourceTags";
        }
    }
}
