using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;

namespace OntologieTags
{
    class TaggingDelegateControl : WebControl
    {
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            string helloAlert = "var options = SP.UI.$create_DialogOptions();" + 
                "options.width = 800; options.height = 500; options.url = &quot;/_layouts/CreatePage.aspx&quot;" +
                "SP.UI.ModalDialog.showModalDialog(options);";
            this.Page.ClientScript.RegisterClientScriptBlock(this.GetType(), "popup", helloAlert, true);
        }
    }
}
