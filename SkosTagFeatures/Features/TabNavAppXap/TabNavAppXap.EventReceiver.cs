using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Security;
using System.Reflection;

namespace TagButton.Features.TabNavAppXap
{
    /// <summary>
    /// This class handles events raised during feature activation, deactivation, installation, uninstallation, and upgrade.
    /// </summary>
    /// <remarks>
    /// The GUID attached to this class may be used during packaging and should not be modified.
    /// </remarks>

    [Guid("c6ea5be9-bea5-4eec-af10-c5f57aad6fb9")]
    public class TabNavAppXapEventReceiver : SPFeatureReceiver
    {
            private string STR_publicToken = Assembly.GetExecutingAssembly().GetName().GetPublicKeyToken().ToString();

            // Uncomment the method below to handle the event raised after a feature has been activated.
            private SPContentType fetchContentType(SPContentTypeCollection contentTypeCollection, string ID)
            {
                SPContentType publContentType = null;
                foreach (SPContentType contentType in contentTypeCollection)
                {

                    //if (contentType.Id.Equals(ID))
                    if (string.Equals(contentType.Id.ToString(), ID, StringComparison.InvariantCultureIgnoreCase))
                    {
                        publContentType = contentType;
                        break;
                    }
                }
                return publContentType;
            }

            public override void FeatureActivated(SPFeatureReceiverProperties properties)
            {
                SPWeb myweb = (properties.Feature.Parent as SPWeb);
                SPSite sitecollection = myweb.Site;
                string assVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                string assName = Assembly.GetExecutingAssembly().GetName().Name;
                try
                {
                    foreach (SPWeb web in sitecollection.AllWebs)
                    {
                        foreach (SPList list in web.Lists)
                        {
                            if (!list.Hidden) //not!
                            {
                                if (list.EventReceivers.Count > 0)
                                {
                                    for (int i = list.EventReceivers.Count - 1; i >= 0; i--)
                                    {
                                        if (list.EventReceivers[i].Assembly.Contains(STR_publicToken))
                                            list.EventReceivers[i].Delete();
                                    }
                                }
                                list.EventReceivers.Add(SPEventReceiverType.ItemDeleting,
                                       assName + ", Version=" + assVersion + ", Culture=neutral, PublicKeyToken=" + STR_publicToken,
                                       assName + ".SPVirtuosoItemEventReceivers");

                                list.EventReceivers.Add(SPEventReceiverType.ItemAdded,
                                       assName + ", Version=" + assVersion + ", Culture=neutral, PublicKeyToken=" + STR_publicToken,
                                       assName + ".SPVirtuosoItemEventReceivers");

                                list.EventReceivers.Add(SPEventReceiverType.ItemUpdating,
                                   assName + ", Version=" + assVersion + ", Culture=neutral, PublicKeyToken=" + STR_publicToken,
                                   assName + ".SPVirtuosoItemEventReceivers");
                            }
                        }
                    }
                    myweb.Update();
                }
                catch (Exception ex)
                {
                    //throw new Exception(STR_publicToken + assName + assVersion);
                    return;

                }

                finally
                {

                    sitecollection.Dispose();
                    myweb.Dispose();
                }
            }


            // Uncomment the method below to handle the event raised before a feature is deactivated.

            public override void FeatureDeactivating(SPFeatureReceiverProperties properties)
            {
                SPWeb myweb = (properties.Feature.Parent as SPWeb);
                SPSite sitecollection = myweb.Site;
                try
                {
                    foreach (SPWeb web in sitecollection.AllWebs)
                    {
                        foreach (SPList list in web.Lists)
                        {
                            if (!list.Hidden) //not!
                            {
                                if (list.EventReceivers.Count > 0)
                                {
                                    for (int i = list.EventReceivers.Count - 1; i >= 0; i--)
                                    {
                                        if (list.EventReceivers[i].Assembly.Contains(STR_publicToken))
                                            list.EventReceivers[i].Delete();
                                    }
                                }
                            }
                        }
                    }

                    myweb.Update();

                }
                catch (Exception ex)
                {
                    return;
                }

                finally
                {

                    sitecollection.Dispose();
                    myweb.Dispose();
                }
            }
        
        // Uncomment the method below to handle the event raised after a feature has been activated.

        //public override void FeatureActivated(SPFeatureReceiverProperties properties)
        //{
        //}


        // Uncomment the method below to handle the event raised before a feature is deactivated.

        //public override void FeatureDeactivating(SPFeatureReceiverProperties properties)
        //{
        //}


        // Uncomment the method below to handle the event raised after a feature has been installed.

        //public override void FeatureInstalled(SPFeatureReceiverProperties properties)
        //{
        //}


        // Uncomment the method below to handle the event raised before a feature is uninstalled.

        //public override void FeatureUninstalling(SPFeatureReceiverProperties properties)
        //{
        //}

        // Uncomment the method below to handle the event raised when a feature is upgrading.

        //public override void FeatureUpgrading(SPFeatureReceiverProperties properties, string upgradeActionName, System.Collections.Generic.IDictionary<string, string> parameters)
        //{
        //}
    }
}
