using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Security;
using System.Text;
using System.Reflection;

namespace TagButton.Features.TabNavAppXap
{
    /// <summary>
    /// This class handles events raised during feature activation, deactivation, installation, uninstallation, and upgrade.
    /// </summary>
    /// <remarks>
    /// The GUID attached to this class may be used during packaging and should not be modified.
    /// </remarks>

    [Guid("ac04d8a1-fa0c-45c7-a09e-e1bd784ec125")]
    public class TabNavAppXapEventReceiver : SPFeatureReceiver
    {
        /// <summary>
        /// constructs the public key token for the used assambly
        /// </summary>
        private string GetAssemblyPublicKeyToken()
        {
            StringBuilder builder = new StringBuilder();
            foreach (byte b in Assembly.GetExecutingAssembly().GetName().GetPublicKeyToken())
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }

        public override void FeatureActivated(SPFeatureReceiverProperties properties)
        {
            SPWeb myweb = (properties.Feature.Parent as SPWeb);
            SPSite sitecollection = myweb.Site;
            string assVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string assName = Assembly.GetExecutingAssembly().GetName().Name;
            string assToken = GetAssemblyPublicKeyToken();
            try
            {
                foreach (SPWeb web in sitecollection.AllWebs)
                {
                    for (int l = 0; l < web.Lists.Count; l++)
                    {
                        if (!web.Lists[l].Hidden) //not!
                        {
                            if (web.Lists[l].EventReceivers.Count > 0)
                            {
                                for (int i = web.Lists[l].EventReceivers.Count - 1; i >= 0; i--)
                                {
                                    if (web.Lists[l].EventReceivers[i].Assembly.Contains(assToken))
                                        web.Lists[l].EventReceivers[i].Delete();
                                }
                            }
                            web.Lists[l].EventReceivers.Add(SPEventReceiverType.ItemDeleting,
                                   assName + ", Version=" + assVersion + ", Culture=neutral, PublicKeyToken=" + assToken,
                                   assName + ".SPVirtuosoItemEventReceivers");

                            web.Lists[l].EventReceivers.Add(SPEventReceiverType.ItemAdded,
                                   assName + ", Version=" + assVersion + ", Culture=neutral, PublicKeyToken=" + assToken,
                                   assName + ".SPVirtuosoItemEventReceivers");

                            web.Lists[l].EventReceivers.Add(SPEventReceiverType.ItemUpdating,
                               assName + ", Version=" + assVersion + ", Culture=neutral, PublicKeyToken=" + assToken,
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
            string assToken = GetAssemblyPublicKeyToken();
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
                                    if (list.EventReceivers[i].Assembly.Contains(assToken))
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
