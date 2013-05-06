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
    public class Document
    {
        public string Name { get; set; }
        public string Author { get; set; }
        public string CreationDate { get; set; }
        public string GUID { get; set; }

        public string CreationDate_Author
        {
            get { return CreationDate + ", " + Author; }
        }
    }

}
