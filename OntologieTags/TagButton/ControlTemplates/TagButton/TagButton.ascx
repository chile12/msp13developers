﻿<%@ Assembly Name="$SharePoint.Project.AssemblyFullName$" %>
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="asp" Namespace="System.Web.UI" Assembly="System.Web.Extensions, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" %>
<%@ Import Namespace="Microsoft.SharePoint" %> 
<%@ Register Tagprefix="WebPartPages" Namespace="Microsoft.SharePoint.WebPartPages" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="TagButton.ascx.cs" Inherits="TagButton.ControlTemplates.TagButton.TagButton" %>
<asp:ImageButton ID="tagsAndCategoriesBT" runat="server" BorderStyle="Dashed" 
    EnableTheming="True" Height="61px" ImageAlign="Middle" 
    ImageUrl="~/_layouts/images/SocialTagsAndNotes_32.png" 
    onclick="ImageButton1_Click" 
    ToolTip="extended tagging feature: tag any site or document" Width="61px" />

    
<script type="text/javascript">
    //ruft das Popup-Fenster auf
    function ModalPop() {
        // stellt sicher, dass das Modul: sp.js rechtzeitig geladen ist
        ExecuteOrDelayUntilScriptLoaded(
        //eine functions-definition innerhalb von ExecuteOrDelayUntilScriptLoaded ist vorgesehen                 
          function pop() {
              var options = SP.UI.$create_DialogOptions();
              options.width = 1080;
              options.height = 650;
              options.title = "Taxonomy & Tagging Control";
              options.url = "http://" + document.location.hostname + "/SitePages/Pages/TabNavAppTestPage.aspx";
              //popup öffnen
              SP.UI.ModalDialog.showModalDialog(options);
          }

, "sp.js")}
</script>