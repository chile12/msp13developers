//Opens the popup and calls the tabnavapp in it
function TabNavApp_PopUp(params) {
    return ExecuteOrDelayUntilScriptLoaded(
    //eine functions-definition innerhalb von ExecuteOrDelayUntilScriptLoaded ist vorgesehen 
    function pop() {
        var options = {
            url: "/_Layouts/SkosTagFeatures/MainPage/MainPage.aspx?" + params,
            title: "Taxonomy & Tagging Control",
            allowMaximize: true,
            showClose: true,
            width: 1080,
            height: 700,
            dialogReturnValueCallback: onCloseCallback
        };
        SP.UI.ModalDialog.showModalDialog(options);
    }
, "sp.js");
}

function onCloseCallback(dialogResult, returnValue) {
    window.location.href = returnValue;
}



function getURLandPopup(params)
{
    var url = location.protocol + "//" + location.host + L_Menu_BaseUrl;
    TabNavApp_PopUp("MS.SP.Url=" + url + "&" + params);
    
}

//Sends an async recursive request to the database to retreive the GUIDs of all selected items
function GetParameters_AsyncRecursive(idstring, ctx, i, count, items, list) {
    if (i == count) {
        //if all GUIDs have been looked up, open the popup
        getURLandPopup(idstring);
        return;
    }

    var itemId = items[i].id;
    var item = list.getItemById(itemId);
    ctx.load(item);
    ctx.executeQueryAsync(
    //success, call recursive
	function () {
	    idstring += item.get_item('UniqueId') + '_';
	    GetParameters_AsyncRecursive(idstring, ctx, i + 1, count, items, list);
	},
	function (sender, args) {
	    alert('Request failed. \nError: ' + args.get_message() + '\nStackTrace: ' + args.get_stackTrace());
	    return;
	});
}

//Call to look up Document parameters and open popup
function TabNavApp_GetDocsPopUp() {
    var ctx = SP.ClientContext.get_current();
    var listId = SP.ListOperation.Selection.getSelectedList();
    var list = ctx.get_web().get_lists().getById(listId);
    var selectedItems = SP.ListOperation.Selection.getSelectedItems(ctx);
    var itemCount = CountDictionary(selectedItems);
    
    if (itemCount == 0) {
        alert('No Items selected!');
        return;
    }
    var idstring = "listId=" + listId + "&docs=";
    GetParameters_AsyncRecursive(idstring, ctx, 0, itemCount, selectedItems, list);
}