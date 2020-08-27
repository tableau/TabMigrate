using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Net;

/// <summary>
/// A arbitrary HTTP GET request we can perform after login into the REST API session
/// </summary>
class SendPostLogInCommand : TableauServerSignedInRequestBase
{
    /// <summary>
    /// URL manager
    /// </summary>
    private readonly TableauServerUrls _onlineUrls;
    private readonly string _postLoginCommandUrl;

    private string _commandResult;
    /// <summary>
    /// The result from running the command
    /// </summary>
    public string CommandResult
    {
        get
        {
            return _commandResult;
        }
    }

    public SendPostLogInCommand(TableauServerUrls onlineUrls, TableauServerSignIn login, string commandUrl)
        : base(login)
    {
        _onlineUrls = onlineUrls;
        _postLoginCommandUrl = commandUrl;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serverName"></param>
    public string ExecuteRequest()
    {
        string url = _postLoginCommandUrl;
        var webRequest = CreateLoggedInWebRequest(url);
        webRequest.Method = "GET";

        //Request the data from server
        _onlineSession.StatusLog.AddStatus("Custom web request: " + url, -10);
        var response = GetWebReponseLogErrors(webRequest, "custom request");
        
        using(response)
        {
            var responseText = GetWebResponseAsText(response);
            _commandResult = responseText;
            return responseText;
        }
    }
}
