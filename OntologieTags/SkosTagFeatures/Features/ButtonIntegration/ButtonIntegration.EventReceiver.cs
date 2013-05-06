using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Security;
using Microsoft.SharePoint.Administration;

namespace TagButton.Features.Feature1
{
    /// <summary>
    /// This class handles events raised during feature activation, deactivation, installation, uninstallation, and upgrade.
    /// </summary>
    /// <remarks>
    /// The GUID attached to this class may be used during packaging and should not be modified.
    /// </remarks>

    [Guid("f0014f14-1b85-4220-a338-027cfbb4841c")]
    public class Feature1EventReceiver : SPFeatureReceiver
    {
        // Uncomment the method below to handle the event raised after a feature has been activated.
        public override void FeatureActivated(SPFeatureReceiverProperties properties)
        {
            SPWebService.ContentService.ApplyApplicationContentToLocalServer();
        }


        // Uncomment the method below to handle the event raised before a feature is deactivated.

        //public override void FeatureDeactivating(SPFeatureReceiverProperties properties)
        //{
        //}


        // Uncomment the method below to handle the event raised after a feature has been installed.


        //public override void FeatureInstalled(SPFeatureReceiverProperties properties)
        //{
        //    SPSite tmpRoot = new SPSite(SPServer.Local.Address.ToString());
        //    SPSiteCollection tmpRootColl = tmpRoot.WebApplication.Sites;
        //    foreach (SPSite site in tmpRootColl)
        //    {
        //        foreach (SPWeb web in site.AllWebs)
        //        {
        //            //FeatureID of Social Tags and Note Board Ribbon Controls
        //            string FeatureID = "756d8a58-4e24-4288-b981-65dc93f9c4e5";
        //            //Get feature Guid  
        //            Guid myfeatureid = new Guid(FeatureID);
        //            //remove feature

        //            SPFeatureCollection features = web.Features;
        //            //SPFeature myfeature = features[myfeatureid];

        //            if (features[myfeatureid] != null)    //Make sure Feature is installed!
        //            {
        //                web.Features.Remove(myfeatureid); //deactivate feature
        //            }
                    
        //            web.Dispose();
        //        }

        //        site.Dispose();
        //    }

        //}


        // Uncomment the method below to handle the event raised before a feature is uninstalled.

        //public override void FeatureUninstalling(SPFeatureReceiverProperties properties)
        //{
        //    SPSite tmpRoot = new SPSite(SPServer.Local.Address.ToString());
        //    SPSiteCollection tmpRootColl = tmpRoot.WebApplication.Sites;
        //    foreach (SPSite site in tmpRootColl)
        //    {
        //        foreach (SPWeb web in site.AllWebs)
        //        {
        //            //FeatureID of Social Tags and Note Board Ribbon Controls
        //            string FeatureID = "756d8a58-4e24-4288-b981-65dc93f9c4e5";
        //            //Get feature Guid  
        //            Guid myfeatureid = new Guid(FeatureID);
        //            //remove feature

        //            SPFeatureCollection features = web.Features;
        //            //SPFeature myfeature = features[myfeatureid];

        //            if (features[myfeatureid] != null)    //Make sure Feature is installed!
        //            {
        //                web.Features.Add(myfeatureid); //deactivate feature
        //            }

        //            web.Dispose();
        //        }

        //        site.Dispose();
        //    }
        //}

        // Uncomment the method below to handle the event raised when a feature is upgrading.

        //public override void FeatureUpgrading(SPFeatureReceiverProperties properties, string upgradeActionName, System.Collections.Generic.IDictionary<string, string> parameters)
        //{
        //}
    }
}