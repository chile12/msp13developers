<%@ Page Language="C#" AutoEventWireup="true" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
<head id="Head1" runat="server">
    <title>TabNavApp</title>
    <style type="text/css">
    html, body {
	    height: 100%;
	    overflow: auto;
    }
    body {
	    padding: 0;
	    margin: 0;
    }
    #silverlightControlHost {
	    height: 100%;
	    text-align:center;
    }
    </style>
    <script type="text/javascript" src="../js/MainPage.js"></script>
    <script type="text/javascript" src="../js/Silverlight.js"></script>
</head>
<body>
    <form id="form1" runat="server" style="height:100%">
        <div id="silverlightControlHost">
            <script type="text/javascript">
                embedSilverlight("/_catalogs/masterpage/TabNavApp/TabNavApp.xap", 
                    silverlightControlHost, "slPlugin", "docs");
            </script>
        </div>
    </form>
</body>
</html>


