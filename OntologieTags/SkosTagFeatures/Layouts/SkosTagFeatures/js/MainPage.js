//Default silverlight error handling
function onSilverlightError(sender, args) {
    var appSource = "";
    if (sender != null && sender != 0) {
        appSource = sender.getHost().Source;
    }

    var errorType = args.ErrorType;
    var iErrorCode = args.ErrorCode;

    if (errorType == "ImageError" || errorType == "MediaError") {
        return;
    }

    var errMsg = "Unhandled Error in Silverlight Application " + appSource + "\n";

    errMsg += "Code: " + iErrorCode + "    \n";
    errMsg += "Category: " + errorType + "       \n";
    errMsg += "Message: " + args.ErrorMessage + "     \n";

    if (errorType == "ParserError") {
        errMsg += "File: " + args.xamlFile + "     \n";
        errMsg += "Line: " + args.lineNumber + "     \n";
        errMsg += "Position: " + args.charPosition + "     \n";
    }
    else if (errorType == "RuntimeError") {
        if (args.lineNumber != 0) {
            errMsg += "Line: " + args.lineNumber + "     \n";
            errMsg += "Position: " + args.charPosition + "     \n";
        }
        errMsg += "MethodName: " + args.methodName + "     \n";
    }

    throw new Error(errMsg);
}

//get the value of a specific parameter from an URL via regEx
function getURLParam(name) {
    var regexString = "[\\?&]" + name + "=([^&#]*)";
    var regex = new RegExp(regexString);
    var results = regex.exec(window.location.href);
    if (results == null)
        return null;
    else
        return results[1];
}

//builds an url parameter from param name and value
function getInitParamString(param) {
    return param + "=" + getURLParam(param);
}

//returns the html that embeds a silverlight application at a specific path in a webpage
function embedSilverlight(applicationPath, parentControl, id, paramName) {
    var initParams = getInitParamString("listId") + "," + getInitParamString("MS.SP.Url") + "," + getInitParamString(paramName);
    return Silverlight.createObject(
                    applicationPath,
                    parentControl,
                    id,
                    { width: "100%", height: "100%", background: "white", version: "4.0.60310.0" },
                    null,
                    initParams,
                    "context"
                );
}