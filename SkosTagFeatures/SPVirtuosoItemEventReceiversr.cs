using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;
using VirtuosoDocGraphUpdate;
using Microsoft.SharePoint.Utilities;

namespace TagButton
{
    class SPVirtuosoItemEventReceivers : SPItemEventReceiver
    {
        Guid featureGuid = new Guid("b85a4771-b84c-4240-bfb9-94dc108c6a17");
        
        public override void ItemDeleting(SPItemEventProperties properties)
        {
            if (properties.Context != null)
            {
                try
                {
                    string config = SPUtility.GetGenericSetupPath(@"TEMPLATE\LAYOUTS\SkosTagFeatures\FeatureConfig.xml");
                    VirtuosoGraphUpdate virtuosoUpdate = new VirtuosoGraphUpdate(config);
                    string guid = properties.ListItem.UniqueId.ToString().Replace("{", "").Replace("}", "");
                    virtuosoUpdate.deleteEntryAndTagsByGuid(guid);
                }
                catch { return; }
            }
        }

        public override void ItemAdded(SPItemEventProperties properties)
        {
            if (properties.Context != null)
            {
                try
                {
                    string config = SPUtility.GetGenericSetupPath(@"TEMPLATE\LAYOUTS\SkosTagFeatures\FeatureConfig.xml");
                    VirtuosoGraphUpdate virtuosoUpdate = new VirtuosoGraphUpdate(config);
                    string guid = properties.ListItem.UniqueId.ToString().Replace("{", "").Replace("}", "");
                    
                        SPListItem item = properties.ListItem;
                        virtuosoUpdate.insertEntry(guid, properties.ListId.ToString().Replace("{", "").Replace("}", ""), item.Name, DateTime.Now,
                            Entry.Document, properties.UserLoginName.Replace("\\", ""), properties.Web.Url + properties.Web.ServerRelativeUrl);
                    
                }
                catch (Exception)
                {
                    return;
                }
            }
        }

        //public override void ItemUpdating(SPItemEventProperties properties)
        //{
        //    if (properties.Context != null)
        //    {
        //        try
        //        {
        //            string featureRoot = properties.Web.Features[featureGuid].Definition.RootDirectory;
        //            VirtuosoGraphUpdate virtuosoUpdate = new VirtuosoGraphUpdate(featureRoot);
        //            SPListItem item = properties.ListItem;
        //            virtuosoUpdate.updateListIdByGuid(properties.ListItem.UniqueId.ToString().Replace("{", "").Replace("}", ""), 
        //                properties.AfterProperties["ListId"].ToString().Replace("{", "").Replace("}", ""));
        //        }
        //        catch { return; }
        //    }
        //}
    }
}
