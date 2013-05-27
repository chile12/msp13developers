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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="properties"></param>
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="properties"></param>
        public override void ItemAdded(SPItemEventProperties properties)
        {
            if (properties.Context != null)
            {
                try
                {
                    string config = SPUtility.GetGenericSetupPath(@"TEMPLATE\LAYOUTS\SkosTagFeatures\FeatureConfig.xml");
                    VirtuosoGraphUpdate virtuosoUpdate = new VirtuosoGraphUpdate(config);                           //query-object - needs url of config-file
                    
                    SPListItem item = properties.ListItem;                                                          //retrive item
                    string guid = properties.ListItem.UniqueId.ToString().Replace("{", "").Replace("}", "");        //guid of list item
                    string listId = properties.ListId.ToString().Replace("{", "").Replace("}", "");                 //guid of list
                    Entry entryType = (Entry)Enum.Parse(typeof(Entry), item.ContentType.ResourceFolder.Name);       //retrieving the non-localized SPContentType.Name from ContentType.ResourceFolder.Name -> may need to be revisited
                    string userName = properties.UserLoginName.Replace("\\", "");                                   //username (aka author)
                    string server = properties.Web.Url + properties.Web.ServerRelativeUrl;                          //construct server url
                    
                    virtuosoUpdate.insertEntry(guid, listId, item.Name, DateTime.Now, entryType, userName, server); //send query
                }
                catch (Exception)
                {
                    return;
                }
            }
        }
    }
}
