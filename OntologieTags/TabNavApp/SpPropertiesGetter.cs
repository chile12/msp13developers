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
using Microsoft.SharePoint.Client;
using System.Collections.Generic;

namespace TabNavApp
{
    public class SpPropertiesGetter
    {
        ClientContext context;
        List<ListItem> documents = new List<ListItem>();

        public SpPropertiesGetter(string[] guids)
        {
            context = ClientContext.Current;
            context.Load(context.Web);

            foreach (var library in context.Web.Lists)
            {
                if (library.BaseType == BaseType.DocumentLibrary)
                {
                    if (!library.Hidden && guids != null && guids.Length > 0)
                    {
                        foreach(string item in guids)
                            documents.Add(library.GetItemById(guids[0]));
                    }
                }
            }
        }

        //public string getDocCreatedDate(string guid)
        //{
        //    string zw = documents[0].FieldValues[
        //}
    }
}
