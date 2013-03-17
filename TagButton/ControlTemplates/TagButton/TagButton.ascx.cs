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

        protected void ImageButton1_Click(object sender, ImageClickEventArgs e)
        {
            string javaScript =
               "\n<script language=JavaScript>\n" +
               "ModalPop();\n" +
               "</script>\n";
  
           this.Parent.Page.ClientScript.RegisterStartupScript(this.GetType(), "Button1_ClickScript", javaScript, false);

        }
    }
}
