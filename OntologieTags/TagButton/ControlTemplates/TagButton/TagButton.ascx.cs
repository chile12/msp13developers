using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web;

namespace TagButton.ControlTemplates.TagButton
{
    public partial class TagButton : UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {


        }
        /// <summary>
        /// event: fires when Tag-Button on Masterpage is clicked
        /// opens popup window to present the Web-Feature
        /// </summary>
        /// <param name="sender">not needed</param>
        /// <param name="e">not needed</param>
        protected void ImageButton1_Click(object sender, ImageClickEventArgs e)
        {
            string javaScript =         //javasript-string
               "\n<script language=JavaScript>\n" +
               "ModalPop();\n" +
               "</script>\n";
  
            //run script on sharepont
           this.Parent.Page.ClientScript.RegisterStartupScript(this.GetType(), "Button1_ClickScript", javaScript, false);

        }
    }
}
